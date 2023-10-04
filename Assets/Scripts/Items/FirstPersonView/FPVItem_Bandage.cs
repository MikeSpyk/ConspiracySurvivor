using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPVItem_Bandage : FPVItem_Base
{
    [SerializeField] int maxHeal = 10;
    private Player_local player;
    // Start is called before the first frame update
    void Start()
    {
        base.Start();
    }

    protected override void onItemUsagePrimary()
    {
        // Intervall einbauen wie lang er heilt bis er sich fertig geheilt hat.
        NetworkMessage message = getCustomMessageBase();
        message.addIntegerValues((int)CustomMessageContext2.Heal);
        client_sendCustomTCPMessage(message);

    }

    public override bool server_receivedCustomNetworkMessage(NetworkMessage message)
    {
        switch (message.getIntValue(1))
        {
            case (int)CustomMessageContext2.Heal:

                if (message.checkInputCorrectness(2, 0, 0))
                {
                    int tempitemindex;

                    m_carrierPlayer.addHeal(maxHeal);
                    tempitemindex = m_carrierPlayer.getCurrentHotBarItemIndex();
                    m_carrierPlayer.tryDeleteItem(1, tempitemindex);

                }
                else
                {
                    Debug.LogWarning("Wrong Input Format" + message.ToString());
                }

                return true;

            default:
                {
                    return false;
                }
        }
        return false;
    }

}
