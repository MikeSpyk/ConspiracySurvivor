using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity_NetworkPositionRotation : Entity_NetworkPosition
{
    [Header("Entity_NetworkPositionRotation")]
    [SerializeField] private float m_rotationUpdateTime = 0.04f; // time to pass before another rotation updates will be send
    [SerializeField] private NetworkingProtocol m_RotationSendingProtocol = NetworkingProtocol.UDP;

    private float m_lastTimeSendRotationUpdate = 0;
    private Quaternion m_rotationLastSend = Quaternion.identity;

    protected void Awake()
    {
        base.Awake();
    }

    protected void Start()
    {
        base.Start();
    }

    protected void Update()
    {
        base.Update();

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            if (Time.time > m_lastTimeSendRotationUpdate + m_rotationUpdateTime && m_rotationLastSend != transform.rotation)
            {
                m_lastTimeSendRotationUpdate = Time.time;
                m_rotationLastSend = transform.rotation;

                NetworkMessage message = getCustomMessageBase();
                message.addIntegerValues((int)CustomMessageContext1.Rotation);
                message.addFloatValues(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

                if (m_RotationSendingProtocol == NetworkingProtocol.UDP)
                {
                    server_sendUDPMessageToAllClients(message);
                }
                else if (m_RotationSendingProtocol == NetworkingProtocol.TCP)
                {
                    server_sendTCPMessageToAllClients(message);
                }
            }
        }
    }

    protected void OnDestroy()
    {
        base.OnDestroy();
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
                case (int)CustomMessageContext1.Rotation:
                    {
                        if (message.checkInputCorrectness(2, 3, 0))
                        {
                            transform.rotation = Quaternion.Euler(new Vector3(message.getFloatValue(0), message.getFloatValue(1), message.getFloatValue(2)));
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
