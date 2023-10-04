using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_local : Player_base
{
    private static readonly int LAYERMASK_NORMAL = System.BitConverter.ToInt32(new byte[] {
                                                                                                BitByteTools.getByte(true,false,false,false,false,false,false,false), // default layer
                                                                                                BitByteTools.getByte(true,false,true,true,true,true,true,false), // no bulding preview, no first person view
                                                                                                BitByteTools.getByte(true,true,true,true,false,true,true,true), // no player
                                                                                                byte.MaxValue
                                                                                                }, 0); // everything but terrain

    [Header("Player_local")]
    [SerializeField] private float m_transformSendingInterval = 0.03f; // time between 2 transform (pos, rot) messages from client
    [SerializeField] private float m_interactionDistance = 2;
    [SerializeField] private float m_movingStateSendingInterval = 0.2f;

    protected List<Vector2> externalCameraRot = new List<Vector2>();
    private float m_test_lastHealthGUI = 0f;
    protected PlayerRaycastTarget m_interactionTarget = null;
    protected GameObject m_cameraObj = null;
    private LootContainer m_openExternalContainer = null;
    private LootContainerGame m_openExternalContainerGame = null;
    private float m_lastTimeTransformSending = 0;
    private Vector3 m_lastSendPosition = Vector3.zero;
    private Quaternion m_lastSendRotation = Quaternion.identity;
    private int m_lastSelectedHotbarIndex = -1;
    private WorldViewPoint m_ownedWorldViewPoint;
    private bool m_playerInputMoving = false;
    private bool m_playerInputMovingLastSend = false;
    private float m_lastTimeSendPlayerInputMoving = 0f;

    public void addExternalCameraRotation(Vector2 rotXY)
    {
        externalCameraRot.Add(rotXY);
    }

    protected void Awake()
    {
        // is client
        m_cameraObj = CameraStack.gameobject;
        m_inventoryContainer = new LootContainer(m_inventorySize, 6, gameObject);
        m_hotbarContainer = new LootContainer(m_hotbarSize, 6, gameObject);
        m_openExternalContainer = new LootContainer(30, 6, gameObject); // size = GUI-elements in scene

        m_hotbarContainer.ItemChangedEvent += onHotbarItemChanged_client;
    }

    protected void Player_local_start()
    {
        base.Start();

        FirstPersonViewManager.singleton.client_onLocalPlayerSpawned(this);

        m_ownedWorldViewPoint = PlayerManager.singleton.getWorldViewPoint(-1);
    }

    protected void Update()
    {
        base.Update();

        m_ownedWorldViewPoint.transform.position = transform.position;

        if (m_health != m_test_lastHealthGUI)
        {
            GUIManager.singleton.setHealth(m_health);
            m_test_lastHealthGUI = m_health;
        }

        interactionRaycast();

        if(Time.time > m_lastTimeSendPlayerInputMoving + m_movingStateSendingInterval && m_playerInputMoving != m_playerInputMovingLastSend)
        {
            m_playerInputMovingLastSend = m_playerInputMoving;
            m_lastTimeSendPlayerInputMoving = Time.time;

            NetworkMessage message = getCustomMessageBase();
            message.addIntegerValues((int)CustomMessageContext1.PlayerMovingState);

            if (m_playerInputMoving)
            {
                message.addIntegerValues(1);
            }
            else
            {
                message.addIntegerValues(0);
            }

            client_sendCustomTCPMessage(message);
        }

        if (Input.GetKeyDown(ClientSettingsManager.singleton.getInventoryKey()))
        {
            GUIManager.singleton.switchGUI_Inventory();
        }

        if (Input.GetKey(ClientSettingsManager.singleton.getUseKey()))
        {
            onPlayerUseButton();
        }

        if (Input.GetKeyDown(ClientSettingsManager.singleton.getHotbarItem1Key()))
        {
            client_selectHotbarItem(0);
        }

        if (Input.GetKeyDown(ClientSettingsManager.singleton.getHotbarItem2Key()))
        {
            client_selectHotbarItem(1);
        }

        if (Input.GetKeyDown(ClientSettingsManager.singleton.getHotbarItem3Key()))
        {
            client_selectHotbarItem(2);
        }

        if (Input.GetKeyDown(ClientSettingsManager.singleton.getHotbarItem4Key()))
        {
            client_selectHotbarItem(3);
        }

        if (Input.GetKeyDown(ClientSettingsManager.singleton.getHotbarItem5Key()))
        {
            client_selectHotbarItem(4);
        }

        if (Input.GetKeyDown(ClientSettingsManager.singleton.getHotbarItem6Key()))
        {
            client_selectHotbarItem(5);
        }

        transformSending();
    }

    private void client_selectHotbarItem(int index)
    {
        if (GameManager_Custom.singleton.isServerAndClient)
        {
            if (index == m_selectedHotbarIndex) // same button twice => unselect
            {
                client_receiveHotbarIndex(-1);
            }
            else
            {
                client_receiveHotbarIndex(index);
            }
        }
        else
        {
            if (index == m_selectedHotbarIndex) // same button twice => unselect
            {
                client_sendHotbarIndex(-1);
            }
            else
            {
                client_sendHotbarIndex(index);
            }
        }
    }


    protected void Player_local_FixedUpdate()
    {
        fellThroughGroundCheck();

        //FogManager.singleton.setFogOrigin(transform.position);
    }

    private void transformSending()
    {
        if (m_Entity_UID != -1) // -1 means didnt receive UID for this player from server yet
        {
            if (Time.realtimeSinceStartup > m_lastTimeTransformSending + m_transformSendingInterval)
            {
                if (m_lastSendPosition != transform.position || m_lastSendRotation != transform.rotation)
                {
                    m_lastTimeTransformSending = Time.realtimeSinceStartup;
                    m_lastSendPosition = transform.position;
                    m_lastSendRotation = transform.rotation;

                    NetworkMessage message = getCustomMessageBase();
                    message.addIntegerValues((int)CustomMessageContext1.PositionAndRotation);
                    message.addFloatValues(transform.position.x, transform.position.y, transform.position.z, transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

                    if (GameManager_Custom.singleton.isClient)
                    {
                        client_sendCustomUDPMessage(message); // gets received by a player external
                    }
                    else if (GameManager_Custom.singleton.isServerAndClient)
                    {
                        server_sendUDPMessageToAllClients(message, m_gameID);
                    }
                    else
                    {
                        // not supposed to happen
                        Debug.LogWarning("Player_local: transformSending: should not be possible to get called as server only !");
                        NetworkingManager.singleton.recyleNetworkMessage(message);
                    }
                }
            }
        }
    }

    public void setInventoryItem(GUIRaycastIdentifier.Type type, int countainerUID, int pos, StorableItem item)
    {
        switch (type)
        {
            case GUIRaycastIdentifier.Type.PlayerHotbar:
                {
                    if (m_hotbarContainer != null)
                    {
                        m_hotbarContainer.setItem(item, pos);
                    }
                    break;
                }
            case GUIRaycastIdentifier.Type.PlayerInventory:
                {
                    if (m_inventoryContainer != null)
                    {
                        m_inventoryContainer.setItem(item, pos);
                    }
                    break;
                }
            case GUIRaycastIdentifier.Type.PlayerLootContainer:
                {
                    if (m_openExternalContainerGame == null || m_openExternalContainerGame.getEntityUID() != countainerUID)
                    {
                        NetworkingManager.singleton.client_sendContainerCloseRequest(m_openExternalContainerGame.getEntityUID());
                    }
                    else
                    {
                        m_openExternalContainer.setItem(item, pos);
                    }
                    break;
                }
            default:
                {
                    Debug.LogWarning("Player_local: setInventoryItem: unknown container-type: " + type);
                    break;
                }
        }
    }

    public LootContainer getInventory()
    {
        return m_inventoryContainer;
    }

    public LootContainer getHotbar()
    {
        return m_hotbarContainer;
    }

    protected virtual void onHotbarItemChanged_client(object obj, IntegerEventArgs args)
    {
        FirstPersonViewManager.singleton.client_onReceiveHotbarItem(args.integer);
    }

    protected void onPlayerInputMove(bool isMoving)
    {
        m_playerInputMoving = isMoving;
    }

    public void setOpenLootContainerExternal(LootContainer container)
    {
        Debug.Log("TODO: delete this");
        // m_openExternalContainer = container;
    }

    public LootContainer getOpenLootContainerExternal()
    {
        if (m_openExternalContainerGame == null)
        {
            return null;
        }
        else
        {
            return m_openExternalContainer;
        }
    }

    public LootContainerGame getOpenLootContainerGameExternal()
    {
        return m_openExternalContainerGame;
    }

    public void onReceiveOpenContainerCommand(int containerUID)
    {
        if (m_openExternalContainerGame != null && m_openExternalContainerGame.getEntityUID() == containerUID)
        {
            return; // no need to open the same container twice
        }

        if (EntityManager.singleton.client_getLootContainerGame(containerUID) != null)
        {
            if (m_openExternalContainerGame != null)
            {
                m_openExternalContainerGame.client_SetInUseState(false);
            }

            m_openExternalContainerGame = EntityManager.singleton.client_getLootContainerGame(containerUID);
            m_openExternalContainer.removeAllItems();
            if (m_openExternalContainerGame != null)
            {
                m_openExternalContainerGame.client_SetInUseState(true);
                GUIManager.singleton.setInventoryExternalContainerSize(m_openExternalContainerGame.getItemsSlotsSize());
            }
        }
        else
        {
            m_openExternalContainerGame = null;
            Debug.LogWarning("Player_local: onReceiveOpenContainerCommand: can't find loot-container for ID: " + containerUID);
        }

        GUIManager.singleton.setGUI_Inventory(true); // triggers inventory-reload
    }

    public void onReceiveCloseContainerCommand(int containerUID)
    {
        if (m_openExternalContainerGame == null || m_openExternalContainerGame.getEntityUID() != containerUID)
        {
            return; // don't try to close a container that isnt open
        }

        m_openExternalContainerGame.client_SetInUseState(false);

        if (EntityManager.singleton.client_getLootContainerGame(containerUID) != null)
        {
            m_openExternalContainerGame = null;
        }
        else
        {
            m_openExternalContainerGame = null;
            Debug.LogWarning("Player_local: onReceiveCloseContainerCommand: can't find loot-container for ID: " + containerUID);
        }

        GUIManager.singleton.reloadPlayerInventoryItems();
    }

    protected void interactionRaycast()
    {
        RaycastHit hit;

        if (Physics.Raycast(m_cameraObj.transform.position, m_cameraObj.transform.forward, out hit, m_interactionDistance, LAYERMASK_NORMAL))
        {
            m_interactionTarget = hit.collider.GetComponent<PlayerRaycastTarget>();

            if (m_interactionTarget != null)
            {
                m_interactionTarget.registerPlayerRaycastHit();
            }
        }
        else
        {
            m_interactionTarget = null;
        }
    }

    private void client_sendHotbarIndex(int index)
    {
        NetworkMessage message = getCustomMessageBase();

        message.addIntegerValues((int)CustomMessageContext1.SetHotbarIndex, index);
        client_sendCustomTCPMessage(message); // send to player external
    }

    public void client_receiveHotbarIndex(int index) // received from player external
    {
        m_lastSelectedHotbarIndex = m_selectedHotbarIndex;
        m_selectedHotbarIndex = index;

        GUIManager.singleton.setHotbarSelectedIndex(m_selectedHotbarIndex);
        FirstPersonViewManager.singleton.client_onHotbarIndexChanged(m_selectedHotbarIndex, m_lastSelectedHotbarIndex);
    }

    public override bool client_receivedCustomNetworkMessage(NetworkMessage message)
    {
        if (base.client_receivedCustomNetworkMessage(message))
        {
            return true; // a base class already handles this message
        }
        else
        {
            switch (message.getIntValue(1))
            {
                case (int)CustomMessageContext1.SetHotbarIndex:
                    {
                        if (message.checkInputCorrectness(3, 0, 0))
                        {
                            client_receiveHotbarIndex(message.getIntValue(2));
                        }
                        else
                        {
                            Debug.LogWarning("Player_local: client_receivedCustomNetworkMessage: message values count unexpected: " + message.ToString());
                        }
                        return true;
                    }
                case (int)CustomMessageContext1.FoodWaterHungerThirst:
                    {
                        if (message.checkInputCorrectness(2, 4, 0))
                        {
                            if (GameManager_Custom.singleton.isClient)
                            {
                                m_food = message.getFloatValue(0);
                                m_water = message.getFloatValue(1);
                                m_hunger = message.getFloatValue(2);
                                m_thirst = message.getFloatValue(3);
                            }

                            GUIManager.singleton.setFood(m_food);
                            GUIManager.singleton.setWater(m_water);
                            GUIManager.singleton.setHunger(m_hunger);
                            GUIManager.singleton.setThirst(m_thirst);
                        }
                        else
                        {
                            Debug.LogWarning("Player_local: client_receivedCustomNetworkMessage: message values count unexpected: " + message.ToString());
                        }
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }
    }

    protected override void onDamaged(float damage)
    {
        base.onDamaged(damage);

        GUIManager.singleton.tryStartHurtOverlay();
    }

    protected virtual void onPlayerUseButton()
    {
        if (m_interactionTarget != null)
        {
            m_interactionTarget.registerPlayerUsedAction();
        }
    }

    protected override void onRegisterEntity()
    {
        if (GameManager_Custom.singleton.isServerAndClient)
        {
            m_Entity_UID = EntityManager.singleton.registerLocalPlayer(this);
        }
        else
        {
            if (GameManager_Custom.singleton.isClient)
            {
                EntityManager.singleton.registerLocalPlayer(this);
            }
        }
    }

    protected override void onUnregisterEntity()
    {
        EntityManager.singleton.unregisterLocalPlayer();
    }
}
