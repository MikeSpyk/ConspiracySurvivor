using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPVItem_Pickaxe : FPVItem_Base
{

    private RaycastHit hit;
    private Transform m_Cam;
    private Vector3 p1;
    private bool isObjektTree;
    private bool ishit;
    [SerializeField] int maxResource = 10;
    [SerializeField] float maxDistance;



    void Start()
    {
        base.Start();
        m_Cam = Camera.main.transform;

    }

    protected override void onItemUsagePrimary()
    {
        NetworkMessage message = getCustomMessageBase();

        ishit = Physics.Raycast(m_Cam.position, m_Cam.forward, out hit, maxDistance);
        Debug.DrawRay(m_Cam.position, m_Cam.forward * maxDistance, Color.blue);

        if (ishit)
        {


        }



    }

    public override bool server_receivedCustomNetworkMessage(NetworkMessage message)
    {
        switch (message.getIntValue(1))
        {
            case (int)CustomMessageContext2.PickStone:

                if (message.checkInputCorrectness(3, 0, 0))
                {
                    Entity_ResourceStone Stone = EntityManager.singleton.getEntity(message.getIntValue(2)) as Entity_ResourceStone;

                    if (Stone != null)
                    {
                        Stone.harvest(m_carrierPlayer, maxResource);

                    }
                    else
                    {
                        Debug.LogWarning("Wrong GameObjekt" + EntityManager.singleton.getEntity(message.getIntValue(2)).ToString());
                    }
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
