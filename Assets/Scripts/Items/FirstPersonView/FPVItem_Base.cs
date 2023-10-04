using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPVItem_Base : MonoBehaviour
{
    protected enum ItemUsageMode { Once, Continuous }

    [SerializeField] private Vector3 m_modelPositionOffset;
    [SerializeField] private Vector3 m_modelRotationOffset;
    [SerializeField] private Vector3 m_modelScale = new Vector3(1, 1, 1);
    [SerializeField] private bool m_forceShowArms = false;
    //[SerializeField] private int m_firstPersonPrefabIndex = -1;
    [SerializeField] protected float m_timeBetweenItemUsagePrimary = 1f;
    [SerializeField] protected ItemUsageMode m_primaryUsageMode = ItemUsageMode.Continuous;
    [SerializeField] protected ItemUsageMode m_secondaryUsageMode = ItemUsageMode.Continuous;
    [SerializeField, ReadOnly] private int m_hotbarIndex = -1;

    protected Player_base m_carrierPlayer = null;

    public float timeBetweenItemUsagePrimary { get { return m_timeBetweenItemUsagePrimary; } }
    public Vector3 modelPositionOffset { get { return m_modelPositionOffset; } }
    public Vector3 modelRotationOffset { get { return m_modelRotationOffset; } }
    public Vector3 modelScale { get { return m_modelScale; } }
    public bool showFPVArms
    {
        get
        {
            if (m_animator != null)
            {
                return true;
            }

            if (m_forceShowArms)
            {
                return true;
            }

            return false;
        }
    }
    public int hotbarIndex { get { return m_hotbarIndex; } set { setHotbarIndex(value); } }

    protected ClientSettingsManager m_keySource;
    protected Animator m_animator;

    protected float m_lastTimeItemUsagePrimary = 0f;
    protected float m_lastTimeItemUsagePrimaryEnded = 0f;

    protected float m_timeBetweenItemUsageSecondary = 1f;
    protected float m_lastTimeItemUsageSecondary = 0f;

    protected void Start() { }

    protected void Awake()
    {
        m_keySource = ClientSettingsManager.singleton;
        m_animator = GetComponent<Animator>();
    }

    protected void Update()
    {
        if (!GUIManager.singleton.coursorActive)
        {
            if (m_primaryUsageMode == ItemUsageMode.Continuous)
            {
                if (Input.GetKey(m_keySource.getPrimaryActionKey()))
                {
                    if (Time.time > m_lastTimeItemUsagePrimary + m_timeBetweenItemUsagePrimary)
                    {
                        onItemUsagePrimary();
                        m_lastTimeItemUsagePrimary = Time.time;
                    }
                }
                else
                {
                    m_lastTimeItemUsagePrimaryEnded = Time.time;
                    onItemUsagePrimaryEnded();
                }
            }
            else
            {
                if (Input.GetKeyDown(m_keySource.getPrimaryActionKey()))
                {
                    if (Time.time > m_lastTimeItemUsagePrimary + m_timeBetweenItemUsagePrimary)
                    {
                        onItemUsagePrimary();
                        m_lastTimeItemUsagePrimary = Time.time;
                    }
                }
            }

            if (m_secondaryUsageMode == ItemUsageMode.Continuous)
            {
                if (Input.GetKey(m_keySource.getSecondaryActionKey()))
                {
                    if (Time.time > m_lastTimeItemUsageSecondary + m_timeBetweenItemUsageSecondary)
                    {
                        onItemUsageSecondary();
                        m_lastTimeItemUsageSecondary = Time.time;
                    }
                }
                else
                {
                    onItemUsageSecondaryEnded();
                }
            }
            else
            {
                if (Input.GetKeyDown(m_keySource.getSecondaryActionKey()))
                {
                    if (Time.time > m_lastTimeItemUsageSecondary + m_timeBetweenItemUsageSecondary)
                    {
                        onItemUsageSecondary();
                        m_lastTimeItemUsageSecondary = Time.time;
                    }
                }
            }
        }
    }

    protected void OnDestroy()
    {
        if (gameObject.activeSelf)
        {
            //onItemDeactivated(); // TODO: check if game is shutting down first
        }
    }

    public void activateItem()
    {
        gameObject.SetActive(true);
        onItemActivated();
    }

    public void deactivateItem()
    {
        gameObject.SetActive(false);
        onItemDeactivated();
    }

    public void setHotbarIndex(int index)
    {
        m_hotbarIndex = index;
    }

    public void setCarrierPlayer(Player_base player)
    {
        m_carrierPlayer = player;
    }

    #region Networking

    /// <summary>
    /// gets a new NetworkMessage from cache, that includes a integer-Value of the items hotbar index
    /// </summary>
    /// <returns></returns>
    protected NetworkMessage getCustomMessageBase()
    {
        NetworkMessage returnValue = NetworkingManager.singleton.getNewNetworkMessage();
        returnValue.addIntegerValues(m_hotbarIndex);
        return returnValue;
    }

    /// <summary>
    /// sends a networkmessage via UDP to the server
    /// </summary>
    /// <param name="message"></param>
    protected void client_sendCustomUDPMessage(NetworkMessage message)
    {
        message.setMessageContext((int)NetworkingManager.NetMessageContextUDP.FPVCustomMessage);
        NetworkingManager.singleton.client_sendCustomMessageUDP(message);
    }

    /// <summary>
    /// sends a networkmessage via TCP to the server
    /// </summary>
    /// <param name="message"></param>
    protected void client_sendCustomTCPMessage(NetworkMessage message)
    {
        message.setMessageContext((int)NetworkingManager.NetMessageContextTCP.FPVCustomMessage);
        NetworkingManager.singleton.client_sendCustomMessageTCP(message);
    }

    /// <summary>
    /// sends a networkmessage via UDP to the clients that holds that item
    /// </summary>
    /// <param name="message"></param>
    protected void server_sendCustomUDPMessage(NetworkMessage message)
    {
        message.setMessageContext((int)NetworkingManager.NetMessageContextUDP.FPVCustomMessage);
        NetworkingManager.singleton.server_sendCustomMessageUDP(message, m_carrierPlayer.m_gameID);
    }

    /// <summary>
    /// sends a networkmessage via TCP to the clients that holds that item
    /// </summary>
    /// <param name="message"></param>
    protected void server_sendCustomTCPMessage(NetworkMessage message)
    {
        message.setMessageContext((int)NetworkingManager.NetMessageContextTCP.FPVCustomMessage);
        NetworkingManager.singleton.server_sendCustomMessageTCP(message, m_carrierPlayer.m_gameID);
    }

    /// <summary>
    /// the interface for entities to receive messages from the server
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

    protected virtual void onItemUsagePrimary() { }

    protected virtual void onItemUsagePrimaryEnded() { }

    protected virtual void onItemUsageSecondary() { }

    protected virtual void onItemUsageSecondaryEnded() { }

    protected virtual void onItemActivated()
    {
        Animator arms = FirstPersonViewManager.singleton.firstPersonArmsAnimator;

        if (arms != null && arms.gameObject.activeSelf && arms.isInitialized)
        {
            arms.Rebind();
        }
    }

    protected virtual void onItemDeactivated() { }
}
