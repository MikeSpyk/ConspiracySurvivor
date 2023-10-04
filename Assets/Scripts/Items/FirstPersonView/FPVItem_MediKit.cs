using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPVItem_MediKit : FPVItem_Base
{
    [SerializeField] int maxHeal = 100;
    [SerializeField] float iteamUseTime = 10;
    private Player_local player;
    private float startItemUse = 0;
    private int clientIsUse = 0;
    private int serverIsUse = 0;
    // Start is called before the first frame update
    void Start()
    {
        base.Start();
    }

    protected override void onItemUsagePrimary()
    {
        if(clientIsUse == 0)
        {
            clientIsUse = 1;
            NetworkMessage message = getCustomMessageBase();
            message.addIntegerValues((int)CustomMessageContext2.Heal);
            message.addStringValues("START");
            client_sendCustomTCPMessage(message);
        }
       

    }

    protected override void onItemUsagePrimaryEnded()
    {
        NetworkMessage message = getCustomMessageBase();
        message.addIntegerValues((int)CustomMessageContext2.Heal);
        message.addStringValues("STOP");
        client_sendCustomTCPMessage(message);
        clientIsUse = 0;
    }

    public override bool client_receivedCustomNetworkMessage(NetworkMessage message)
    {
        switch (message.getIntValue(1))
        {
            case (int)CustomMessageContext2.Heal:

                if (message.checkInputCorrectness(3, 0, 0))
                {

                    clientIsUse = message.getIntValue(2);

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




    public override bool server_receivedCustomNetworkMessage(NetworkMessage message)
    {
        switch (message.getIntValue(1))
        {
            case (int)CustomMessageContext2.Heal:
            {
                switch (message.getStringValue(0))
                {
                    case "START":
                            {
                                if (message.checkInputCorrectness(2, 0, 1))
                                {
                                    if (serverIsUse == 0)
                                    {
                                        serverIsUse = 1;
                                    }

                                    while (serverIsUse == 0)
                                    {
                                        if (startItemUse == 0)
                                        {
                                            startItemUse = Time.time;
                                        }

                                        if (startItemUse + iteamUseTime <= Time.time)
                                        {
                                            int tempitemindex;
                                            m_carrierPlayer.addHeal(maxHeal);
                                            tempitemindex = m_carrierPlayer.getCurrentHotBarItemIndex();
                                            m_carrierPlayer.tryDeleteItem(1, tempitemindex);
                                            startItemUse = 0;

                                            NetworkMessage messageclient = getCustomMessageBase();
                                            messageclient.addIntegerValues((int)CustomMessageContext2.Heal);
                                            messageclient.addIntegerValues(0);
                                            server_sendCustomTCPMessage(messageclient);
                                        }
                                    }

                                }
                                else
                                {
                                    Debug.LogWarning("Wrong Input Format" + message.ToString());
                                }
                                return true;
                            }

                    case "STOP":
                            {
                                if (message.checkInputCorrectness(2, 0, 1))
                                {
                                    serverIsUse = 0;
                                    startItemUse = 0;
                                }

                                return true;

                            }

                    default:
                            {
                                return false;
                            }
                    }
            }

            default:
                {
                    return false;
                }
        }
        return false;
    }
}
