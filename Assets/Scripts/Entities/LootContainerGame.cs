using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LootContainerGame : Entity_damageable
{
    [Header("LootContainerGame")]
    [SerializeField, ReadOnly] private bool m_isInUse = false;
    [SerializeField] private int m_item_slots = 12;
    [SerializeField] private int m_itemsPerRow = 6;

    #region server only members

    private LootContainer m_lootContainer;
    private List<int> m_registeredPlayersGameID; // players that are currently using this container. needed to determine which clients need to receive an update notifications for this container

    #endregion

    #region Unity Methods

    protected void Awake()
    {
        base.Awake();

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            m_lootContainer = new LootContainer(m_item_slots, m_itemsPerRow, gameObject);
            m_lootContainer.ItemChangedEvent += onItemChanged_server;
            m_registeredPlayersGameID = new List<int>();
        }
    }

    protected void Start()
    {
        base.Start();

        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            PlayerRaycastTarget target = GetComponent<PlayerRaycastTarget>();

            if (target != null)
            {
                target.PlayerStartUseEvent += onPlayerStartClickUse;
                target.PlayerRaycastStartHitEvent += onPlayerStartLooking;
                target.PlayerRaycastEndHitEvent += onPlayerEndLooking;
            }
        }
    }

    #endregion

    public bool setItem(StorableItem item, int index)
    {
        return m_lootContainer.setItem(item, index);
    }

    public void setItems(StorableItem[] items)
    {
        m_lootContainer.setAllItems(items);
    }

    public StorableItem removeItem(int index)
    {
        return m_lootContainer.removeItem(index);
    }

    public void switchItem(int sourceIndex, int targetIndex)
    {
        m_lootContainer.switchItem(sourceIndex, targetIndex);
    }

    public void switchItemContainer(int sourceIndex, int targetIndex, LootContainer targetContainer)
    {
        m_lootContainer.switchItemContainer(sourceIndex, targetIndex, targetContainer);
    }

    public int getItemsSlotsSize()
    {
        return m_item_slots;
    }

    #region Client only

    private void onPlayerStartLooking(object obj, EventArgs args)
    {
        //Debug.Log("Loot Container " + m_Entity_UID + " start looking");
    }

    private void onPlayerEndLooking(object obj, EventArgs args)
    {
        if (m_isInUse)
        {
            NetworkingManager.singleton.client_sendContainerCloseRequest(m_Entity_UID);
        }
        //Debug.Log("Loot Container " + m_Entity_UID + " end looking");
    }

    private void onPlayerStartClickUse(object obj, EventArgs args)
    {
        NetworkingManager.singleton.client_sendEntityUsed(m_Entity_UID);

        //Debug.Log("Loot Container " + m_Entity_UID + " used");
    }

    public void client_SetInUseState(bool newState)
    {
        m_isInUse = newState;
    }

    #endregion

    #region server only

    public override void server_onClientUse(int gameID)
    {
        if (m_registeredPlayersGameID.Contains(gameID)) // is already using
        {
            EntityManager.singleton.server_setLootContainerForClient(gameID, null);
            NetworkingManager.singleton.server_sendContainerCloseCommand(gameID, m_Entity_UID);
            server_unregisterPlayerUsing(gameID);
        }
        else // using first time
        {
            EntityManager.singleton.server_setLootContainerForClient(gameID, this);
            NetworkingManager.singleton.server_sendContainerOpenCommand(gameID, m_Entity_UID);

            StorableItem[] items = m_lootContainer.getAllItems();

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    NetworkingManager.singleton.server_sendInventoryItemUpdate(gameID, GUIRaycastIdentifier.Type.PlayerLootContainer, i, m_Entity_UID, items[i]);
                }
            }

            server_registerPlayerUsing(gameID);
        }
    }

    public void server_registerPlayerUsing(int gameID)
    {
        if (m_registeredPlayersGameID.Contains(gameID))
        {
            Debug.LogWarning("LootContainerGame: registerPlayerOpened: player is already registered");
        }
        else
        {
            m_registeredPlayersGameID.Add(gameID);
            m_isInUse = true;
        }
    }

    public void server_unregisterPlayerUsing(int gameID)
    {
        if (m_registeredPlayersGameID.Contains(gameID))
        {
            m_registeredPlayersGameID.Remove(gameID);
        }

        if (m_registeredPlayersGameID.Count < 1)
        {
            m_isInUse = false;
        }
    }

    public void onItemChanged_server(object obj, IntegerEventArgs args)
    {
        for (int i = 0; i < m_registeredPlayersGameID.Count; i++)
        {
            NetworkingManager.singleton.server_sendInventoryItemUpdate(m_registeredPlayersGameID[i], GUIRaycastIdentifier.Type.PlayerLootContainer, args.integer, m_Entity_UID, m_lootContainer.getItem(args.integer));
        }
    }

    #endregion
    protected void OnDestroy()
    {
        base.OnDestroy();

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            if (m_lootContainer != null)
            {
                m_lootContainer.ItemChangedEvent -= onItemChanged_server;
                //m_lootContainer.destroyAllItems(); not with data entity might still need it
            }
        }

        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            if (m_isInUse)
            {
                Player_local playerScript = EntityManager.singleton.getLocalPlayer();
                if (playerScript != null)
                {
                    playerScript.setOpenLootContainerExternal(null);
                    GUIManager.singleton.setGUI_Inventory(false);
                }
            }
        }
    }

    #region Data Entity

    public override DataEntity_Base getNewDefaultDataEntity()
    {
        return new DataEntity_LootContainer();
    }

    protected override void updateDataEntity()
    {
        base.updateDataEntity();
        (m_dataEntity as DataEntity_LootContainer).m_lootContainer = m_lootContainer;
    }

    protected override void updateEntity()
    {
        base.updateEntity();

        if (m_lootContainer != null)
        {
            m_lootContainer.ItemChangedEvent -= onItemChanged_server;
        }

        m_lootContainer = (m_dataEntity as DataEntity_LootContainer).m_lootContainer;
        m_lootContainer.ItemChangedEvent += onItemChanged_server;
    }

    #endregion

}
