using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPVItem_WaterBottle : FPVItem_Base
{
    protected override void onItemUsagePrimary()
    {
        NetworkMessage message = getCustomMessageBase();
        message.addIntegerValues((int)CustomMessageContext1.ItemConsuming);
        client_sendCustomTCPMessage(message);
    }

    public override bool server_receivedCustomNetworkMessage(NetworkMessage message)
    {
        switch (message.getIntValue(1))
        {
            case (int)CustomMessageContext1.ItemConsuming:
                {
                    if (message.checkInputCorrectness(2, 0, 0))
                    {
                        m_carrierPlayer.addWater(30); // WATER VALUE HERE !!
                        NetworkingManager.singleton.server_sendWorldSoundToAllInRange(28, m_carrierPlayer.transform.position, m_carrierPlayer.entityUID);

                        if (m_carrierPlayer.getCurrentHotBarItem().m_stackSize < 2)
                        {
                            m_carrierPlayer.tryDeleteItem(1, hotbarIndex);
                        }
                        else
                        {
                            m_carrierPlayer.getCurrentHotBarItem().m_stackSize--;
                            NetworkingManager.singleton.server_sendInventoryItemUpdate(m_carrierPlayer.m_gameID, GUIRaycastIdentifier.Type.PlayerHotbar, hotbarIndex, -1, m_carrierPlayer.getCurrentHotBarItem());
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Wrong Input Format" + message.ToString());
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
