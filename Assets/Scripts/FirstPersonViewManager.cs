using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonViewManager : MonoBehaviour
{
    private static FirstPersonViewManager m_singleton = null;

    public static FirstPersonViewManager singleton
    {
        get
        {
            return m_singleton;
        }
    }

    [SerializeField] private int m_playerHotbarSize = 6;
    [SerializeField] private GameObject m_player_FPV_ParentPrefab;
    [SerializeField] private GameObject m_FPVArmsPrefab;
    [SerializeField] private Vector3 m_PFVArmsPositionOffset;
    [SerializeField] private Vector3 m_PFVArmsRotationOffset;
    [SerializeField] private GameObject[] m_FPVItemsPrefab;

    private Dictionary<int, GameObject> m_gameID_FPVItemGameObjectsParents = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject[]> m_gameID_FPVItemGameObjects = new Dictionary<int, GameObject[]>();
    private Dictionary<int, FPVItem_Base[]> m_gameID_FPVItemScripts = new Dictionary<int, FPVItem_Base[]>();
    private Dictionary<int, int[]> m_gameID_FPVItemTemplateID = new Dictionary<int, int[]>();
    private GameObject m_firstPersonArms = null;
    private GameObject m_firstPersonArmsItemSocket = null;
    private Renderer m_firstPersonArmsRenderer = null;
    private Animator m_firstPersonArmsAnimator = null;

    public Animator firstPersonArmsAnimator { get { return m_firstPersonArmsAnimator; } }

    private void Awake()
    {
        m_singleton = this;
    }

    #region On Player Spawned

    public void server_onPlayerExternalSpawned(Player_external script)
    {
        if (m_gameID_FPVItemGameObjectsParents.ContainsKey(script.m_gameID))
        {
            m_gameID_FPVItemGameObjectsParents.Remove(script.m_gameID);
            m_gameID_FPVItemGameObjects.Remove(script.m_gameID);
            m_gameID_FPVItemScripts.Remove(script.m_gameID);
            m_gameID_FPVItemTemplateID.Remove(script.m_gameID);
        }

        GameObject parent = Instantiate(m_player_FPV_ParentPrefab, script.transform.transform.position, Quaternion.identity) as GameObject;
        parent.transform.SetParent(script.transform);

        m_gameID_FPVItemGameObjectsParents.Add(script.m_gameID, parent);
        m_gameID_FPVItemGameObjects.Add(script.m_gameID, new GameObject[m_playerHotbarSize]);
        m_gameID_FPVItemScripts.Add(script.m_gameID, new FPVItem_Base[m_playerHotbarSize]);
        m_gameID_FPVItemTemplateID.Add(script.m_gameID, new int[m_playerHotbarSize]);
        for (int i = 0; i < m_gameID_FPVItemTemplateID[script.m_gameID].Length; i++)
        {
            m_gameID_FPVItemTemplateID[script.m_gameID][i] = -1;
        }
    }

    public void client_onLocalPlayerSpawned(Player_local script)
    {
        if (m_gameID_FPVItemGameObjectsParents.ContainsKey(script.m_gameID))
        {
            Destroy(m_gameID_FPVItemGameObjectsParents[script.m_gameID]);

            m_gameID_FPVItemGameObjectsParents.Remove(script.m_gameID);
            m_gameID_FPVItemGameObjects.Remove(script.m_gameID);
            m_gameID_FPVItemScripts.Remove(script.m_gameID);
            m_gameID_FPVItemTemplateID.Remove(script.m_gameID);
        }

        GameObject parent = Instantiate(m_player_FPV_ParentPrefab, script.transform.transform.position, Quaternion.identity) as GameObject;
        parent.transform.SetParent(CameraStack.gameobject.transform);
        parent.transform.localPosition = Vector3.zero;
        parent.transform.localRotation = Quaternion.identity;

        m_gameID_FPVItemGameObjectsParents.Add(script.m_gameID, parent);
        m_gameID_FPVItemGameObjects.Add(script.m_gameID, new GameObject[m_playerHotbarSize]);
        m_gameID_FPVItemScripts.Add(script.m_gameID, new FPVItem_Base[m_playerHotbarSize]);
        m_gameID_FPVItemTemplateID.Add(script.m_gameID, new int[m_playerHotbarSize]);
        for (int i = 0; i < m_gameID_FPVItemTemplateID[script.m_gameID].Length; i++)
        {
            m_gameID_FPVItemTemplateID[script.m_gameID][i] = -1;
        }

        m_firstPersonArms = Instantiate(m_FPVArmsPrefab, script.transform.transform.position, Quaternion.identity) as GameObject;
        m_firstPersonArms.transform.SetParent(parent.transform);
        m_firstPersonArms.transform.localPosition = m_PFVArmsPositionOffset;
        m_firstPersonArms.transform.localRotation = Quaternion.Euler(m_PFVArmsRotationOffset);

        m_firstPersonArmsRenderer = findNameInChildren(m_firstPersonArms, "Arms_Model").GetComponent<Renderer>();

        m_firstPersonArmsItemSocket = findNameInChildren(m_firstPersonArms, "WeaponSocket");

        for (int i = 0; i < m_firstPersonArms.transform.childCount; i++)
        {
            Animator animator = m_firstPersonArms.transform.GetChild(i).GetComponent<Animator>();

            if (animator != null)
            {
                m_firstPersonArmsAnimator = animator;
                break;
            }
        }

        m_firstPersonArmsRenderer.enabled = false;
    }

    #endregion

    #region Hotbar Items

    public void server_onHotBarItemChanged(Player_external player, StorableItem item, int index)
    {
        if (item == null || m_gameID_FPVItemTemplateID[player.m_gameID][index] != item.itemTemplateIndex) // this prevents script reset if only item-count gets changed. reloading script resets variables for example cooldowns
        {
            if (m_gameID_FPVItemGameObjects[player.m_gameID][index] != null)
            {
                Destroy(m_gameID_FPVItemGameObjects[player.m_gameID][index]);
                m_gameID_FPVItemScripts[player.m_gameID][index] = null;
                m_gameID_FPVItemTemplateID[player.m_gameID][index] = -1;
            }

            if (item != null)
            {
                int prefabIndex = item.itemTemplateIndex;

                if (prefabIndex < m_FPVItemsPrefab.Length && prefabIndex >= 0)
                {
                    if (m_FPVItemsPrefab[prefabIndex] != null)
                    {
                        GameObject FPVItem = Instantiate(m_FPVItemsPrefab[prefabIndex]) as GameObject;
                        FPVItem.transform.SetParent(m_gameID_FPVItemGameObjectsParents[player.m_gameID].transform);

                        m_gameID_FPVItemGameObjects[player.m_gameID][index] = FPVItem;
                        FPVItem_Base script = FPVItem.GetComponent<FPVItem_Base>();
                        m_gameID_FPVItemScripts[player.m_gameID][index] = script;
                        m_gameID_FPVItemTemplateID[player.m_gameID][index] = item.itemTemplateIndex;

                        script.setHotbarIndex(index);
                        script.setCarrierPlayer(player);

                        if (player.selectedHotbarIndex != index)
                        {
                            script.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    public void server_onHotbarIndexChanged(int newIndex, int oldIndex, Player_external player)
    {
        if (oldIndex != -1)
        {
            if (m_gameID_FPVItemScripts.ContainsKey(player.m_gameID))
            {
                if (m_gameID_FPVItemScripts[player.m_gameID][oldIndex] != null)
                {
                    m_gameID_FPVItemScripts[player.m_gameID][oldIndex].deactivateItem();
                }
            }
        }

        if (newIndex != -1)
        {
            if (m_gameID_FPVItemScripts.ContainsKey(player.m_gameID))
            {
                if (m_gameID_FPVItemScripts[player.m_gameID][newIndex] != null)
                {
                    m_gameID_FPVItemScripts[player.m_gameID][newIndex].activateItem();
                }
            }
        }
    }

    public void client_onReceiveHotbarItem(int index)
    {
        Player_local player = EntityManager.singleton.getLocalPlayer();
        LootContainer playerHotbar = player.getHotbar();
        StorableItem item = playerHotbar.getItem(index);

        if (item == null || m_gameID_FPVItemTemplateID[player.m_gameID][index] != item.itemTemplateIndex) // this prevents script reset if only item-count gets changed. reloading script resets variables for example cooldowns
        {
            if (m_gameID_FPVItemGameObjects[player.m_gameID][index] != null)
            {
                Destroy(m_gameID_FPVItemGameObjects[player.m_gameID][index]);
                m_gameID_FPVItemScripts[player.m_gameID][index] = null;
                m_gameID_FPVItemTemplateID[player.m_gameID][index] = -1;
            }

            if (item != null)
            {
                int prefabIndex = item.itemTemplateIndex;

                if (prefabIndex < m_FPVItemsPrefab.Length && prefabIndex >= 0)
                {
                    if (m_FPVItemsPrefab[prefabIndex] != null)
                    {
                        GameObject FPVItem = Instantiate(m_FPVItemsPrefab[prefabIndex]) as GameObject;
                        FPVItem_Base script = FPVItem.GetComponent<FPVItem_Base>();

                        FPVItem.transform.SetParent(m_firstPersonArmsItemSocket.transform);
                        FPVItem.transform.localPosition = script.modelPositionOffset;
                        FPVItem.transform.localRotation = Quaternion.Euler(script.modelRotationOffset);
                        FPVItem.transform.localScale = script.modelScale;

                        m_gameID_FPVItemGameObjects[player.m_gameID][index] = FPVItem;
                        m_gameID_FPVItemScripts[player.m_gameID][index] = script;
                        m_gameID_FPVItemTemplateID[player.m_gameID][index] = item.itemTemplateIndex;

                        script.setHotbarIndex(index);
                        script.setCarrierPlayer(player);

                        if (player.selectedHotbarIndex != index)
                        {
                            script.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    public void client_onHotbarIndexChanged(int newIndex, int oldIndex)
    {
        if (oldIndex != -1)
        {
            Player_local player = EntityManager.singleton.getLocalPlayer();
            if (player != null)
            {
                if (m_gameID_FPVItemScripts[player.m_gameID][oldIndex] != null)
                {
                    m_gameID_FPVItemScripts[player.m_gameID][oldIndex].deactivateItem();
                }
            }
        }

        if (newIndex == -1)
        {
            if (m_firstPersonArms != null)
            {
                m_firstPersonArmsRenderer.enabled = false;
            }
        }
        else
        {
            Player_local player = EntityManager.singleton.getLocalPlayer();
            if (player != null)
            {
                if (m_gameID_FPVItemScripts[player.m_gameID][newIndex] == null)
                {
                    if (m_firstPersonArms != null)
                    {
                        m_firstPersonArmsRenderer.enabled = false;
                    }
                }
                else
                {
                    m_gameID_FPVItemScripts[player.m_gameID][newIndex].activateItem();

                    if (m_firstPersonArms != null && m_gameID_FPVItemScripts[player.m_gameID][newIndex].showFPVArms)
                    {
                        m_firstPersonArmsRenderer.enabled = true;
                    }
                    else
                    {
                        m_firstPersonArmsRenderer.enabled = false;
                    }
                }
            }
        }
    }

    #endregion

    #region Networking Custom Messages

    public void server_receivedCustomMessage(NetworkMessage message, int playerGameID)
    {
        if (message.integerValuesCount > 1) // 0: hotbarIndex, 1: contextID
        {
            int hotbarIndex = message.getIntValue(0);

            if (hotbarIndex < m_playerHotbarSize && hotbarIndex > -1)
            {
                FPVItem_Base[] entityScripts = null;

                if (m_gameID_FPVItemScripts.TryGetValue(playerGameID, out entityScripts))
                {
                    if (entityScripts[hotbarIndex] != null)
                    {
                        entityScripts[hotbarIndex].server_receivedCustomNetworkMessage(message);
                    }
                }
            }
        }
    }

    public void client_receivedCustomMessage(NetworkMessage message)
    {
        if (message.integerValuesCount > 1) // 0: hotbarIndex, 1: contextID
        {
            int hotbarIndex = message.getIntValue(0);

            if (hotbarIndex < m_playerHotbarSize && hotbarIndex > -1)
            {
                FPVItem_Base[] entityScripts = null;

                Player_local localPlayer = EntityManager.singleton.getLocalPlayer();

                if (localPlayer != null)
                {
                    if (m_gameID_FPVItemScripts.TryGetValue(localPlayer.m_gameID, out entityScripts))
                    {
                        if (entityScripts[hotbarIndex] != null)
                        {
                            entityScripts[hotbarIndex].client_receivedCustomNetworkMessage(message);
                        }
                    }
                }
            }
        }
    }

    #endregion

    private static GameObject findNameInChildren(GameObject inGameObject, string name)
    {
        if (inGameObject.name == name)
        {
            return inGameObject;
        }

        for (int i = 0; i < inGameObject.transform.childCount; i++)
        {
            GameObject result = findNameInChildren(inGameObject.transform.GetChild(i).gameObject, name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
