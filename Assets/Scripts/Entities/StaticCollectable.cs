using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerRaycastTarget))]
public class StaticCollectable : Entity_base
{
    [Header("StaticCollectable")]
    [SerializeField] private string[] m_itemsToCollectType;
    [SerializeField] private int[] m_itemsToCollectMinCount;
    [SerializeField] private int[] m_itemsToCollectMaxCount;
    [SerializeField] private int m_pickUpSoundIndex = -1;

    public int m_rasterFieldID = -1;

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
            }
        }
    }

    private void onPlayerStartClickUse(object obj, EventArgs args)
    {
        NetworkMessage message = getCustomMessageBase();
        message.addIntegerValues((int)CustomMessageContext1.ItemPickup);
        client_sendCustomTCPMessage(message);
    }

    public override DataEntity_Base getNewDefaultDataEntity()
    {
        return new DataEntity_StaticCollectable();
    }

    protected override void updateDataEntity()
    {
        base.updateDataEntity();

        DataEntity_StaticCollectable dataEntity = m_dataEntity as DataEntity_StaticCollectable;

        dataEntity.m_rasterFieldID = m_rasterFieldID;
    }

    protected override void updateEntity()
    {
        base.updateEntity();

        DataEntity_StaticCollectable dataEntity = m_dataEntity as DataEntity_StaticCollectable;

        m_rasterFieldID = dataEntity.m_rasterFieldID;
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
                                Debug.LogWarning("StaticCollectable: server_receivedCustomNetworkMessage: unknown player for IP: " + message.iPEndPoint.Address.ToString());
                            }
                            else if (playerGameID == -1) // local player
                            {
                                Player_local player = EntityManager.singleton.getLocalPlayer();
                                if (player != null)
                                {
                                    for (int i = 0; i < m_itemsToCollectType.Length; i++)
                                    {
                                        int randomSize = Math.Max(m_itemsToCollectMinCount[i], (int)(UnityEngine.Random.value * m_itemsToCollectMaxCount[i]));

                                        StorableItem item = ItemManager.singleton.createNewStorableItem(m_itemsToCollectType[i], randomSize);

                                        player.tryAddItem(item);
                                    }

                                    if (m_pickUpSoundIndex > -1)
                                    {
                                        NetworkingManager.singleton.server_sendWorldSoundToAllInRange(m_pickUpSoundIndex, transform.position);
                                    }

                                    WorldManager.singleton.unregisterResource(FieldResources.ResourceType.BerryPlant, m_rasterFieldID, new Vector2(transform.position.x, transform.position.z));
                                    Destroy(gameObject);
                                }
                            }
                            else // external client
                            {
                                Player_base externalPlayer = EntityManager.singleton.getActivePlayer(playerGameID) as Player_base;

                                if (externalPlayer == null)
                                {
                                    Debug.LogWarning("StaticCollectable: server_receivedCustomNetworkMessage: item pickup request from invalid player: " + playerGameID);
                                }
                                else
                                {
                                    //Debug.Log("DroppedItemWorld: server_receivedCustomNetworkMessage: externalPlayer.tryAddItem");

                                    for (int i = 0; i < m_itemsToCollectType.Length; i++)
                                    {
                                        int randomSize = Math.Max(m_itemsToCollectMinCount[i], (int)(UnityEngine.Random.value * m_itemsToCollectMaxCount[i]));

                                        StorableItem item = ItemManager.singleton.createNewStorableItem(m_itemsToCollectType[i], randomSize);

                                        externalPlayer.tryAddItem(item);
                                    }

                                    if (m_pickUpSoundIndex > -1)
                                    {
                                        NetworkingManager.singleton.server_sendWorldSoundToAllInRange(m_pickUpSoundIndex, transform.position);
                                    }

                                    WorldManager.singleton.unregisterResource(FieldResources.ResourceType.BerryPlant, m_rasterFieldID, new Vector2(transform.position.x, transform.position.z));
                                    Destroy(gameObject);
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

}
