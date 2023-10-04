using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerConstruction : Entity_damageable
{
    public const float EDGE_LENGTH = 3; // length of an egde that another building part can attached to
    private const float STABILITY_REDUCTION_VERTICAL = 2f;
    private const float STABILITY_REDUCTION_HORIZONTAL = 40f;

    public enum BuildingPartType { Default, Foundation, Wall, Floor, DoorFrame, Door } // TODO: Move to Class "BuildingSocket"

    [Header("PlayerConstruction")]
    [SerializeField] private BuildingPartType m_buildingType = BuildingPartType.Default;
    [SerializeField, ReadOnly] public float m_stability = -1;
    [SerializeField] private bool m_DEBUG_showSocketPositions = false;
    [SerializeField] private bool m_DEBUG_showSocketPositionsConstant = false;
    [SerializeField, ReadOnly] private int m_builtByPlayerGameID = -1;

    public PlayerBuilding m_associatedBuilding;
    public List<int>[] m_connectedBuildPartsEntityID;
    private Vector3[] m_buildingSocketsPositionsTransposed;
    private Quaternion[] m_buildingSocketsRotationsTransposed;
    private bool m_updateBuildingStability = true; // on destroy
    //private bool m_isDestroyed = false;

    public BuildingPartType buildingType { get { return m_buildingType; } }
    public int builtByPlayerGameID { get { return m_builtByPlayerGameID; } set { m_builtByPlayerGameID = value; } }
    //public bool isDestroyed { get { return m_isDestroyed; } }

    protected void Awake()
    {
        base.Awake();

        BuildingSocket[] templates = getBuildingSocketTemplate();

        m_connectedBuildPartsEntityID = new List<int>[templates.Length];
        for (int i = 0; i < m_connectedBuildPartsEntityID.Length; i++)
        {
            m_connectedBuildPartsEntityID[i] = new List<int>();
        }

        m_buildingSocketsPositionsTransposed = new Vector3[templates.Length];
        m_buildingSocketsRotationsTransposed = new Quaternion[templates.Length];
    }

    protected void Start()
    {
        base.Start();

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            PlayerBuildingManager.singleton.server_onBuildingPartSpawned(this);
        }
    }

    protected void Update()
    {
        if (m_DEBUG_showSocketPositions || m_DEBUG_showSocketPositionsConstant)
        {
            m_DEBUG_showSocketPositions = false;
            DEBUG_showSocketPositions();
        }
    }

    protected void OnDestroy()
    {
        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            if (m_associatedBuilding == null)
            {
                Debug.LogWarning("PlayerConstruction: OnDestroy: m_associatedBuilding = null. EntityID: " + entityUID + "; building type: " + m_buildingType);
            }
            else
            {
                m_associatedBuilding.onBuildingPartDestroyed(this, m_updateBuildingStability);
            }
        }

        base.OnDestroy();

        //m_isDestroyed = true;
    }

    protected virtual BuildingSocket[] getBuildingSocketTemplate()
    {
        throw new System.NotImplementedException("this method may only be called on a inherited class !");
    }

    public void setAssociatedBuilding(PlayerBuilding building)
    {
        m_associatedBuilding = building;
    }

    public void addConnectionToSocket(int socketIndex, PlayerConstruction buildingPart)
    {
        if (!m_connectedBuildPartsEntityID[socketIndex].Contains(socketIndex))
        {
            m_connectedBuildPartsEntityID[socketIndex].Add(buildingPart.entityUID);
        }
    }

    public void removeConnectionToSockets(int entityID)
    {
        for (int i = 0; i < m_connectedBuildPartsEntityID.Length; i++)
        {
            m_connectedBuildPartsEntityID[i].Remove(entityID);
        }
    }

    public void getBuildingSockets(out BuildingSocket[] socketsTemplate, out Vector3[] buildingSocketsPositions, out Quaternion[] buildingSocketsRotations, out List<int>[] connectedBuildPartsEntityID)
    {
        updateBuildingSocketsTransposed();

        socketsTemplate = getBuildingSocketTemplate();
        buildingSocketsPositions = m_buildingSocketsPositionsTransposed;
        buildingSocketsRotations = m_buildingSocketsRotationsTransposed;
        connectedBuildPartsEntityID = m_connectedBuildPartsEntityID;
    }
    public void getBuildingSockets(out BuildingSocket[] socketsTemplate, out Vector3[] buildingSocketsPositions)
    {
        updateBuildingSocketsTransposed();

        socketsTemplate = getBuildingSocketTemplate();
        buildingSocketsPositions = m_buildingSocketsPositionsTransposed;
    }

    public void destroyBuildingPart(bool updateBuildingStability)
    {
        m_updateBuildingStability = updateBuildingStability;
        //if (!isDestroyed)
        //{
            Destroy(gameObject);
        //    m_isDestroyed = true;
        //}
    }

    public void recalculateStability(bool triggerNeighbors)
    {
        //Debug.Log("recalculateStability: " + entityUID);

        float startStability = m_stability;

        switch (m_buildingType)
        {
            case BuildingPartType.Foundation:
                {
                    m_stability = 100f;
                    break;
                }
            case BuildingPartType.Wall:
                {
                    PlayerConstruction bestConnectedWallBelow = null;
                    PlayerConstruction bestConnectedFloorBelow = null;
                    bool connectedWithFoundation = false;

                    for (int i = 0; i < m_connectedBuildPartsEntityID.Length; i++)
                    {
                        for (int j = 0; j < m_connectedBuildPartsEntityID[i].Count; j++)
                        {
                            PlayerConstruction connectedBuildingPart = EntityManager.singleton.getEntity(m_connectedBuildPartsEntityID[i][j]) as PlayerConstruction;

                            if (connectedBuildingPart != null)
                            {
                                if (connectedBuildingPart.buildingType == BuildingPartType.Foundation)
                                {
                                    connectedWithFoundation = true;
                                    goto LOOP_END;
                                }
                                else if (connectedBuildingPart.buildingType == BuildingPartType.Wall)
                                {
                                    if (connectedBuildingPart.transform.position.y < transform.position.y)
                                    {
                                        if (bestConnectedWallBelow == null)
                                        {
                                            bestConnectedWallBelow = connectedBuildingPart;
                                        }
                                        else
                                        {
                                            if (connectedBuildingPart.m_stability > bestConnectedWallBelow.m_stability)
                                            {
                                                bestConnectedWallBelow = connectedBuildingPart;
                                            }
                                        }
                                    }
                                }
                                else if (connectedBuildingPart.buildingType == BuildingPartType.Floor)
                                {
                                    if (connectedBuildingPart.transform.position.y < transform.position.y)
                                    {
                                        if (bestConnectedFloorBelow == null)
                                        {
                                            bestConnectedFloorBelow = connectedBuildingPart;
                                        }
                                        else
                                        {
                                            if (connectedBuildingPart.m_stability > bestConnectedFloorBelow.m_stability)
                                            {
                                                bestConnectedFloorBelow = connectedBuildingPart;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                LOOP_END:

                    if (connectedWithFoundation)
                    {
                        m_stability = 100 - STABILITY_REDUCTION_VERTICAL;
                    }
                    else if (bestConnectedWallBelow != null)
                    {
                        m_stability = bestConnectedWallBelow.m_stability - STABILITY_REDUCTION_VERTICAL;
                    }
                    else if (bestConnectedFloorBelow != null)
                    {
                        m_stability = bestConnectedFloorBelow.m_stability - STABILITY_REDUCTION_VERTICAL;
                    }
                    else // no connection at all
                    {
                        m_stability = 0;
                    }
                    break;
                }
            case BuildingPartType.Floor:
                {
                    PlayerConstruction bestConnectedWallBelow = null;
                    PlayerConstruction bestConnectedFloor = null;

                    for (int i = 0; i < m_connectedBuildPartsEntityID.Length; i++)
                    {
                        for (int j = 0; j < m_connectedBuildPartsEntityID[i].Count; j++)
                        {
                            PlayerConstruction connectedBuildingPart = EntityManager.singleton.getEntity(m_connectedBuildPartsEntityID[i][j]) as PlayerConstruction;

                            if (connectedBuildingPart != null)
                            {
                                if (connectedBuildingPart.buildingType == BuildingPartType.Foundation)
                                {
                                    Debug.LogWarning("PlayerConstruction: recalculateStability: " + entityUID + ": Floor connected with Foundation.");
                                }
                                else if (connectedBuildingPart.buildingType == BuildingPartType.Wall)
                                {
                                    if (connectedBuildingPart.transform.position.y < transform.position.y)
                                    {
                                        if (bestConnectedWallBelow == null)
                                        {
                                            bestConnectedWallBelow = connectedBuildingPart;
                                        }
                                        else
                                        {
                                            if (connectedBuildingPart.m_stability > bestConnectedWallBelow.m_stability)
                                            {
                                                bestConnectedWallBelow = connectedBuildingPart;
                                            }
                                        }
                                    }
                                }
                                else if (connectedBuildingPart.buildingType == BuildingPartType.Floor)
                                {
                                    if (bestConnectedFloor == null)
                                    {
                                        bestConnectedFloor = connectedBuildingPart;
                                    }
                                    else
                                    {
                                        if (connectedBuildingPart.m_stability > bestConnectedFloor.m_stability)
                                        {
                                            bestConnectedFloor = connectedBuildingPart;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (bestConnectedWallBelow != null)
                    {
                        m_stability = bestConnectedWallBelow.m_stability - STABILITY_REDUCTION_VERTICAL;
                    }
                    else if (bestConnectedFloor != null)
                    {
                        m_stability = bestConnectedFloor.m_stability - STABILITY_REDUCTION_HORIZONTAL;
                    }
                    else // no connection at all
                    {
                        m_stability = 0;
                    }

                    break;
                }
            default:
                {
                    throw new System.NotImplementedException();
                    break;
                }
        }

        //Debug.Log("updated stability: " + entityUID);
        //Debug.DrawRay(transform.position, Vector3.up, Color.red, 2f);

        if (triggerNeighbors)
        {
            if (startStability != m_stability)
            {
                for (int i = 0; i < m_connectedBuildPartsEntityID.Length; i++)
                {
                    for (int j = 0; j < m_connectedBuildPartsEntityID[i].Count; j++)
                    {
                        PlayerConstruction connectedBuildingPart = EntityManager.singleton.getEntity(m_connectedBuildPartsEntityID[i][j]) as PlayerConstruction;

                        if (connectedBuildingPart != null)
                        {
                            connectedBuildingPart.recalculateStability(true);
                        }
                    }
                }
            }
        }

        if (m_stability <= 0)
        {
            Debug.Log("destroying 0 stability ! " + entityUID);

            Destroy(gameObject);
           // m_isDestroyed = true;
        }
    }

    private void updateBuildingSocketsTransposed()
    {
        BuildingSocket[] socketTemplates = getBuildingSocketTemplate();

        for (int i = 0; i < m_buildingSocketsPositionsTransposed.Length; i++)
        {
            m_buildingSocketsPositionsTransposed[i] = transform.rotation * socketTemplates[i].m_socketPositionOffset + transform.position;
            m_buildingSocketsRotationsTransposed[i] = transform.rotation * socketTemplates[i].m_socketRotation;
        }
    }

    private void DEBUG_showSocketPositions()
    {
        updateBuildingSocketsTransposed();

        int colorCounter = 0;

        for (int i = 0; i < m_buildingSocketsPositionsTransposed.Length; i++)
        {
            Debug.DrawRay(m_buildingSocketsPositionsTransposed[i], Vector3.up, Color.red, 2f);
            Debug.DrawRay(m_buildingSocketsPositionsTransposed[i], Vector3.down, Color.green, 2f);
            Debug.DrawRay(m_buildingSocketsPositionsTransposed[i], m_buildingSocketsRotationsTransposed[i] * Vector3.forward, Color.blue, 2f);

            Color edgeColor;

            if (colorCounter == 0)
            {
                edgeColor = Color.yellow;
                colorCounter = 1;
            }
            else if (colorCounter == 1)
            {
                edgeColor = Color.magenta;
                colorCounter = 2;
            }
            else
            {
                edgeColor = Color.cyan;
                colorCounter = 0;
            }

            Debug.DrawRay(m_buildingSocketsPositionsTransposed[i], m_buildingSocketsRotationsTransposed[i] * Vector3.right * EDGE_LENGTH / 2, edgeColor, 2f);
            Debug.DrawRay(m_buildingSocketsPositionsTransposed[i], m_buildingSocketsRotationsTransposed[i] * Vector3.left * EDGE_LENGTH / 2, edgeColor, 2f);

            Debug.DrawRay(m_buildingSocketsPositionsTransposed[i] + m_buildingSocketsRotationsTransposed[i] * Vector3.right * EDGE_LENGTH / 2, Vector3.down, edgeColor, 2f);
            Debug.DrawRay(m_buildingSocketsPositionsTransposed[i] + m_buildingSocketsRotationsTransposed[i] * Vector3.right * EDGE_LENGTH / 2, Vector3.up, edgeColor, 2f);

            Debug.DrawRay(m_buildingSocketsPositionsTransposed[i] + m_buildingSocketsRotationsTransposed[i] * Vector3.left * EDGE_LENGTH / 2, Vector3.down, edgeColor, 2f);
            Debug.DrawRay(m_buildingSocketsPositionsTransposed[i] + m_buildingSocketsRotationsTransposed[i] * Vector3.left * EDGE_LENGTH / 2, Vector3.up, edgeColor, 2f);
        }

        int counter1 = 0;

        for (int i = 0; i < m_connectedBuildPartsEntityID.Length; i++)
        {
            for (int j = 0; j < m_connectedBuildPartsEntityID[i].Count; j++)
            {
                PlayerConstruction connectedBuildingPart = EntityManager.singleton.getEntity(m_connectedBuildPartsEntityID[i][j]) as PlayerConstruction;

                if (connectedBuildingPart != null)
                {
                    counter1++;
                    Debug.DrawLine(transform.position, connectedBuildingPart.transform.position, Color.magenta, 2f);
                }
            }
        }

        Debug.Log("conencted buildings: " + counter1);
    }

    protected override void updateDataEntity()
    {
        base.updateDataEntity();

        DataEntity_BuildingPart dataEntity = m_dataEntity as DataEntity_BuildingPart;

        dataEntity.m_associatedBuildingUID = m_associatedBuilding.buildingUID;
        dataEntity.m_connectedBuildPartsEntityID = m_connectedBuildPartsEntityID;
        dataEntity.m_stability = m_stability;
        dataEntity.m_builtByPlayerGameID = m_builtByPlayerGameID;
    }

    protected override void updateEntity()
    {
        base.updateEntity();

        DataEntity_BuildingPart dataEntity = m_dataEntity as DataEntity_BuildingPart;

        m_associatedBuilding = PlayerBuildingManager.singleton.getPlayerBuildingForUID(dataEntity.m_associatedBuildingUID);
        m_connectedBuildPartsEntityID = dataEntity.m_connectedBuildPartsEntityID;
        m_stability = dataEntity.m_stability;
        m_builtByPlayerGameID = dataEntity.m_builtByPlayerGameID;
    }

    public override DataEntity_Base getNewDefaultDataEntity()
    {
        return new DataEntity_BuildingPart();
    }
}
