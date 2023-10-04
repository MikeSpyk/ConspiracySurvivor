using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity_SpawnpointPlayer : Entity_damageable
{
    [Header("Entity_SpawnpointPlayer")]
    [SerializeField] private Vector3 m_spawnOffset = Vector3.zero;
    [SerializeField] private float m_cooldownTime = 60f;
    [SerializeField, ReadOnly] private int m_associatedPlayerGameID = -2;
    [SerializeField, ReadOnly] private float m_lastTimeSpawned = 0;
    [SerializeField] private bool DEBUG_showSpawnPoint = false;

    private bool m_delayedPlayerRegistration = false;
    private int m_delayedPlayerRegistrationFrame = -1;
    private int m_delayedPlayerRegistrationPlayerID = -2;

    public Vector3 spawnOffset { get { return m_spawnOffset; } }
    public float lastTimeSpawned { get { return m_lastTimeSpawned; } }
    public float cooldownTime { get { return m_cooldownTime; } }
    public int associatedPlayerGameID { get { return m_associatedPlayerGameID; } }

    protected void Update()
    {
        if (DEBUG_showSpawnPoint)
        {
            Debug.DrawRay(transform.position + transform.rotation * m_spawnOffset, Vector3.up, Color.red);
            Debug.DrawRay(transform.position + transform.rotation * m_spawnOffset, Vector3.right, Color.red);
        }

        if (m_delayedPlayerRegistration && Time.frameCount > m_delayedPlayerRegistrationFrame)
        {
            m_delayedPlayerRegistration = false;
            server_registerSpawnpointForPlayer(m_delayedPlayerRegistrationPlayerID);
        }
    }

    protected void unregisterSpawnpoint()
    {
        ClientUserData userData = NetworkingManager.singleton.server_getClientUserData(m_associatedPlayerGameID);

        if(userData == null)
        {
            Debug.LogWarning("Entity_SpawnpointPlayer: unregisterSpawnpoint: associated player for spawnpoint could not be found");
        }
        else
        {
            userData.removePlayerSpawnpointEntityID(entityUID);
        }
    }

    public void server_registerSpawnpointForPlayer(int playerGameID)
    {
        if (entityUID == -1)
        {
            m_delayedPlayerRegistration = true;
            m_delayedPlayerRegistrationFrame = Time.frameCount + 1;
            m_delayedPlayerRegistrationPlayerID = playerGameID;
        }
        else
        {
            ClientUserData clientData = NetworkingManager.singleton.server_getClientUserData(playerGameID);

            if (clientData != null)
            {
                m_associatedPlayerGameID = playerGameID;
                setSpawnCooldown();
                NetworkingManager.singleton.server_sendSpawnpointCooldown(m_associatedPlayerGameID, entityUID, m_lastTimeSpawned + m_cooldownTime - Time.time);

                if (!clientData.playerSpawnpointsEntityIDs.Contains(entityUID))
                {
                    clientData.addPlayerSpawnpointEntityID(entityUID);
                }
            }
        }
    }

    public bool hasCooldown()
    {
        if (Time.time > m_lastTimeSpawned + m_cooldownTime)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void setSpawnCooldown()
    {
        m_lastTimeSpawned = Time.time;
    }

    protected override void onDamaged(float damage)
    {
        base.onDamaged(damage);

        if(m_health < 0)
        {
            unregisterSpawnpoint();
            Destroy(gameObject);
        }
    }

    public override DataEntity_Base getNewDefaultDataEntity()
    {
        return new DataEntity_PlayerSpawnPoint();
    }

    protected override void updateDataEntity()
    {
        base.updateDataEntity();

        DataEntity_PlayerSpawnPoint dataEntity = m_dataEntity as DataEntity_PlayerSpawnPoint;

        dataEntity.m_playerGameID = m_associatedPlayerGameID;
        dataEntity.m_lastTimeSpawned = m_lastTimeSpawned;
    }

    protected override void updateEntity()
    {
        base.updateEntity();

        DataEntity_PlayerSpawnPoint dataEntity = m_dataEntity as DataEntity_PlayerSpawnPoint;

        m_lastTimeSpawned = dataEntity.m_lastTimeSpawned;
        m_associatedPlayerGameID = dataEntity.m_playerGameID;
    }
}
