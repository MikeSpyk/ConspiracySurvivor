using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DroppedItemWorld : Entity_NetworkPositionRotation
{
    [SerializeField] private float m_maxLifeTime = 600;

    private StorableItem m_item;
    private bool m_isLooking = false;
    private float m_startTime;
    private Vector3 m_spawnPosition;

    protected void Awake()
    {
        base.Awake();

        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            PlayerRaycastTarget target = GetComponent<PlayerRaycastTarget>();

            if (target == null)
            {
                Debug.LogError("DroppedItemWorld: Awake: PlayerRaycastTarget is null");
            }
            else
            {
                target.PlayerStartUseEvent += onPlayerStartClickUse;
                target.PlayerRaycastStartHitEvent += onPlayerStartLooking;
                target.PlayerRaycastEndHitEvent += onPlayerEndLooking;
            }
        }
    }

    protected void Start()
    {
        base.Start();

        m_spawnPosition = transform.position;

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            m_startTime = Time.time;

            if (m_item == null)
            {
                Debug.LogWarning("DroppedItemWorld: Start: item = null");
                Destroy(gameObject);
            }
        }
    }

    protected void Update()
    {
        base.Update();

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            if (Time.time > m_startTime + m_maxLifeTime)
            {
                Destroy(gameObject);
            }

            if(transform.position.y < -1000) // fall through ground
            {
                transform.position = m_spawnPosition;
                m_rigidbody.velocity = Vector3.zero;
            }
        }
    }

    protected void OnDestroy()
    {
        base.OnDestroy();
        if (m_isLooking)
        {
            GUIManager.singleton.setInteractableText("");
        }
    }

    public override NetworkMessage server_fillUncullingMessage(NetworkMessage message)
    {
        base.server_fillUncullingMessage(message);

        message.setMessageContext((int)NetworkingManager.NetMessageContextTCP.EntityUncull);
        message.addIntegerValues(m_item.itemTemplateIndex, m_item.m_stackSize);

        return message;
    }

    public override void client_receiveUncullMessage(NetworkMessage message)
    {
        base.client_receiveUncullMessage(message);

        if (message.integerValuesCount < 4)
        {
            Debug.LogWarning("DroppedItemWorld: client_receiveUncullMessage: not enought integer values: " + message.integerValuesCount);
        }
        else
        {
            m_item = ItemManager.singleton.createNewStorableItem(message.getIntValue(2), message.getIntValue(3));
        }
    }

    public override DataEntity_Base getNewDefaultDataEntity()
    {
        return new DataEntity_DroppedItem();
    }

    protected override void updateDataEntity()
    {
        base.updateDataEntity();
        (m_dataEntity as DataEntity_DroppedItem).m_item = m_item;
    }

    protected override void updateEntity()
    {
        base.updateEntity();
        m_item = (m_dataEntity as DataEntity_DroppedItem).m_item;
    }

    public override bool server_receivedCustomNetworkMessage(NetworkMessage message)
    {
        //Debug.Log("DroppedItemWorld: server_receivedCustomNetworkMessage");

        if (base.server_receivedCustomNetworkMessage(message))
        {
            return true; // a base class already handles this message
        }
        else
        {
            switch (message.getIntValue(1))
            {
                case (int)CustomMessageContext1.ItemPickup:
                    {
                        //Debug.Log("DroppedItemWorld: server_receivedCustomNetworkMessage: ItemPickup");

                        if (message.checkInputCorrectness(2, 0, 0))
                        {
                            int playerGameID = NetworkingManager.singleton.server_getPlayerGameIDForIPEndpoint(message.iPEndPoint);

                            //Debug.Log("DroppedItemWorld: server_receivedCustomNetworkMessage: playerGameID: "+ playerGameID);

                            if (playerGameID < -1)
                            {
                                Debug.LogWarning("DroppedItemWorld: server_receivedCustomNetworkMessage: unknown player for IP: " + message.iPEndPoint.Address.ToString());
                            }
                            else if (playerGameID == -1) // local player
                            {
                                Player_local player = EntityManager.singleton.getLocalPlayer();
                                if (player != null)
                                {
                                    if (player.tryAddItem(m_item))
                                    {
                                        Destroy(gameObject);
                                    }
                                }
                            }
                            else // external client
                            {
                                Player_base externalPlayer = EntityManager.singleton.getActivePlayer(playerGameID) as Player_base;

                                if (externalPlayer == null)
                                {
                                    Debug.LogWarning("DroppedItemWorld: server_receivedCustomNetworkMessage: item pickup request from invalid player: " + playerGameID);
                                }
                                else
                                {
                                    //Debug.Log("DroppedItemWorld: server_receivedCustomNetworkMessage: externalPlayer.tryAddItem");

                                    if (externalPlayer.tryAddItem(m_item))
                                    {
                                        Destroy(gameObject);
                                    }
                                    else
                                    {
                                        Debug.Log("DroppedItemWorld: player pickup faild");
                                    }
                                }
                            }
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

    private void onPlayerStartClickUse(object obj, EventArgs args)
    {
        NetworkMessage message = getCustomMessageBase();
        message.addIntegerValues((int)CustomMessageContext1.ItemPickup);
        client_sendCustomTCPMessage(message);

        //Debug.Log("onPlayerStartClickUse");

        //NetworkingManager.singleton.client_sendItemPickupRequest(m_Entity_UID);
    }

    private void onPlayerStartLooking(object obj, EventArgs args)
    {
        m_isLooking = true;
        GUIManager.singleton.setInteractableText(m_item.displayName);
    }

    private void onPlayerEndLooking(object obj, EventArgs args)
    {
        m_isLooking = false;
        GUIManager.singleton.setInteractableText("");
    }

    public void setItem(StorableItem item)
    {
        m_item = item;
    }

    public StorableItem getItem()
    {
        return m_item;
    }

    public void addForce(Vector3 force)
    {
        m_rigidbody.AddForce(force);
    }
}
