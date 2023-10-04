using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuildingDoor : Entity_damageable
{
    [Header("PlayerBuildingDoor")]
    [SerializeField] private float m_distanceRotateOrigin;
    [SerializeField] private float m_angleDistance = 90;
    [SerializeField] private float m_rotationSpeed = 100;
    [SerializeField] private float m_timeBeetweenPosRotUpdates = 0.1f;

    public int m_associatedWallEntityID;
    public int m_associatedBuildingID;
    private bool m_stateOpen = false;
    private bool m_isMoving = false;
    private Vector3 m_rotationOrigin;
    private Quaternion m_startMovingRot = Quaternion.identity;
    private Vector3 m_startMovingPos = Vector3.zero;
    private float m_lastTimePosRotUpdate = 0f;
    private Quaternion m_lastRot = Quaternion.identity;

    protected void Start()
    {
        Entity_damageable_Start();

        PlayerRaycastTarget raycastTarget = GetComponent<PlayerRaycastTarget>();

        if (raycastTarget == null)
        {
            Debug.LogError("PlayerBuildingDoor: PlayerRaycastTarget is null");
        }
        else
        {
            raycastTarget.PlayerStartUseEvent += client_PlayerStartUseEvent;
        }

        m_rotationOrigin = transform.position + transform.forward * m_distanceRotateOrigin;

        Debug.DrawRay(m_rotationOrigin, Vector3.up, Color.red, 2f);
    }

    protected void Update()
    {
        if (m_isMoving)
        {
            if (m_stateOpen) // closing door
            {
                transform.RotateAround(m_rotationOrigin, Vector3.up, m_rotationSpeed * Time.deltaTime);
            }
            else // opening door
            {
                transform.RotateAround(m_rotationOrigin, Vector3.up, -m_rotationSpeed * Time.deltaTime);
            }

            if (Mathf.Abs(Mathf.DeltaAngle(transform.rotation.eulerAngles.y, m_startMovingRot.eulerAngles.y)) > m_angleDistance) // reached end position
            {
                transform.rotation = m_startMovingRot;
                transform.position = m_startMovingPos;

                if (m_stateOpen) // closing door
                {
                    transform.RotateAround(m_rotationOrigin, Vector3.up, m_angleDistance);
                    server_sendClosedDoorCommand();
                }
                else // opening door
                {
                    transform.RotateAround(m_rotationOrigin, Vector3.up, -m_angleDistance);
                }

                m_isMoving = false;
                m_stateOpen = !m_stateOpen;
            }
        }

        if (transform.rotation != m_lastRot && Time.time > m_lastTimePosRotUpdate + m_timeBeetweenPosRotUpdates)
        {
            m_lastRot = transform.rotation;
            m_lastTimePosRotUpdate = Time.time;
            server_sendPositionRotation();
        }
    }

    /// <summary>
    /// opens the door if it is closed. closes the door if it is open. doese'nt do anything if the door is currently moving
    /// </summary>
    private void trySwitchDoorState()
    {
        if (!m_isMoving)
        {
            m_startMovingRot = transform.rotation;
            m_startMovingPos = transform.position;
            m_isMoving = true;

            if (!m_stateOpen)
            {
                server_sendOpenDoorCommand();
            }
        }
    }

    private void server_sendPositionRotation()
    {
        NetworkMessage message = getCustomMessageBase();
        message.addIntegerValues((int)CustomMessageContext1.PositionAndRotation);
        message.addFloatValues(transform.position.x, transform.position.y, transform.position.z, transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        server_sendTCPMessageToAllClients(message);
    }

    private void server_sendOpenDoorCommand()
    {
        NetworkMessage message = getCustomMessageBase();
        message.addIntegerValues((int)CustomMessageContext1.Opened);
        server_sendTCPMessageToAllClients(message);
    }

    private void server_sendClosedDoorCommand()
    {
        NetworkMessage message = getCustomMessageBase();
        message.addIntegerValues((int)CustomMessageContext1.Closed);
        server_sendTCPMessageToAllClients(message);
    }

    private void client_PlayerStartUseEvent(object sender, System.EventArgs e)
    {
        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            NetworkMessage message = getCustomMessageBase();
            message.addIntegerValues((int)CustomMessageContext1.PlayerUseEntity);
            client_sendCustomTCPMessage(message);
        }
    }

    public override bool client_receivedCustomNetworkMessage(NetworkMessage message)
    {
        switch (message.getIntValue(1))
        {
            case (int)CustomMessageContext1.PositionAndRotation:
                {
                    if (message.checkInputCorrectness(2, 6, 0))
                    {
                        transform.position = new Vector3(message.getFloatValue(0), message.getFloatValue(1), message.getFloatValue(2));
                        transform.rotation = Quaternion.Euler(message.getFloatValue(3), message.getFloatValue(4), message.getFloatValue(5));
                    }

                    return true;
                }
            case (int)CustomMessageContext1.Opened:
                {
                    if (message.checkInputCorrectness(2, 0, 0))
                    {
                        SoundManager.singleton.playSoundAt(24, transform.position, Sound.SoundPlaystyle.Once);
                    }

                    return true;
                }
            case (int)CustomMessageContext1.Closed:
                {
                    if (message.checkInputCorrectness(2, 0, 0))
                    {
                        SoundManager.singleton.playSoundAt(23, transform.position, Sound.SoundPlaystyle.Once);
                    }

                    return true;
                }
            default:
                {
                    return false;
                }
        }
    }

    public override bool server_receivedCustomNetworkMessage(NetworkMessage message)
    {
        switch (message.getIntValue(1))
        {
            case (int)CustomMessageContext1.PlayerUseEntity:
                {
                    int playerGameID = NetworkingManager.singleton.server_getPlayerGameIDForIPEndpoint(message.iPEndPoint);

                    PlayerBuilding building = PlayerBuildingManager.singleton.getPlayerBuildingForUID(m_associatedBuildingID);

                    if (building != null && building.isPlayerAllowedOpenDoor(playerGameID))
                    {
                        trySwitchDoorState();
                    }

                    return true;
                }
            default:
                {
                    return false;
                }
        }
    }

    public override DataEntity_Base getNewDefaultDataEntity()
    {
        return new DataEntity_BuildingPartDoor();
    }

    protected override void updateDataEntity()
    {
        base.updateDataEntity();

        DataEntity_BuildingPartDoor dataEntity = m_dataEntity as DataEntity_BuildingPartDoor;

        dataEntity.m_associatedWallEntityID = m_associatedWallEntityID;
        dataEntity.m_associatedBuildingID = m_associatedBuildingID;
    }

    protected override void updateEntity()
    {
        base.updateEntity();

        DataEntity_BuildingPartDoor dataEntity = m_dataEntity as DataEntity_BuildingPartDoor;

        m_associatedWallEntityID = dataEntity.m_associatedWallEntityID;
        m_associatedBuildingID = dataEntity.m_associatedBuildingID;
    }
}
