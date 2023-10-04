using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity_NetworkPosition : Entity_base
{
    const float MAX_POS_DISTANCE_NETWORK = 1; // max distance to move away from last received position before resetting

    [Header("Entity_NetworkPosition")]
    [SerializeField] private float m_positionUpdateTime = 0.04f; // time to pass before another position updates will be send
    [SerializeField] private NetworkingProtocol m_positionSendingProtocol = NetworkingProtocol.UDP;
    private float m_lastTimeSendPositionUpdate = 0;
    private Vector3 m_positionLastSent = Vector3.zero;
    private Vector3 m_client_lastReceivedPosition;
    protected Rigidbody m_rigidbody = null;

    protected void Awake()
    {
        base.Awake();

        m_rigidbody = GetComponent<Rigidbody>();
    }

    protected void Start()
    {
        base.Start();
        m_client_lastReceivedPosition = transform.position;
    }

    protected void Update()
    {
        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            if (Time.time > m_lastTimeSendPositionUpdate + m_positionUpdateTime && m_positionLastSent != transform.position)
            {
                m_lastTimeSendPositionUpdate = Time.time;
                m_positionLastSent = transform.position;

                NetworkMessage message = getCustomMessageBase();
                message.addIntegerValues((int)CustomMessageContext1.Position);
                message.addFloatValues(transform.position.x, transform.position.y, transform.position.z);

                if (m_positionSendingProtocol == NetworkingProtocol.UDP)
                {
                    server_sendUDPMessageToAllClients(message);
                }
                else if (m_positionSendingProtocol == NetworkingProtocol.TCP)
                {
                    server_sendTCPMessageToAllClients(message);
                }
            }
        }
    }

    protected void FixedUpdate()
    {
        if (GameManager_Custom.singleton.isClient)
        {
            if (Vector3.Distance(transform.position, m_client_lastReceivedPosition) > MAX_POS_DISTANCE_NETWORK)
            {
                transform.position = m_client_lastReceivedPosition;

                if (m_rigidbody != null)
                {
                    m_rigidbody.velocity = Vector3.zero;
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
                case (int)CustomMessageContext1.Position:
                    {
                        if (message.checkInputCorrectness(2, 3, 0))
                        {
                            transform.position = new Vector3(message.getFloatValue(0), message.getFloatValue(1), message.getFloatValue(2));
                            m_client_lastReceivedPosition = transform.position;
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
