using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class Entity_base : MonoBehaviour
{
    [Header("Entity_base")]
    [SerializeField, ReadOnly] protected int m_Entity_UID = -1; // UID provieded by the entitymanager
    [SerializeField, ReadOnly] private int m_Entity_GridFieldID = -2;
    [SerializeField] protected int m_prefabIndex = -1; // must be the same as in index of the entitymanager prefab array
    [SerializeField] private bool m_cullableServer = true; // should this entity be affected by culling if no player is nearby or should it always be present on the server.

    public List<int> m_observingPlayersID = null; // gameID of players that can see this entity (server only)
    public List<int> m_observingDistantPlayersID = null; // gameID of players that can see this entity as a distant object (server only)
    protected DataEntity_Base m_dataEntity = null; // data to keep in culling state

    public int entity_gridFieldID { get { return m_Entity_GridFieldID; } }

    #region Unity Methods

    protected void Awake()
    {
        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            m_observingPlayersID = new List<int>();
            m_observingDistantPlayersID = new List<int>();
        }
    }

    protected void Start()
    {
        onRegisterEntity();
        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            if (m_dataEntity == null)
            {
                // if created by instantiating on server
                initializeDefaultDataEntity();
            }
        }

        if (m_Entity_GridFieldID == -2)
        {
            WorldGridField field = EntityManager.singleton.getGridFieldForPostion(new Vector2(transform.position.x, transform.position.z));
            field.activeEntities.Add(this);
            m_Entity_GridFieldID = field.uid;
        }
    }

    protected void OnDestroy()
    {
        if (EntityManager.singleton != null)
        {
            onUnregisterEntity();
        }
    }

    #endregion

    public void setEntityUID(int UID)
    {
        if (m_Entity_UID == -1)
        {
            m_Entity_UID = UID;
        }
        else
        {
            Debug.LogWarning("Entity_base: setEntityUID: Entity UID was already set");
        }
    }

    public virtual void server_onClientUse(int gameID) { }

    protected void updateWorldGridAssignment()
    {
        WorldGridField latestField = EntityManager.singleton.getGridFieldForUID(m_Entity_GridFieldID);

        if (latestField.uid == (int)WorldGrid.SpecialFieldUIDs.OutOfBound)
        {
            WorldGridField newField = EntityManager.singleton.getGridFieldForPostion(new Vector2(transform.position.x, transform.position.z));

            if (newField != latestField)
            {
                latestField.activeEntities.Remove(this);
                latestField = newField;
                latestField.activeEntities.Add(this);
                m_Entity_GridFieldID = latestField.uid;
            }
        }
        else if (!latestField.isWithin(new Vector2(transform.position.x, transform.position.z)))
        {
            latestField.activeEntities.Remove(this);
            latestField = EntityManager.singleton.getGridFieldForPostion(new Vector2(transform.position.x, transform.position.z));
            latestField.activeEntities.Add(this);
            m_Entity_GridFieldID = latestField.uid;
        }
    }

    #region Registration

    protected virtual void onRegisterEntity()
    {
        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            if (m_Entity_UID == -1)
            {
                m_Entity_UID = EntityManager.singleton.registerNewEntity(this);
            }
        }
    }

    protected virtual void onUnregisterEntity()
    {
        EntityManager.singleton.unregisterEntity(this);
    }

    #endregion

    #region Data Entity

    /// <summary>
    /// synchronizes the data entity with the entity and returns it
    /// </summary>
    /// <returns></returns>
    public DataEntity_Base getUpdatedDataEntity()
    {
        updateDataEntity();
        return m_dataEntity;
    }

    public void setDataEntity(DataEntity_Base dataEntity)
    {
        m_dataEntity = dataEntity;
        updateEntity();
    }

    public void removeDataEntity()
    {
        m_dataEntity = null;
    }

    private void initializeDefaultDataEntity()
    {
        m_dataEntity = getNewDefaultDataEntity();
    }

    /// <summary>
    /// synchronizes the data entity with this entity
    /// </summary>
    protected virtual void updateDataEntity() // with entity (this)
    {
        m_dataEntity.m_entityPrefabID = m_prefabIndex; // is this ok ?
        m_dataEntity.m_gridFieldUID = m_Entity_GridFieldID;
        m_dataEntity.m_entity_UID = m_Entity_UID;
        m_dataEntity.m_position = transform.position;
        m_dataEntity.m_rotation = transform.rotation;
        m_dataEntity.m_observingDistantPlayers = m_observingDistantPlayersID;
    }

    /// <summary>
    /// synchronizes this entity with the data entity
    /// </summary>
    protected virtual void updateEntity() // with data entity
    {
        m_Entity_UID = m_dataEntity.m_entity_UID;
        m_Entity_GridFieldID = m_dataEntity.m_gridFieldUID;
        transform.position = m_dataEntity.m_position;
        transform.rotation = m_dataEntity.m_rotation;
        m_observingDistantPlayersID = m_dataEntity.m_observingDistantPlayers;
    }

    /// <summary>
    /// gets the default data-entity fitting for this entity
    /// </summary>
    /// <returns></returns>
    public virtual DataEntity_Base getNewDefaultDataEntity()
    {
        return new DataEntity_Base();
    }

    #endregion

    #region Networking

    /// <summary>
    /// gets a NetworkMessage that includes the entity UID
    /// </summary>
    /// <returns></returns>
    protected NetworkMessage getCustomMessageBase()
    {
        NetworkMessage returnValue = NetworkingManager.singleton.getNewNetworkMessage();
        returnValue.addIntegerValues(m_Entity_UID);
        return returnValue;
    }

    /// <summary>
    /// fill the messages, that gets send to a client if this entity gets unculled. this is the counterpart to client_receiveUncullMessage
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual NetworkMessage server_fillUncullingMessage(NetworkMessage message)
    {
        message.setMessageContext((int)NetworkingManager.NetMessageContextTCP.EntityUncull);
        message.addIntegerValues(m_Entity_UID, m_prefabIndex);
        message.addFloatValues(transform.position.x, transform.position.y, transform.position.z, transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        return message;
    }

    /// <summary>
    /// fills this entity with data provided from the server within the network message. this is the counterpart to server_fillUncullingMessage. Custom Message payload starts at float-index 6 and int-index 2
    /// </summary>
    /// <param name="message"></param>
    public virtual void client_receiveUncullMessage(NetworkMessage message)
    {
        // message values counts for entity_base has already been checked by entity manager
        m_Entity_UID = message.getIntValue(0);
    }

    /// <summary>
    /// sends a networkmessage via UDP to the server
    /// </summary>
    /// <param name="message"></param>
    protected void client_sendCustomUDPMessage(NetworkMessage message)
    {
        message.setMessageContext((int)NetworkingManager.NetMessageContextUDP.EntityCustomMessage);
        NetworkingManager.singleton.client_sendCustomMessageUDP(message);
    }

    /// <summary>
    /// sends a networkmessage via TCP to the server
    /// </summary>
    /// <param name="message"></param>
    protected void client_sendCustomTCPMessage(NetworkMessage message)
    {
        message.setMessageContext((int)NetworkingManager.NetMessageContextTCP.EntityCustomMessage);
        NetworkingManager.singleton.client_sendCustomMessageTCP(message);
    }

    /// <summary>
    /// sends a networkmessage via UDP to the clients with the given GameID
    /// </summary>
    /// <param name="message"></param>
    /// <param name="receiverGameID"></param>
    protected void server_sendCustomUDPMessage(NetworkMessage message, int receiverGameID)
    {
        message.setMessageContext((int)NetworkingManager.NetMessageContextUDP.EntityCustomMessage);
        NetworkingManager.singleton.server_sendCustomMessageUDP(message, receiverGameID);
    }

    /// <summary>
    /// sends a networkmessage via TCP to the clients with the given GameID
    /// </summary>
    /// <param name="message"></param>
    /// <param name="receiverGameID"></param>
    protected void server_sendCustomTCPMessage(NetworkMessage message, int receiverGameID)
    {
        message.setMessageContext((int)NetworkingManager.NetMessageContextTCP.EntityCustomMessage);
        NetworkingManager.singleton.server_sendCustomMessageTCP(message, receiverGameID);
    }

    /// <summary>
    /// sends a networkmessage via TCP to all clients, that can see this entity
    /// </summary>
    /// <param name="message"></param>
    protected void server_sendTCPMessageToAllClients(NetworkMessage message)
    {
        message.setMessageContext((int)NetworkingManager.NetMessageContextTCP.EntityCustomMessage);

        byte[] temp_bytes;
        int temp_int;

        if (message.getOutput(out temp_bytes, out temp_int, true, false))
        {
            for (int i = 0; i < m_observingPlayersID.Count; i++)
            {
                NetworkMessage temp_message = NetworkingManager.singleton.getNewNetworkMessage();
                temp_message.copyOutputDataFrom(message);

                NetworkingManager.singleton.server_sendCustomMessageTCP(temp_message, m_observingPlayersID[i]);
            }
        }
        else
        {
            Debug.LogWarning("Entity_base: server_sendTCPMessageToAllClients: encoding message failed: " + message.getOutputMessageBitView());
        }

        NetworkingManager.singleton.recyleNetworkMessage(message);
    }
    /// <summary>
    /// sends a networkmessage via TCP to all clients, that can see this entity except the given player IDs in exceptions
    /// </summary>
    /// <param name="message"></param>
    protected void server_sendTCPMessageToAllClients(NetworkMessage message, params int[] exceptions)
    {
        message.setMessageContext((int)NetworkingManager.NetMessageContextTCP.EntityCustomMessage);

        byte[] temp_bytes;
        int temp_int;
        bool skip;

        if (message.getOutput(out temp_bytes, out temp_int, true, false))
        {
            skip = false;

            for (int i = 0; i < m_observingPlayersID.Count; i++)
            {
                for (int j = 0; j < exceptions.Length; j++)
                {
                    if (m_observingPlayersID[i] == exceptions[j])
                    {
                        skip = true;
                        break;
                    }
                }

                if (skip)
                {
                    continue;
                }

                NetworkMessage temp_message = NetworkingManager.singleton.getNewNetworkMessage();
                temp_message.copyOutputDataFrom(message);
                NetworkingManager.singleton.server_sendCustomMessageTCP(temp_message, m_observingPlayersID[i]);
            }
        }
        else
        {
            Debug.LogWarning("Entity_base: server_sendTCPMessageToAllClients: encoding message failed: " + message.getOutputMessageBitView());
        }

        NetworkingManager.singleton.recyleNetworkMessage(message);
    }

    /// <summary>
    /// sends a networkmessage via UDP to all clients, that can see this entity
    /// </summary>
    /// <param name="message"></param>
    protected void server_sendUDPMessageToAllClients(NetworkMessage message)
    {
        message.setMessageContext((int)NetworkingManager.NetMessageContextUDP.EntityCustomMessage);

        byte[] temp_bytes;
        int temp_int;

        if (message.getOutput(out temp_bytes, out temp_int, true, false))
        {
            for (int i = 0; i < m_observingPlayersID.Count; i++)
            {
                NetworkMessage temp_message = NetworkingManager.singleton.getNewNetworkMessage();
                temp_message.copyOutputDataFrom(message);
                NetworkingManager.singleton.server_sendCustomMessageUDP(temp_message, m_observingPlayersID[i]);
            }
        }
        else
        {
            Debug.LogWarning("Entity_base: server_sendUDPMessageToAllClients: encoding message failed: " + message.getOutputMessageBitView());
        }

        NetworkingManager.singleton.recyleNetworkMessage(message);
    }
    /// <summary>
    /// sends a networkmessage via UDP to all clients, that can see this entity except the given player IDs in exceptions
    /// </summary>
    /// <param name="message"></param>
    protected void server_sendUDPMessageToAllClients(NetworkMessage message, params int[] exceptions)
    {
        message.setMessageContext((int)NetworkingManager.NetMessageContextUDP.EntityCustomMessage);

        byte[] temp_bytes;
        int temp_int;

        bool skip;

        if (message.getOutput(out temp_bytes, out temp_int, true, false))
        {
            for (int i = 0; i < m_observingPlayersID.Count; i++)
            {
                skip = false;

                for (int j = 0; j < exceptions.Length; j++)
                {
                    if (m_observingPlayersID[i] == exceptions[j])
                    {
                        skip = true;
                        break;
                    }
                }

                if (skip)
                {
                    continue;
                }

                NetworkMessage temp_message = NetworkingManager.singleton.getNewNetworkMessage();
                temp_message.copyOutputDataFrom(message);
                NetworkingManager.singleton.server_sendCustomMessageUDP(temp_message, m_observingPlayersID[i]);
            }
        }
        else
        {
            Debug.LogWarning("Entity_base: server_sendUDPMessageToAllClients: encoding message failed: " + message.getOutputMessageBitView());
        }

        NetworkingManager.singleton.recyleNetworkMessage(message);
    }

    /// <summary>
    /// the interface for entities to receive messages from the server. it is guaranteed that the message has at least 2 integer values [0:EntityID, 1:MessageContext]
    /// </summary>
    /// <param name="message"></param>
    /// <returns>returns true if message was already handled</returns>
    public virtual bool client_receivedCustomNetworkMessage(NetworkMessage message)
    {
        switch (message.getIntValue(1))
        {
            default:
                {
                    return false;
                }
        }
    }

    /// <summary>
    /// the interface for entities to receive messages from a client
    /// </summary>
    /// <param name="message"></param>
    /// <returns>returns true if message was already handled</returns>
    public virtual bool server_receivedCustomNetworkMessage(NetworkMessage message)
    {
        switch (message.getIntValue(1))
        {
            default:
                {
                    return false;
                }
        }
    }

    #endregion

    public override int GetHashCode()
    {
        if (m_Entity_UID < 0)
        {
            Debug.LogError("Entity_base: GetHashCode: Entity UID not set (UID = \"" + m_Entity_UID + "\"). Hash code not unique !");
            return -1;
        }
        else
        {
            return m_Entity_UID;
        }
    }

    public int getEntityUID()
    {
        return m_Entity_UID;
    }

    public int getPrefabIndex()
    {
        return m_prefabIndex;
    }

    public bool cullable
    {
        get
        {
            return m_cullableServer;
        }
    }

    public int prefabIndex
    {
        get
        {
            return m_prefabIndex;
        }
    }

    public int entityUID
    {
        get
        {
            return m_Entity_UID;
        }
    }

    public static bool server_isPlayerAlive(System.Net.IPEndPoint ipEndPoint)
    {
        return EntityManager.singleton.isPlayerAlive(NetworkingManager.singleton.server_getPlayerGameIDForIPEndpoint(ipEndPoint));
    }
}
