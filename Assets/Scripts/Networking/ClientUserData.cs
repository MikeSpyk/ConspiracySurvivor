using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientUserData
{
    public ClientUserData(string username, string passwordHash, string ingameName, int clientID)
    {
        m_username = username;
        m_passwordHash = passwordHash;
        m_ingameName = ingameName;
        m_clientID = clientID;
    }

    private bool m_isAdmin = false;
    private bool m_isConnected = false;
    private bool m_isBanned = false;
    private float m_viewDistance = 700;
    private System.Net.IPEndPoint m_lastIPEndPoint = null;
    private string m_username;
    private string m_passwordHash;
    private string m_ingameName;
    private int m_clientID;
    private List<int> m_playerSpawnpointsEntityIDs = new List<int>();
    private Dictionary<int, float> m_client_spawnpointID_nextTimeSpawned = new Dictionary<int, float>(); // next time spawning possible
    public bool isAdmin { get { return m_isAdmin; } set { m_isAdmin = value; } }
    public bool isConnected { get { return m_isConnected; } set { m_isConnected = value; } }
    public bool isBanned { get { return m_isBanned; } set { m_isBanned = value; } }
    public float viewDistance { get { return m_viewDistance; } set { m_viewDistance = value; } }
    public System.Net.IPEndPoint lastIPEndPoint { get { return m_lastIPEndPoint; } set { m_lastIPEndPoint = value; } }
    public string ingameName { get { return m_ingameName; } set { m_ingameName = value; } }
    public int clientID { get { return m_clientID; } }
    public string username { get { return m_username; } }
    /// <summary>
    /// the (double) hashed password
    /// </summary>
    public string passwordHash { get { return m_passwordHash; } }

    public void addPlayerSpawnpointEntityID(int spawnpointEntityID)
    {
        if (!m_playerSpawnpointsEntityIDs.Contains(spawnpointEntityID))
        {
            m_playerSpawnpointsEntityIDs.Add(spawnpointEntityID);

            if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
            {
                NetworkingManager.singleton.server_sendClientSpawnpointAdd(spawnpointEntityID, clientID);
            }

            if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
            {
                if (m_clientID == -1)
                {
                    GUIManager.singleton.setSpawnSavedLocationVisibility(true);
                }
            }
        }
    }

    public void removePlayerSpawnpointEntityID(int spawnpointEntityID)
    {
        if (m_playerSpawnpointsEntityIDs.Contains(spawnpointEntityID))
        {
            m_playerSpawnpointsEntityIDs.Remove(spawnpointEntityID);

            if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
            {
                NetworkingManager.singleton.server_sendClientSpawnpointRemove(spawnpointEntityID, clientID);
            }

            if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
            {
                if (m_clientID == -1)
                {
                    if (m_playerSpawnpointsEntityIDs.Count < 1)
                    {
                        GUIManager.singleton.setSpawnSavedLocationVisibility(false);
                    }
                }
            }
        }
    }

    public void client_updateSpawnpointCooldown(int spawnpointID, float cooldownDelta)
    {
        if (m_client_spawnpointID_nextTimeSpawned.ContainsKey(spawnpointID))
        {
            m_client_spawnpointID_nextTimeSpawned[spawnpointID] = Time.time + cooldownDelta;
        }
        else
        {
            m_client_spawnpointID_nextTimeSpawned.Add(spawnpointID, Time.time + cooldownDelta);
        }
    }

    private float client_getSpawnpointCooldown(int spawnpointID)
    {
        if (m_client_spawnpointID_nextTimeSpawned.ContainsKey(spawnpointID))
        {
            return m_client_spawnpointID_nextTimeSpawned[spawnpointID] - Time.time;
        }
        else
        {
            return -1;
        }
    }

    public float client_getSpawnpointCooldownIndex(int index)
    {
        if (index > -1 && index < m_playerSpawnpointsEntityIDs.Count)
        {
            return client_getSpawnpointCooldown(m_playerSpawnpointsEntityIDs[index]);
        }
        else
        {
            return -1;
        }
    }

    public List<int> playerSpawnpointsEntityIDs { get { return m_playerSpawnpointsEntityIDs; } }

    public override int GetHashCode()
    {
        return m_clientID;
    }
}
