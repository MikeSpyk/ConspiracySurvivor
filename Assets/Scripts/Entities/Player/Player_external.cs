using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_external : Player_base
{
    [Header("Player_external")]
    [SerializeField] private bool DEBUG_Kill_Player = false;
    [SerializeField] private float m_transformSendingInterval = 0.03f;

    private float m_lastTimeTransformSending = 0;
    private Vector3 m_lastSendPosition = Vector3.zero;
    private Quaternion m_lastSendRotation = Quaternion.identity;

    protected float m_lastTimeReceivedPosition = 0;

    protected void Awake()
    {
        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            // server manages every external player, client doesn't
            m_inventoryContainer = new LootContainer(m_inventorySize, 6, gameObject);
            m_hotbarContainer = new LootContainer(m_hotbarSize, 6, gameObject);

            m_inventoryContainer.ItemChangedEvent += onInventoryItemChanged_server;
            m_hotbarContainer.ItemChangedEvent += onHotbarItemChanged_server;
        }
    }

    protected void Start()
    {
        base.Start();

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            FirstPersonViewManager.singleton.server_onPlayerExternalSpawned(this);

            NetworkingManager.singleton.server_sendPlayerEntityID(m_gameID, m_Entity_UID);

            server_sendAllDataToPlayer();
        }
    }

    protected void Update()
    {
        base.Update();

        if (DEBUG_Kill_Player)
        {
            DEBUG_Kill_Player = false;
            onDamaged(1000);
        }

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            server_transformSending();
        }

        fellThroughGroundCheck();
    }

    private void server_transformSending()
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

                server_sendUDPMessageToAllClients(message, m_gameID); // gets received by a player external
            }
        }
    }

    #region client only

    public virtual void onReceivedPlayerPosition(Vector3 newPosition) { }

    public virtual void onReceivedPlayerRotation(float angleY, float angleZ) { }

    #endregion

    public void onTransformedByWakeUp()
    {
        FirstPersonViewManager.singleton.server_onPlayerExternalSpawned(this);
    }

    public void server_sendAllDataToPlayer()
    {
        for (int i = 0; i < m_hotbarSize; i++)
        {
            onHotbarItemChanged_server(this, new IntegerEventArgs() { integer = i });
        }

        for (int i = 0; i < m_inventorySize; i++)
        {
            onInventoryItemChanged_server(this, new IntegerEventArgs() { integer = i });
        }

        server_sendHealthUpdate();
    }

    protected override void onHotbarItemChanged_server(object obj, IntegerEventArgs args)
    {
        base.onHotbarItemChanged_server(obj, args);

        FirstPersonViewManager.singleton.server_onHotBarItemChanged(this, m_hotbarContainer.getItem(args.integer), args.integer);
    }

    public void server_setHotbarIndex(int index)
    {
        FirstPersonViewManager.singleton.server_onHotbarIndexChanged(index, m_selectedHotbarIndex, this);

        m_selectedHotbarIndex = index;

        NetworkMessage message = getCustomMessageBase();
        message.addIntegerValues((int)CustomMessageContext1.SetHotbarIndex, index);
        server_sendCustomTCPMessage(message, m_gameID);
    }

    protected override void onRegisterEntity()
    {
        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            if (m_Entity_UID == -1)
            {
                m_Entity_UID = EntityManager.singleton.server_registerPlayerEntity(this);
            }
            else
            {
                EntityManager.singleton.server_registerPlayerEntity(this, m_Entity_UID);
            }
        }
        else if (GameManager_Custom.singleton.isClient)
        {

        }
    }

    protected override void onUnregisterEntity()
    {
        EntityManager.singleton.server_unregisterPlayerEntity(this);
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
                case (int)CustomMessageContext1.PositionAndRotation: // potential thread due to everyone could send this message not only the client that owns player-object
                    {
                        if (message.checkInputCorrectness(2, 6, 0))
                        {
                            onReceivedPlayerPosition(new Vector3(message.getFloatValue(0), message.getFloatValue(1), message.getFloatValue(2)));
                            onReceivedPlayerRotation(message.getFloatValue(4), message.getFloatValue(5));

                            //Debug.Log("Player_external: client_receivedCustomNetworkMessage: " + message.ToString());
                        }
                        else
                        {
                            //Debug.LogWarning("Player_external: client_receivedCustomNetworkMessage: message values count unexpected: " + message.ToString());
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

    public override bool server_receivedCustomNetworkMessage(NetworkMessage message)
    {
        if (base.server_receivedCustomNetworkMessage(message))
        {
            return true; // a base class already handles this message
        }
        else
        {
            switch (message.getIntValue(1))
            {
                case (int)CustomMessageContext1.PositionAndRotation: // potential thread due to everyone could send this message not only the client that owns player-object
                    {
                        if (message.checkInputCorrectness(2, 6, 0))
                        {
                            onReceivedPlayerPosition(new Vector3(message.getFloatValue(0), message.getFloatValue(1), message.getFloatValue(2)));
                            onReceivedPlayerRotation(message.getFloatValue(4), message.getFloatValue(5));
                        }
                        return true;
                    }
                case (int)CustomMessageContext1.SetHotbarIndex:
                    {
                        if (message.checkInputCorrectness(3, 0, 0))
                        {
                            server_setHotbarIndex(message.getIntValue(2));
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

}
