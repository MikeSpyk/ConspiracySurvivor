#define DEBUG_SAVE
//#undef DEBUG_SAVE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EntityManager : MonoBehaviour
{
    public static EntityManager singleton;

    [Header("Settings")]
    [SerializeField] public float m_clientPlayerViewDistance = 2000; // for lowering distant objects count on client
    [SerializeField] private float m_clientInstancingDistanceMax = 100; // how far can the player move after the Distant Entities Instancing Data was updated before it will update again
    [SerializeField] private float m_serverPlayerViewDistanceMin = 100;
    [SerializeField] private float m_serverPlayerViewDistanceMax = 700;
    [SerializeField] private float m_serverPlayerDistantViewDistanceMax = 2000;
    [SerializeField, ReadOnly] private int m_entityUIDCounter = 0;
    [SerializeField, ReadOnly] private int m_activeEntitiesCountOutput = 0;
    [SerializeField, ReadOnly] private int m_culledEntitiesCountOutput = 0;
    [SerializeField, ReadOnly] private int m_distantEntitiesCountOutput = 0; // client only
    [Header("Prefabs")]
    [SerializeField] private GameObject[] m_entityPrefabs;
    [SerializeField] private LODSpritesEntity[] m_distantEntitesPrefabs;
    [SerializeField] private bool[] m_entityHideInHierarchy;
    [Header("World Items")]
    [SerializeField] private float m_itemDropHeight = 0.5f;
    [SerializeField] private float m_itemDropForward = 0.5f;
    [SerializeField] private float m_itemDropSpeed = 1f;
    [Header("Debug")]
    [SerializeField] private bool m_localClientDistantInstancing = true;
    [SerializeField] private bool m_recomputeHideInHierarchy = false;
    [SerializeField] private bool DEBUG_showWorldGrid = false;
    [SerializeField] private bool DEBUG_showVisibleEntityFieldsGrid = false;

    private Entity_base[] m_entityPrefabsScripts;
    private Dictionary<int, Entity_base> m_entityID_entityScript = new Dictionary<int, Entity_base>();
    private Dictionary<int, Player_external> m_gameID_playerScript = new Dictionary<int, Player_external>();
    private List<DataEntity_Base> m_culledEntities = new List<DataEntity_Base>();
    private Dictionary<int, Dictionary<int, DistantEntityData>> m_prefabIndex_distantEntities = null; // client only
    private bool m_distantEntitiesDirty = true; // client only
    private InstancedMeshDrawer m_distantEntityDrawer = new InstancedMeshDrawer();
    private float m_lastViewDistanceClient = 0;
    private Vector3 m_lastPlayerPosInstancingUpdate = Vector3.zero;
    private WorldGrid m_worldGrid = null;

    private Player_local m_localPlayerScript = null;

    #region Unity Methods

    private void Awake()
    {
        singleton = this;

        m_entityPrefabsScripts = new Entity_base[m_entityPrefabs.Length];

        for (int i = 0; i < m_entityPrefabs.Length; i++)
        {
            m_entityPrefabsScripts[i] = m_entityPrefabs[i].GetComponent<Entity_base>();
            if (m_entityPrefabsScripts[i] == null)
            {
                Debug.LogError("EntityManager: Awake: Entity_base script could not be found !");
            }
        }

        if (m_entityPrefabs.Length != m_distantEntitesPrefabs.Length)
        {
            Debug.LogError("m_entityPrefabs.Length != m_distantEntitesPrefabs.Length. both must have the same dimensions. use null entries in m_distantEntitesPrefabs to avoid rendering them.");
        }

        if (m_entityPrefabs.Length != m_entityHideInHierarchy.Length)
        {
            Debug.LogError("m_entityPrefabs.Length != m_entityHideInHierarchy.Length. both must have the same dimensions. use null entries in m_distantEntitesPrefabs to avoid rendering them.");
        }

        // client only
        m_prefabIndex_distantEntities = new Dictionary<int, Dictionary<int, DistantEntityData>>();
        for (int i = 0; i < m_entityPrefabs.Length; i++)
        {
            m_prefabIndex_distantEntities.Add(i, new Dictionary<int, DistantEntityData>());
        }

        m_distantEntityDrawer.setPrefabs(m_distantEntitesPrefabs);
    }

    private void Update()
    {
        if(DEBUG_showWorldGrid)
        {
            DEBUG_showWorldGrid = false;
            m_worldGrid.DEBUG_showGrid();
        }

        if(DEBUG_showVisibleEntityFieldsGrid)
        {
            m_worldGrid.DEBUG_showVisibleEntityFields();
        }

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            server_entityUnculling();// create entities that entered the range of at least one player
            server_entityPlayerVisibility();// update players within range of each entity
            server_distantEntityPlayerVisibility(); // culls and unculls distant objects that are on serverside absolute culled
        }

        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            if (m_clientPlayerViewDistance != m_lastViewDistanceClient)
            {
                m_lastViewDistanceClient = m_clientPlayerViewDistance;
                m_distantEntitiesDirty = true;
            }

            if (Vector3.Distance(getLocalPlayerPosition(), m_lastPlayerPosInstancingUpdate) > m_clientInstancingDistanceMax)
            {
                m_distantEntitiesDirty = true;
            }

            m_distantEntitiesCountOutput = 0;
            foreach (KeyValuePair<int, Dictionary<int, DistantEntityData>> keyValuePair in m_prefabIndex_distantEntities)
            {
                m_distantEntitiesCountOutput += keyValuePair.Value.Count;
            }

            if (GameManager_Custom.singleton.isClient || (GameManager_Custom.singleton.isServerAndClient && m_localClientDistantInstancing))
            {
                if (m_distantEntitiesDirty)
                {
                    recreateDistantEntitiesInstancingData();
                    m_distantEntitiesDirty = false;
                }

                if (m_distantEntityDrawer.instancesReady)
                {
                    m_distantEntityDrawer.drawInstances();
                }
            }
        }

        if (m_recomputeHideInHierarchy)
        {
            foreach (KeyValuePair<int, Entity_base> ID_EntityPair in m_entityID_entityScript)
            {
                if (ID_EntityPair.Value.prefabIndex > -1)
                {
                    if (m_entityHideInHierarchy[ID_EntityPair.Value.prefabIndex])
                    {
                        ID_EntityPair.Value.gameObject.hideFlags = HideFlags.HideInHierarchy;
                    }
                    else
                    {
                        ID_EntityPair.Value.gameObject.hideFlags = HideFlags.None;
                    }
                }
            }

            m_recomputeHideInHierarchy = false;
        }

        m_culledEntitiesCountOutput = m_culledEntities.Count;
        m_activeEntitiesCountOutput = m_entityID_entityScript.Count;
    }

    #endregion

    #region Save File Serialization

    public List<byte> getGameSaveData()
    {
        List<byte> returnValue = new List<byte>();


        returnValue.AddRange(BitConverter.GetBytes(m_entityUIDCounter));

        List<byte> entityData = getAllEntitesSaveData();

        returnValue.AddRange(BitConverter.GetBytes(entityData.Count));
        returnValue.AddRange(entityData);

        return returnValue;
    }

    public int loadFromSaveData(byte[] data, int index)
    {
        m_entityUIDCounter = BitConverter.ToInt32(data, index);
        index += 4;

        int entitesLength = BitConverter.ToInt32(data, index);
        index += 4;

        spawnEntitesFromSaveData(data, index, index + entitesLength);
        index += entitesLength;

        return index;
    }

    private List<byte> getAllEntitesSaveData()
    {
        List<byte> saveData = new List<byte>();
        List<byte> temp_oneData = new List<byte>();

        for (int i = 0; i < m_culledEntities.Count; i++)
        {
            if (m_culledEntities[i].m_entityPrefabID != -1)
            {
                temp_oneData.Clear();
                m_culledEntities[i].fillSaveData(temp_oneData);
                saveData.AddRange(temp_oneData);
            }
        }

        foreach (KeyValuePair<int, Entity_base> entityID_Script in m_entityID_entityScript)
        {
            if (entityID_Script.Value.prefabIndex != -1)
            {
                temp_oneData.Clear();
                entityID_Script.Value.getUpdatedDataEntity().fillSaveData(temp_oneData);
                saveData.AddRange(temp_oneData);
            }
        }

        return saveData;
    }

    private void spawnEntitesFromSaveData(byte[] data, int startIndex, int endIndex)
    {
        int temp_prefabIndex;
        DataEntity_Base temp_dataEntity;

        while (startIndex < endIndex)
        {
            temp_prefabIndex = BitConverter.ToInt32(data, startIndex);
            startIndex += 4; // 4 byte int (prefab index)
            temp_dataEntity = m_entityPrefabsScripts[temp_prefabIndex].getNewDefaultDataEntity();
            startIndex = temp_dataEntity.setFromSaveData(data, startIndex);
            temp_dataEntity.m_entityPrefabID = temp_prefabIndex;
            server_spawnEntity(temp_dataEntity);
        }
    }

    #endregion

    #region Distant Entity Rendering

    private void recreateDistantEntitiesInstancingData()
    {
        m_lastPlayerPosInstancingUpdate = getLocalPlayerPosition();

        if (m_clientPlayerViewDistance < m_serverPlayerViewDistanceMax) // client can't see distant entities because its view distance is too short
        {
            foreach (KeyValuePair<int, Dictionary<int, DistantEntityData>> prefabIndex_DictEntities in m_prefabIndex_distantEntities)
            {
                m_distantEntityDrawer.setInstancesDataForPrefab(prefabIndex_DictEntities.Key, new List<Vector3>(), new List<Quaternion>(), Vector3.zero);
            }
            return;
        }

        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();
        Vector3 playerPostition = getLocalPlayerPosition();

        foreach (KeyValuePair<int, Dictionary<int, DistantEntityData>> prefabIndex_DictEntities in m_prefabIndex_distantEntities)
        {
            foreach (KeyValuePair<int, DistantEntityData> entityID_entityScript in prefabIndex_DictEntities.Value)
            {
                if (Vector3.Distance(playerPostition, entityID_entityScript.Value.m_position) < m_clientPlayerViewDistance)
                {
                    positions.Add(entityID_entityScript.Value.m_position);
                    rotations.Add(entityID_entityScript.Value.m_rotation);
                }
            }

            m_distantEntityDrawer.setInstancesDataForPrefab(prefabIndex_DictEntities.Key, positions, rotations, playerPostition);

            positions.Clear();
            rotations.Clear();
        }
    }

    #endregion

    #region Entity Culling

    private void server_entityUnculling() // creates (currently culled) entities that entered the range of at least one player
    {
        List<Vector3> playerPostitions;
        List<int> playerIDs;
        List<float> clientViewDistances;

        PlayerManager.singleton.getAllViewPointsPositions(out playerPostitions, out playerIDs, out clientViewDistances);

        List<float> playerViewDistancesClose = new List<float>();

        for (int i = 0; i < clientViewDistances.Count; i++)
        {
            playerViewDistancesClose.Add(Mathf.Max(Mathf.Min(clientViewDistances[i], m_serverPlayerViewDistanceMax), m_serverPlayerViewDistanceMin));
        }

        if (m_worldGrid != null)
        {
            m_worldGrid.updateVisibleGridSections(playerPostitions, playerIDs, playerViewDistancesClose);

            Dictionary<int, int> playerID_index = new Dictionary<int, int>();

            for (int i = 0; i < playerPostitions.Count; i++)
            {
                playerID_index.Add(playerIDs[i], i);
            }

            int tempPlayerIndex;

            for (int i = 0; i < m_worldGrid.visibleEntityFields.Count; i++)
            {
                for (int j = 0; j < m_worldGrid.visibleEntityFields[i].unloadedEntities.Count; j++)
                {
                    for (int k = 0; k < m_worldGrid.visibleEntityFields[i].entityViewersGameID.Count; k++)
                    {
                        if (playerID_index.TryGetValue(m_worldGrid.visibleEntityFields[i].entityViewersGameID[k], out tempPlayerIndex))
                        {
                            if (Vector3.Distance(m_worldGrid.visibleEntityFields[i].unloadedEntities[j].m_position, playerPostitions[tempPlayerIndex]) < playerViewDistancesClose[tempPlayerIndex])
                            {
                                m_culledEntities.Remove(m_worldGrid.visibleEntityFields[i].unloadedEntities[j]);
                                server_spawnEntity(m_worldGrid.visibleEntityFields[i].unloadedEntities[j]);
                                m_worldGrid.visibleEntityFields[i].unloadedEntities.RemoveAt(j);
                                j--;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    private void server_entityPlayerVisibility() // update players within range of each entity
    {
        List<Vector3> playerPostitions;
        List<int> playerIDs;
        List<float> clientViewDistances;

        PlayerManager.singleton.getAllViewPointsPositions(out playerPostitions, out playerIDs, out clientViewDistances);

        List<float> playerViewDistancesClose = new List<float>();
        List<float> playerViewDistancesDistant = new List<float>();

        for (int i = 0; i < clientViewDistances.Count; i++)
        {
            playerViewDistancesClose.Add(Mathf.Max(Mathf.Min(clientViewDistances[i], m_serverPlayerViewDistanceMax), m_serverPlayerViewDistanceMin));
            playerViewDistancesDistant.Add(Mathf.Min(clientViewDistances[i], m_serverPlayerDistantViewDistanceMax));
        }

        List<int> nullEntities = new List<int>();
        List<int> entitiesToCullServer = new List<int>();

        foreach (KeyValuePair<int, Entity_base> ID_EntityPair in m_entityID_entityScript)
        {
            if (ID_EntityPair.Value == null)
            {
                nullEntities.Add(ID_EntityPair.Key);
                continue;
            }

            for (int j = 0; j < playerPostitions.Count; j++)
            {
                if (ID_EntityPair.Value.m_observingPlayersID.Contains(playerIDs[j]))
                {
                    // check client culling
                    if (Vector3.Distance(playerPostitions[j], ID_EntityPair.Value.transform.position) > playerViewDistancesClose[j])
                    {
                        ID_EntityPair.Value.m_observingPlayersID.Remove(playerIDs[j]);

                        if (Vector3.Distance(playerPostitions[j], ID_EntityPair.Value.transform.position) > playerViewDistancesDistant[j])
                        {
                            // don't see at all
                            server_sendEntityCullCommand(ID_EntityPair.Value.entityUID, playerIDs[j], false);
                        }
                        else
                        {
                            // see as distant
                            ID_EntityPair.Value.m_observingDistantPlayersID.Add(playerIDs[j]);
                            server_sendEntityCullCommand(ID_EntityPair.Value.entityUID, playerIDs[j], true);
                        }
                    }
                }
                else if (ID_EntityPair.Value.m_observingDistantPlayersID.Contains(playerIDs[j]))
                {
                    // check client unculling (close)
                    if (Vector3.Distance(playerPostitions[j], ID_EntityPair.Value.transform.position) < playerViewDistancesClose[j])
                    {
                        ID_EntityPair.Value.m_observingDistantPlayersID.Remove(playerIDs[j]);
                        ID_EntityPair.Value.m_observingPlayersID.Add(playerIDs[j]);
                        server_sendEntityUncullCommand(ID_EntityPair.Value, playerIDs[j]);
                    }
                    else if (Vector3.Distance(playerPostitions[j], ID_EntityPair.Value.transform.position) > playerViewDistancesDistant[j])
                    {
                        // cull distant object
                        ID_EntityPair.Value.m_observingDistantPlayersID.Remove(playerIDs[j]);
                        server_sendDistantEntityCullCommand(ID_EntityPair.Value.prefabIndex, ID_EntityPair.Value.entityUID, playerIDs[j]);
                    }
                }
                else
                {
                    // check client unculling (distant)
                    if (Vector3.Distance(playerPostitions[j], ID_EntityPair.Value.transform.position) < playerViewDistancesDistant[j])
                    {
                        ID_EntityPair.Value.m_observingDistantPlayersID.Add(playerIDs[j]);
                        server_sendDistantEntityUncullCommand(ID_EntityPair.Value.entityUID, ID_EntityPair.Value.prefabIndex, playerIDs[j], ID_EntityPair.Value.transform.position, ID_EntityPair.Value.transform.rotation);
                    }
                }
            }

            if (ID_EntityPair.Value.m_observingPlayersID.Count == 0)
            {
                // server culling
                if (ID_EntityPair.Value.cullable)
                {
                    entitiesToCullServer.Add(ID_EntityPair.Key);
                }
            }
        }

        for (int i = 0; i < entitiesToCullServer.Count; i++)
        {
            if (m_entityID_entityScript.ContainsKey(entitiesToCullServer[i]))
            {
                server_cullEntity(m_entityID_entityScript[entitiesToCullServer[i]]);
            }
        }

        for (int i = 0; i < nullEntities.Count; i++)
        {
            Debug.LogWarning("removed NULL entity ID:" + nullEntities[i]);
            m_entityID_entityScript.Remove(nullEntities[i]);
        }
    }

    private void server_distantEntityPlayerVisibility()
    {
        List<Vector3> playerPostitions;
        List<int> playerIDs;
        List<float> clientViewDistances;

        PlayerManager.singleton.getAllViewPointsPositions(out playerPostitions, out playerIDs, out clientViewDistances);

        List<float> playerViewDistancesDistant = new List<float>();

        for (int i = 0; i < clientViewDistances.Count; i++)
        {
            playerViewDistancesDistant.Add(Mathf.Min(clientViewDistances[i], m_serverPlayerDistantViewDistanceMax));
        }

        /*

        IS THIS NEEDED ??

        foreach (KeyValuePair<int, Player_external> dictEntry in m_gameID_playerScript)
        {
            playerPostitions.Add(dictEntry.Value.transform.position);
            playerIDs.Add(dictEntry.Value.m_gameID);
        }

        if (isLocalPlayerActive())
        {
            playerPostitions.Add(m_localPlayerScript.transform.position);
            playerIDs.Add(-1);
        }

        */

        for (int i = 0; i < m_culledEntities.Count; i++)
        {
            for (int j = 0; j < playerPostitions.Count; j++)
            {
                if (m_culledEntities[i].m_observingDistantPlayers.Contains(playerIDs[j]))
                {
                    // cull
                    if (Vector3.Distance(m_culledEntities[i].m_position, playerPostitions[j]) > playerViewDistancesDistant[j])
                    {
                        m_culledEntities[i].m_observingDistantPlayers.Remove(playerIDs[j]);
                        server_sendDistantEntityCullCommand(m_culledEntities[i].m_entityPrefabID, m_culledEntities[i].m_entity_UID, playerIDs[j]);
                    }
                }
                else
                {
                    // uncull
                    if (Vector3.Distance(m_culledEntities[i].m_position, playerPostitions[j]) < playerViewDistancesDistant[j])
                    {
                        m_culledEntities[i].m_observingDistantPlayers.Add(playerIDs[j]);
                        server_sendDistantEntityUncullCommand(m_culledEntities[i].m_entity_UID, m_culledEntities[i].m_entityPrefabID, playerIDs[j], m_culledEntities[i].m_position, m_culledEntities[i].m_rotation);
                    }
                }
            }
        }
    }

    private void server_gridFieldNoMoreEntityViewers(object sender, WorldGridFieldArgs e)
    {
        Debug.Log("EntityManager: gridFieldNoMoreEntityViewers: " + e.worldGridField.uid);
    }

    private void sever_gridFieldEntityViewerLeftEvent(object sender, WorldGridFieldIntegerArgs e)
    {
        Debug.Log("EntityManager: gridFieldNoMoreEntityViewers: player " + e.integer.ToString() + " left field: " + e.worldGridField.uid);
    }

    private void server_cullEntity(Entity_base entity)
    {
        m_entityID_entityScript.Remove(entity.getEntityUID());

        DataEntity_Base dataEntity = entity.getUpdatedDataEntity();

        m_culledEntities.Add(dataEntity);
        m_worldGrid.getField(entity.entity_gridFieldID).unloadedEntities.Add(dataEntity);
        Destroy(entity.gameObject);
    }

    private void server_sendEntityCullCommand(int entityUID, int clientGameID, bool createDistantObject)
    {
        // could be more effective if network message gets duplicated (getoutput, copyoutput) instead of creating a new message for each client
        NetworkMessage message = NetworkingManager.singleton.getNewNetworkMessage();
        message.setMessageContext((int)NetworkingManager.NetMessageContextTCP.EntityCull);
        message.addIntegerValues(entityUID);
        if (createDistantObject)
        {
            message.addIntegerValues(1);
        }
        NetworkingManager.singleton.server_sendCustomMessageTCP(message, clientGameID);
    }

    private void server_sendEntityUncullCommand(Entity_base entity, int clientGameID)
    {
        NetworkingManager.singleton.server_sendCustomMessageTCP(entity.server_fillUncullingMessage(NetworkingManager.singleton.getNewNetworkMessage()), clientGameID);
    }

    private void server_sendDistantEntityCullCommand(int prefabID, int entityUID, int clientGameID)
    {
        NetworkMessage message = NetworkingManager.singleton.getNewNetworkMessage();
        message.setMessageContext((int)NetworkingManager.NetMessageContextTCP.EntityDistantCull);
        message.addIntegerValues(entityUID, prefabID);
        NetworkingManager.singleton.server_sendCustomMessageTCP(message, clientGameID);
    }

    private void server_sendDistantEntityUncullCommand(int entityUID, int prefabIndex, int clientGameID, Vector3 position, Quaternion rotation)
    {
        NetworkMessage message = NetworkingManager.singleton.getNewNetworkMessage();
        message.setMessageContext((int)NetworkingManager.NetMessageContextTCP.EntityDistantUncull);
        message.addIntegerValues(entityUID, prefabIndex);
        message.addFloatValues(position.x, position.y, position.z, rotation.eulerAngles.x, rotation.eulerAngles.y, rotation.eulerAngles.z);

        NetworkingManager.singleton.server_sendCustomMessageTCP(message, clientGameID);
    }

    public void client_entityCreateCommand(NetworkMessage message) // from server
    {
        if (message.integerValuesCount < 2)
        {
            Debug.LogWarning("EntityManager: client_entityCreateCommand: message too short. need at least 2 integer values: given values: " + message.integerValuesCount);
            return;
        }

        if (m_prefabIndex_distantEntities[message.getIntValue(1)].ContainsKey(message.getIntValue(0)))
        {
            m_prefabIndex_distantEntities[message.getIntValue(1)].Remove(message.getIntValue(0));
            m_distantEntitiesDirty = true;
        }

        if (GameManager_Custom.singleton.isServerAndClient)
        {
            return;
        }

        client_spawnEntity(message);
    }

    public void client_distantEntityCreateCommand(NetworkMessage message) // from server
    {
        if (message.integerValuesCount < 2 || message.floatValuesCount < 6)
        {
            Debug.LogWarning("EntityManager: client_distantEntityCreateCommand: value counts out of bounds: int: " + message.integerValuesCount + "; float: " + message.floatValuesCount);
            return;
        }

        if (message.getIntValue(1) > -1 && message.getIntValue(1) < m_distantEntitesPrefabs.Length)
        {
            if (!m_prefabIndex_distantEntities[message.getIntValue(1)].ContainsKey(message.getIntValue(0)))
            {
                m_prefabIndex_distantEntities[message.getIntValue(1)].Add(
                                                                            message.getIntValue(0), new DistantEntityData()
                                                                            {
                                                                                m_position = new Vector3(message.getFloatValue(0), message.getFloatValue(1), message.getFloatValue(2)),
                                                                                m_rotation = Quaternion.Euler(new Vector3(message.getFloatValue(3), message.getFloatValue(4), message.getFloatValue(5)))
                                                                            }
                                                                            );
                m_distantEntitiesDirty = true;
            }
        }
    }

    public void client_entityRemoveCommand(NetworkMessage message) // from server
    {
        if (message.integerValuesCount == 2) // cull and create distant object
        {
            // distant object
            if (m_entityID_entityScript.ContainsKey(message.getIntValue(0)))
            {
                Entity_base entity = m_entityID_entityScript[message.getIntValue(0)];
                m_prefabIndex_distantEntities[entity.prefabIndex].Add(entity.entityUID, new DistantEntityData() { m_position = entity.transform.position, m_rotation = entity.transform.rotation });
                m_distantEntitiesDirty = true;
            }
            else
            {
                if (GameManager_Custom.singleton.isServerAndClient)
                {
                    DataEntity_Base entity = getCulledEntity(message.getIntValue(0));

                    if (entity == null)
                    {
                        Debug.LogWarning("EntityManager: client_entityRemoveCommand: received entity cull message with unknown entity-ID: " + message.getIntValue(0));
                    }
                    else
                    {
                        m_prefabIndex_distantEntities[entity.m_entityPrefabID].Add(entity.m_entity_UID, new DistantEntityData() { m_position = entity.m_position, m_rotation = entity.m_rotation });
                        m_distantEntitiesDirty = true;
                    }
                }
                else
                {
                    Debug.LogWarning("EntityManager: client_entityRemoveCommand: received entity cull message with unknown entity-ID: " + message.getIntValue(0));
                }
            }

            // cull
            if (GameManager_Custom.singleton.isClient) // not server and client
            {
                client_cullEntity(message.getIntValue(0));
            }
        }
        else if (message.integerValuesCount == 1) // cull only
        {
            if (GameManager_Custom.singleton.isClient) // not server and client
            {
                client_cullEntity(message.getIntValue(0));
            }
        }
        else
        {
            Debug.LogWarning("EntityManager: client_entityRemoveCommand: message out of bounds: " + message.integerValuesCount);
        }
    }

    public void client_distantEntityRemoveCommand(NetworkMessage message)
    {
        //message.addIntegerValues(entityUID, prefabID);

        if (message.integerValuesCount < 2)
        {
            Debug.LogWarning("EntityManager: client_distantEntityRemoveCommand: int values count out of bounds: " + message.integerValuesCount);
        }
        else
        {
            if (message.getIntValue(1) > -1 && message.getIntValue(1) < m_distantEntitesPrefabs.Length)
            {
                if (m_prefabIndex_distantEntities[message.getIntValue(1)].ContainsKey(message.getIntValue(0)))
                {
                    m_prefabIndex_distantEntities[message.getIntValue(1)].Remove(message.getIntValue(0));
                    m_distantEntitiesDirty = true;
                }
                else
                {
                    Debug.LogWarning("EntityManager: client_distantEntityRemoveCommand: Entity UID not found: " + message.getIntValue(0));
                }
            }
        }
    }

    private void server_spawnEntity(DataEntity_Base dataEntity)
    {
        if (m_entityID_entityScript.ContainsKey(dataEntity.m_entity_UID))
        {
            Debug.LogWarning("EntityManager: server_spawnEntity: tried to activate a entity that is already active: ID:" + dataEntity.m_entity_UID);
            return;
        }

        if (dataEntity.m_entityPrefabID < 0 || dataEntity.m_entityPrefabID >= m_entityPrefabs.Length)
        {
            Debug.LogWarning("EntityManager: server_spawnEntity: dataEntity prefab index out of range: " + dataEntity.m_entityPrefabID + " (" + dataEntity.GetType().Name + "). (EntityID: " + dataEntity.m_entity_UID + ") removing from game...");
            return;
        }
        Entity_base entity = (Instantiate(m_entityPrefabs[dataEntity.m_entityPrefabID], dataEntity.m_position, dataEntity.m_rotation) as GameObject).GetComponent<Entity_base>();
        entity.setDataEntity(dataEntity);
        m_entityID_entityScript.Add(entity.getEntityUID(), entity);

        if (m_entityHideInHierarchy[entity.prefabIndex])
        {
            entity.gameObject.hideFlags = HideFlags.HideInHierarchy;
        }
        else
        {
            entity.gameObject.hideFlags = HideFlags.None;
        }
    }

    private void client_spawnEntity(NetworkMessage message)
    {
        if (message.integerValuesCount < 2)
        {
            Debug.LogWarning("EntityManager: client_spawnEntity: message too short: need at least 2 integer values");
            return;
        }

        if (message.floatValuesCount < 6)
        {
            Debug.LogWarning("EntityManager: client_spawnEntity: message too short: need at least 6 float values");
            return;
        }

        int prefabIndex = message.getIntValue(1);

        if (prefabIndex < 0 || prefabIndex >= m_entityPrefabs.Length)
        {
            Debug.LogWarning("EntityManager: client_spawnEntity: prefab index out of range: " + prefabIndex);
            return;
        }

        if (m_entityID_entityScript.ContainsKey(message.getIntValue(0)))
        {
            if (m_localPlayerScript != null)
            {
                if (m_localPlayerScript.entityUID == message.getIntValue(0))
                {
                    return; // it is normal to not create a player external for the local player
                }
            }

            Debug.LogWarning("EntityManager: client_spawnEntity: entity ID already active: " + message.getIntValue(0));
            return;
        }

        Vector3 position = new Vector3(message.getFloatValue(0), message.getFloatValue(1), message.getFloatValue(2));
        Quaternion rotation = Quaternion.Euler(message.getFloatValue(3), message.getFloatValue(4), message.getFloatValue(5));

        Entity_base entity = (Instantiate(m_entityPrefabs[prefabIndex], position, rotation) as GameObject).GetComponent<Entity_base>();

        if (m_entityHideInHierarchy[prefabIndex])
        {
            entity.gameObject.hideFlags = HideFlags.HideInHierarchy;
        }
        else
        {
            entity.gameObject.hideFlags = HideFlags.None;
        }

        entity.client_receiveUncullMessage(message);

        m_entityID_entityScript.Add(entity.getEntityUID(), entity);
    }

    private void client_cullEntity(int entityUID)
    {
        if (m_entityID_entityScript.ContainsKey(entityUID))
        {
#if DEBUG_SAVE
            Player_local localPlayer = m_entityID_entityScript[entityUID] as Player_local;

            if (localPlayer == null)
            {
#endif
                if (m_entityID_entityScript[entityUID] != null)
                {
                    Destroy(m_entityID_entityScript[entityUID].gameObject);
                }
                m_entityID_entityScript.Remove(entityUID);
#if DEBUG_SAVE
            }
            else
            {
                Debug.LogWarning("EntityManager: client_cullEntity: received cull local player command ! ignoring !");
            }
#endif
        }
        else
        {
            Debug.LogWarning("EntityManager: client_cullEntity: received entity cull message with unknown entity-ID: " + entityUID);
        }
    }

    #endregion

    #region Entity Registration

    public int registerNewEntity(Entity_base entityScript) // for an entity that got created by instantiating
    {
        m_entityUIDCounter++;
        m_entityID_entityScript.Add(m_entityUIDCounter, entityScript);
        entityScript.setEntityUID(m_entityUIDCounter);

        foreach (KeyValuePair<int, Player_external> dictEntry in m_gameID_playerScript)
        {
            if (Vector3.Distance(dictEntry.Value.transform.position, entityScript.transform.position) < m_serverPlayerViewDistanceMax)
            {
                entityScript.m_observingPlayersID.Add(dictEntry.Value.m_gameID);
                server_sendEntityUncullCommand(entityScript, dictEntry.Value.m_gameID);
            }
        }

        if (isLocalPlayerActive())
        {
            if (Vector3.Distance(getLocalPlayerPosition(), entityScript.transform.position) < m_serverPlayerViewDistanceMax)
            {
                entityScript.m_observingPlayersID.Add(getLocalPlayer().m_gameID);
                server_sendEntityUncullCommand(entityScript, getLocalPlayer().m_gameID);
            }
        }

        return m_entityUIDCounter;
    }

    public void unregisterEntity(Entity_base entityScript)
    {
        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            if (m_entityID_entityScript.ContainsKey(entityScript.getEntityUID()))
            {
                m_entityID_entityScript.Remove(entityScript.getEntityUID());

                if (entityScript.getPrefabIndex() != -1)
                {
                    for (int i = 0; i < entityScript.m_observingPlayersID.Count; i++)
                    {
                        server_sendEntityCullCommand(entityScript.entityUID, entityScript.m_observingPlayersID[i], false);
                    }

                    if (m_entityHideInHierarchy[entityScript.getPrefabIndex()])
                    {
                        entityScript.gameObject.hideFlags = HideFlags.HideInHierarchy;
                    }
                    else
                    {
                        entityScript.gameObject.hideFlags = HideFlags.None;
                    }
                }
            }
        }
    }

    public int server_registerPlayerEntity(Player_external entityScript)
    {
        //Debug.Log("EntityManager:server_registerPlayerEntity: new ID");

        m_gameID_playerScript.Add(entityScript.m_gameID, entityScript);
        m_entityUIDCounter++;
        m_entityID_entityScript.Add(m_entityUIDCounter, entityScript);

        return m_entityUIDCounter;
    }

    public int server_registerPlayerEntity(Player_external entityScript, int entityUID)
    {
        //Debug.Log("EntityManager:server_registerPlayerEntity: ID given: "+ entityUID);

        m_gameID_playerScript.Add(entityScript.m_gameID, entityScript);

        if (m_entityID_entityScript.ContainsKey(entityUID))
        {
            Entity_base script = m_entityID_entityScript[entityUID];

            if (script != null)
            {
                Destroy(script.gameObject);
            }

            m_entityID_entityScript.Remove(entityUID);
        }

        m_entityID_entityScript.Add(entityUID, entityScript);

        return m_entityUIDCounter;
    }

    public void server_unregisterPlayerEntity(Player_external entityScript)
    {
        m_gameID_playerScript.Remove(entityScript.m_gameID);
        unregisterEntity(entityScript);
    }

    public int registerLocalPlayer(Player_local entityScript)
    {
        if (m_localPlayerScript != null)
        {
            unregisterLocalPlayer();
        }

        //Debug.Log("registerLocalPlayer " + Time.realtimeSinceStartup);

        m_localPlayerScript = entityScript;

        m_entityUIDCounter++;
        m_entityID_entityScript.Add(m_entityUIDCounter, entityScript);

        return m_entityUIDCounter;
    }

    public void client_registerLocalPlayer(Player_local entityScript, int entityUID)
    {
        if (m_localPlayerScript != null)
        {
            unregisterLocalPlayer();
        }

        //Debug.Log("registerLocalPlayer " + Time.realtimeSinceStartup);

        m_entityID_entityScript.Add(entityUID, entityScript);
        m_localPlayerScript = entityScript;
    }

    public void unregisterLocalPlayer()
    {
        //Debug.Log("unregisterLocalPlayer " + Time.realtimeSinceStartup);

        if (m_localPlayerScript != null)
        {
            unregisterEntity(m_localPlayerScript);
            m_localPlayerScript = null;
        }
    }

    #endregion

    #region Player Entity Methods

    public void client_setPlayerLocalForID(int entityID)
    {
        if (m_entityID_entityScript.ContainsKey(entityID))
        {
            if (m_entityID_entityScript[entityID].GetType() == typeof(Player_external))
            {
                Debug.Log("removing player external with UID for player local");
                m_entityID_entityScript.Remove(entityID); // falsely set due to entity creation from server
            }
        }

        if (m_localPlayerScript != null)
        {
            Debug.Log("setting player local for Entity UID");

            if (m_entityID_entityScript.ContainsKey(entityID))
            {
                Entity_base script = m_entityID_entityScript[entityID];

                if (script != null)
                {
                    Destroy(script.gameObject);
                }

                m_entityID_entityScript.Remove(entityID);
            }

            m_entityID_entityScript.Add(entityID, m_localPlayerScript);
        }
    }

    public void server_onPlayerDisconnect(int gameID)
    {
        //Debug.Log("EntityManager: server_onPlayerDisconnect");

        if (m_gameID_playerScript.ContainsKey(gameID))
        {
            DataEntity_Player playerData = m_gameID_playerScript[gameID].getUpdatedDataEntity() as DataEntity_Player;
            Player_external player = m_gameID_playerScript[gameID];

            //Debug.Log("EntityManager: server_onPlayerDisconnect: "+ playerData.m_playerGameID);

            unregisterEntity(player);
            m_gameID_playerScript.Remove(gameID);
            Destroy(player.gameObject);
            server_removePlayerAsObserver(gameID);

            server_spawnEntity(playerData); // spawn sleeper
        }
        else
        {
            Debug.Log("EntityManager: server_onPlayerDisconnect: couldnt find player-gameobject for gameID " + gameID);
        }
    }

    private void server_removePlayerAsObserver(int gameID) // from all entities
    {
        bool clientConnected = NetworkingManager.singleton.server_isClientConnected(gameID);

        if (clientConnected)
        {
            Debug.LogWarning("EntityManager: server_removePlayerAsObserver: player is still connected !");
        }
        else
        {
            foreach (KeyValuePair<int, Entity_base> ID_EntityPair in m_entityID_entityScript)
            {
                if (ID_EntityPair.Value.m_observingPlayersID.Contains(gameID))
                {
                    ID_EntityPair.Value.m_observingPlayersID.Remove(gameID);
                }
            }

            for (int i = 0; i < m_culledEntities.Count; i++)
            {
                if (m_culledEntities[i].m_observingDistantPlayers.Contains(gameID))
                {
                    m_culledEntities[i].m_observingDistantPlayers.Remove(gameID);
                }
            }
        }
    }

    public Dictionary<int, Player_external> getPlayerID_ExternalScriptsDict()
    {
        return m_gameID_playerScript;
    }

    public Player_external getPlayerExternal(int gameID)
    {
        if (m_gameID_playerScript.ContainsKey(gameID))
        {
            return m_gameID_playerScript[gameID];
        }
        else
        {
            return null;
        }
    }

    public List<Vector3> getPlayerExternalsPositions()
    {
        List<Vector3> returnValue = new List<Vector3>();

        foreach (KeyValuePair<int, Player_external> pair in m_gameID_playerScript)
        {
            returnValue.Add(pair.Value.transform.position);
        }

        return returnValue;
    }

    public List<Player_base> getAllActivePlayers() // no sleeper
    {
        List<Player_base> returnValue = new List<Player_base>();

        foreach (KeyValuePair<int, Player_external> pair in m_gameID_playerScript)
        {
            returnValue.Add(pair.Value);
        }

        if (m_localPlayerScript != null)
        {
            returnValue.Add(m_localPlayerScript);
        }

        return returnValue;
    }

    public List<Vector2> getPlayerExternalsPositionsXZ()
    {
        List<Vector2> returnValue = new List<Vector2>();

        foreach (KeyValuePair<int, Player_external> pair in m_gameID_playerScript)
        {
            returnValue.Add(new Vector2(pair.Value.transform.position.x, pair.Value.transform.position.z));
        }

        return returnValue;
    }

    public Player_base getActivePlayer(int gameID) // no sleeper
    {
        if (gameID == -1)
        {
            return getLocalPlayer();
        }
        else
        {
            return getPlayerExternal(gameID);
        }
    }

    public bool isLocalPlayerActive()
    {
        if (m_localPlayerScript != null && m_localPlayerScript.gameObject.activeSelf)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public Player_local getLocalPlayer()
    {
        return m_localPlayerScript;
    }

    public Vector3 getLocalPlayerPosition()
    {
        if (isLocalPlayerActive())
        {
            return m_localPlayerScript.transform.position;
        }
        else
        {
            return Vector3.zero;
        }
    }

    public bool isPlayerAlive(int gameID)
    {
        if (gameID == -1)
        {
            return isLocalPlayerActive();
        }
        else
        {
            return m_gameID_playerScript.ContainsKey(gameID);
        }
    }

    public bool tryRemovePlayerObject(int gameID)
    {
        if (gameID == -1)
        {
            if (m_localPlayerScript != null)
            {
                Destroy(m_localPlayerScript.gameObject);

                return true;
            }
            else
            {
                return false;
            }
        }
        if (m_gameID_playerScript.ContainsKey(gameID))
        {
            Destroy(m_gameID_playerScript[gameID]);
            m_gameID_playerScript.Remove(gameID);

            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gameID"></param>
    /// <returns>the entity id for a sleeping player, thats culled or -1 if nothing is found</returns>
    public DataEntity_Player getCulledSleepingPlayer(int gameID, bool remove = false)
    {
        for (int i = 0; i < m_culledEntities.Count; i++)
        {
            if (m_culledEntities[i].GetType() == typeof(DataEntity_Player))
            {
                DataEntity_Player sleeper = m_culledEntities[i] as DataEntity_Player;

                if (sleeper.m_playerGameID == gameID)
                {
                    if (remove)
                    {
                        m_culledEntities.RemoveAt(i);
                    }

                    return sleeper;
                }
            }
        }

        return null;
    }

    public Player_sleeper getSleepingPlayerEntity(int gameID)
    {
        foreach (KeyValuePair<int, Entity_base> pair in m_entityID_entityScript)
        {
            if (pair.Value.GetType() == typeof(Player_sleeper))
            {
                Player_sleeper player = pair.Value as Player_sleeper;

                if (player.m_gameID == gameID)
                {
                    return player;
                }
            }
        }

        return null;
    }

    public void transformSleepingPlayerActive(int gameID)
    {
        Player_sleeper sleeperEntity = getSleepingPlayerEntity(gameID);
        DataEntity_Player playerData;

        if (sleeperEntity == null)
        {
            playerData = getCulledSleepingPlayer(gameID, true);
        }
        else
        {
            playerData = sleeperEntity.getUpdatedDataEntity() as DataEntity_Player;
            sleeperEntity.m_suppressEntityUnregister = true;
            Destroy(sleeperEntity.gameObject);
        }

        if (playerData.m_playerGameID == -1)
        {
            Debug.LogWarning("EntityManager: transformSleepingPlayerActive: local player not supported");
        }
        else
        {
            GameObject player = Instantiate(m_entityPrefabs[5], playerData.m_position, playerData.m_rotation);

            Player_external playerScript = player.GetComponent<Player_external>();
            playerScript.setDataEntity(playerData);
            playerScript.onTransformedByWakeUp();
        }
    }

    #endregion

    #region Custom Entity Network Messages

    public void server_receivedCustomEntityMessage(NetworkMessage message)
    {
        if (message.integerValuesCount > 1)
        {
            int entityUID = message.getIntValue(0);

            Entity_base entityScript = null;

            if (m_entityID_entityScript.TryGetValue(entityUID, out entityScript))
            {
                entityScript.server_receivedCustomNetworkMessage(message);
            }
            else
            {
                Debug.LogWarning("EntityManager: server_receivedCustomEntityMessage: could'nt find entity for ID " + entityUID);
            }
        }
        else
        {
            Debug.LogWarning("EntityManager: server_receivedCustomEntityMessage: not enought integer values provieded within this network message. need at least 2 values (0:entity ID, 1: custom message context)");
        }
    }

    public void client_receivedCustomEntityMessage(NetworkMessage message)
    {
        //Debug.Log("client_receivedCustomEntityMessage: " + message.ToString());

        if (message.integerValuesCount > 1)
        {
            int entityUID = message.getIntValue(0);

            Entity_base entityScript = null;

            if (m_entityID_entityScript.TryGetValue(entityUID, out entityScript))
            {
                entityScript.client_receivedCustomNetworkMessage(message);
            }
            else
            {
                Debug.LogWarning("EntityManager: client_receivedCustomEntityMessage: could'nt find entity for ID " + entityUID);
            }
        }
        else
        {
            Debug.LogWarning("EntityManager: client_receivedCustomEntityMessage: not enought integer values provieded within this network message. need at least 2 values (0:entity ID, 1: custom message context)");
        }
    }

    #endregion

    #region Special Entity Interfaces

    private DataEntity_Base getCulledEntity(int entityUID)
    {
        // NOT VERY EFFECTIVE !!!

        for (int i = 0; i < m_culledEntities.Count; i++)
        {
            if (m_culledEntities[i].m_entity_UID == entityUID)
            {
                return m_culledEntities[i];
            }
        }

        return null;
    }

    public void server_onEntityUsed(int gameID, int entityUID)
    {
        if (m_entityID_entityScript.ContainsKey(entityUID))
        {
            m_entityID_entityScript[entityUID].server_onClientUse(gameID);
        }
    }

    public void server_closeContainerForClient(int gameID, int containerID)
    {
        Entity_base entityScript;

        if (m_entityID_entityScript.TryGetValue(containerID, out entityScript))
        {
            LootContainerGame lootContainer = entityScript as LootContainerGame;

            if (lootContainer != null)
            {
                lootContainer.server_unregisterPlayerUsing(gameID);
                NetworkingManager.singleton.server_sendContainerCloseCommand(gameID, lootContainer.getEntityUID());
            }
        }
    }

    public void server_switchPlayerInventoryItems(int gameID, int containerID, GUIRaycastIdentifier.Type sourceType, int sourceIndex, GUIRaycastIdentifier.Type targetType, int targetIndex)
    {
        if (m_gameID_playerScript.ContainsKey(gameID))
        {
            m_gameID_playerScript[gameID].server_switchIventoryItems(containerID, sourceType, sourceIndex, targetType, targetIndex);
        }
        else if (gameID == -1) // local player
        {
            if (m_localPlayerScript != null)
            {
                m_localPlayerScript.server_switchIventoryItems(containerID, sourceType, sourceIndex, targetType, targetIndex);
            }
        }
    }

    public void server_SplitInventoryItems(int gameID, int containerID, GUIRaycastIdentifier.Type sourceType, int sourceIndex)
    {
        if (m_gameID_playerScript.ContainsKey(gameID))
        {
            m_gameID_playerScript[gameID].server_SplitIventoryItems(containerID, sourceType, sourceIndex);
        }
        else if (gameID == -1) // local player
        {
            if (m_localPlayerScript != null)
            {
                m_localPlayerScript.server_SplitIventoryItems(containerID, sourceType, sourceIndex);
            }
        }
    }

    public void server_setLootContainerForClient(int gameID, LootContainerGame container)
    {
        if (m_gameID_playerScript.ContainsKey(gameID))
        {
            m_gameID_playerScript[gameID].setOpenContainer(container);
        }
        else if (gameID == -1) // local player
        {
            if (m_localPlayerScript != null)
            {
                m_localPlayerScript.setOpenContainer(container);
            }
        }
    }

    public void server_spawnDroppedItemWorldPlayer(int prefabIndex, StorableItem item, Vector3 position, Vector3 forward)
    {
        if (item != null)
        {
            if (prefabIndex > -1 && prefabIndex < m_entityPrefabs.Length)
            {
                forward = forward.normalized;

                GameObject obj = Instantiate(m_entityPrefabs[prefabIndex], position + Vector3.up * m_itemDropHeight + forward * m_itemDropForward, Quaternion.identity);
                DroppedItemWorld droppedItemScript = obj.GetComponent<DroppedItemWorld>();
                droppedItemScript.setItem(item);
                droppedItemScript.addForce(forward * m_itemDropSpeed);
            }
        }
    }

    public void server_spawnDroppedItemWorld(StorableItem item, Vector3 position)
    {
        server_spawnDroppedItemWorld(item.WorldPrefabIndex, item, position);
    }
    public void server_spawnDroppedItemWorld(int prefabIndex, StorableItem item, Vector3 position)
    {
        if (item != null)
        {
            if (prefabIndex > -1 && prefabIndex < m_entityPrefabs.Length)
            {
                GameObject obj = Instantiate(m_entityPrefabs[prefabIndex], position, Quaternion.identity);
                DroppedItemWorld droppedItemScript = obj.GetComponent<DroppedItemWorld>();
                droppedItemScript.setItem(item);
            }
        }
    }

    public void server_dropItemRequest(int gameID, GUIRaycastIdentifier.Type type, int itemPos, int contaierUID)
    {
        if (m_gameID_playerScript.ContainsKey(gameID))
        {
            StorableItem itemToDrop = m_gameID_playerScript[gameID].server_removeItem(type, itemPos, contaierUID);
            if (itemToDrop != null)
            {
                server_spawnDroppedItemWorldPlayer(itemToDrop.WorldPrefabIndex, itemToDrop, m_gameID_playerScript[gameID].transform.position, m_gameID_playerScript[gameID].transform.forward);
            }
        }
        else if (gameID == -1 && m_localPlayerScript != null) // local player
        {
            StorableItem itemToDrop = m_localPlayerScript.server_removeItem(type, itemPos, contaierUID);
            if (itemToDrop != null)
            {
                server_spawnDroppedItemWorldPlayer(itemToDrop.WorldPrefabIndex, itemToDrop, m_localPlayerScript.transform.position, m_localPlayerScript.transform.forward);
            }
        }
    }

    public void server_itemPickUpRequest(int playerGameID, int itemEntityUID)
    {
        Debug.Log("server_itemPickUpRequest");

        if (m_entityID_entityScript.ContainsKey(itemEntityUID))
        {
            DroppedItemWorld worldItem = m_entityID_entityScript[itemEntityUID] as DroppedItemWorld;

            if (worldItem != null)
            {
                StorableItem item = worldItem.getItem();

                if (m_gameID_playerScript.ContainsKey(playerGameID))
                {
                    if (m_gameID_playerScript[playerGameID].tryAddItem(item))
                    {
                        Destroy(worldItem.gameObject);
                    }
                }
                else if (playerGameID == -1 && isLocalPlayerActive())
                {
                    if (m_localPlayerScript.tryAddItem(item))
                    {
                        Destroy(worldItem.gameObject);
                    }
                }
            }
        }
    }

    public void client_receiveOpenContainerCommand(int entityUID)
    {
        if (m_localPlayerScript != null)
        {
            m_localPlayerScript.onReceiveOpenContainerCommand(entityUID);
        }
    }

    public void client_receiveCloseContainerCommand(int entityUID)
    {
        if (m_localPlayerScript != null)
        {
            m_localPlayerScript.onReceiveCloseContainerCommand(entityUID);
        }
    }

    public LootContainerGame client_getLootContainerGame(int entityUID)
    {
        if (m_entityID_entityScript.ContainsKey(entityUID))
        {
            return m_entityID_entityScript[entityUID] as LootContainerGame;
        }
        else
        {
            return null;
        }
    }

    public void client_receiveItemUpdate(GUIRaycastIdentifier.Type type, int containerUID, int containerPos, StorableItem item)
    {
        if (m_localPlayerScript != null)
        {
            m_localPlayerScript.setInventoryItem(type, containerUID, containerPos, item);
        }
    }

    public void client_resetManager()
    {
        foreach (KeyValuePair<int, Entity_base> entity in m_entityID_entityScript)
        {
            if (entity.Value != null && entity.Value.gameObject != null)
            {
                Destroy(entity.Value.gameObject);
            }
        }

        m_entityID_entityScript.Clear();
        m_gameID_playerScript.Clear();
        m_culledEntities.Clear();

        foreach (KeyValuePair<int, Dictionary<int, DistantEntityData>> keyValuePair in m_prefabIndex_distantEntities)
        {
            keyValuePair.Value.Clear();
        }

        foreach (KeyValuePair<int, Dictionary<int, DistantEntityData>> prefabIndex_DictEntities in m_prefabIndex_distantEntities)
        {
            m_distantEntityDrawer.setInstancesDataForPrefab(prefabIndex_DictEntities.Key, new List<Vector3>(), new List<Quaternion>(), Vector3.zero);
        }

        m_distantEntitiesDirty = true; // client only
        m_lastViewDistanceClient = 0;
        m_lastPlayerPosInstancingUpdate = Vector3.zero;

        m_localPlayerScript = null;
    }

    #endregion

    #region Manager Input/Output

    public GameObject spawnEntity(int entityPrefabIndex, Vector3 position)
    {
        return spawnEntity(entityPrefabIndex, position, Quaternion.identity);
    }
    public GameObject spawnEntity(int entityPrefabIndex, Vector3 position, Quaternion rotation)
    {
        if (entityPrefabIndex > -1 && entityPrefabIndex < m_entityPrefabs.Length)
        {
            return Instantiate(m_entityPrefabs[entityPrefabIndex], position, rotation);
        }
        else
        {
            Debug.LogWarning("EntityManager: spawnEntity: entity index out of range: " + entityPrefabIndex + ", range:[0-" + m_entityPrefabs.Length + ")");
            return null;
        }
    }

    public Entity_base getEntity(int entityID)
    {
        if (m_entityID_entityScript.ContainsKey(entityID))
        {
            return m_entityID_entityScript[entityID];
        }
        else
        {
            return null;
        }
    }

    public Entity_base getEntityPrefab(int prefabID)
    {
        return m_entityPrefabs[prefabID].GetComponent<Entity_base>();
    }

    public void getSpawnpoints(int playerGameID, out List<Vector3> spawnPositions)
    {
        List<int> entityID;
        List<float> cooldownTimes;

        getSpawnpoints(playerGameID, out entityID, out cooldownTimes, out spawnPositions);
    }
    public void getSpawnpoints(int playerGameID, out List<int> entityID, out List<float> cooldownTimes, out List<Vector3> spawnPositions)
    {
        entityID = new List<int>();
        cooldownTimes = new List<float>();
        spawnPositions = new List<Vector3>();

        ClientUserData userData = NetworkingManager.singleton.server_getClientUserData(playerGameID);

        if (userData != null)
        {
            List<int> spawnpointsIDs = userData.playerSpawnpointsEntityIDs;

            for (int i = 0; i < spawnpointsIDs.Count; i++)
            {
                entityID.Add(spawnpointsIDs[i]);

                Entity_SpawnpointPlayer activeEntity = getEntity(spawnpointsIDs[i]) as Entity_SpawnpointPlayer;

                if (activeEntity == null)
                {
                    DataEntity_PlayerSpawnPoint culledEntity = getCulledEntity(spawnpointsIDs[i]) as DataEntity_PlayerSpawnPoint;

                    if (culledEntity == null)
                    {
                        Debug.LogWarning("EntityManager: getSpawnpoints: entity not found");
                        entityID.Remove(spawnpointsIDs[i]);
                    }
                    else
                    {
                        cooldownTimes.Add(culledEntity.m_lastTimeSpawned + (m_entityPrefabsScripts[culledEntity.m_entityPrefabID] as Entity_SpawnpointPlayer).cooldownTime - Time.time);
                        spawnPositions.Add(culledEntity.m_position + culledEntity.m_rotation * (m_entityPrefabsScripts[culledEntity.m_entityPrefabID] as Entity_SpawnpointPlayer).spawnOffset);
                    }
                }
                else
                {
                    cooldownTimes.Add(activeEntity.lastTimeSpawned + activeEntity.cooldownTime - Time.time);
                    spawnPositions.Add(activeEntity.transform.position + activeEntity.transform.rotation * activeEntity.spawnOffset);
                }
            }
        }
    }

    public void setSpawnpointCooldown(int spawnpointEntityID)
    {
        Entity_SpawnpointPlayer activeEntity = getEntity(spawnpointEntityID) as Entity_SpawnpointPlayer;

        if (activeEntity == null)
        {
            DataEntity_PlayerSpawnPoint culledEntity = getCulledEntity(spawnpointEntityID) as DataEntity_PlayerSpawnPoint;

            if (culledEntity == null)
            {
                Debug.LogWarning("EntityManager: setSpawnpointCooldown: entity not found");
            }
            else
            {
                culledEntity.m_lastTimeSpawned = Time.time;
                NetworkingManager.singleton.server_sendSpawnpointCooldown(
                                                                            culledEntity.m_playerGameID,
                                                                            culledEntity.m_entity_UID,
                                                                            culledEntity.m_lastTimeSpawned + (m_entityPrefabsScripts[culledEntity.m_entityPrefabID] as Entity_SpawnpointPlayer).cooldownTime - Time.time
                                                                            );
            }
        }
        else
        {
            activeEntity.setSpawnCooldown();

            NetworkingManager.singleton.server_sendSpawnpointCooldown(
                                                                            activeEntity.associatedPlayerGameID,
                                                                            activeEntity.entityUID,
                                                                            activeEntity.lastTimeSpawned + activeEntity.cooldownTime - Time.time
                                                                            );
        }
    }

    /// <summary>
    /// is this entity active (unculled)
    /// </summary>
    /// <param name="entityUID"></param>
    /// <returns></returns>
    public bool isEntityActive(int entityUID)
    {
        if (m_entityID_entityScript.ContainsKey(entityUID))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public int countEntites(int prefabIndex)
    {
        int returnValue = 0;

        foreach (KeyValuePair<int, Entity_base> pair in m_entityID_entityScript)
        {
            if (pair.Value.prefabIndex == prefabIndex)
            {
                returnValue++;
            }
        }

        for (int i = 0; i < m_culledEntities.Count; i++)
        {
            if (m_culledEntities[i].m_entityPrefabID == prefabIndex)
            {
                returnValue++;
            }
        }

        return returnValue;
    }

    public void onCreateWorld(float sizeX, float sizeY)
    {
        m_worldGrid = new WorldGrid(sizeX + m_serverPlayerViewDistanceMax*2, sizeY+ m_serverPlayerViewDistanceMax*2, m_serverPlayerViewDistanceMax/2, -m_serverPlayerViewDistanceMax, -m_serverPlayerViewDistanceMax); // worldsize + one border field around actual world-fields (so that beach fields have no direct connection to out of range field (which is expensive))

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            m_worldGrid.GridFieldNoMoreEntityViewersEvent += server_gridFieldNoMoreEntityViewers;
            m_worldGrid.GridFieldEntityViewerLeftEvent += sever_gridFieldEntityViewerLeftEvent;
        }
    }

    public WorldGridField getGridFieldForPostion(Vector2 position)
    {
        return m_worldGrid.getFieldForPosition(position);
    }

    public WorldGridField getGridFieldForUID(int uid)
    {
        return m_worldGrid.getField(uid);
    }

    #endregion

}
