#define SHOW_INPUT_MESSAGES
#undef SHOW_INPUT_MESSAGES
#define CONNECTION_DEBUG
#undef CONNECTION_DEBUG

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;

public class NetworkingManager : MonoBehaviour
{
    public static NetworkingManager singleton;

    public enum MessageType { Request = 1, Confirmation = 2, Success = 3, Ping = 4, Pong = 5, Command = 6, Add = 7, Remove = 7, DEBUG_Position = 8 }
    public enum ConnectionStartResult { Success, Unknown_Failure, TCP_Failed, UDP_Failed }
    public enum HostRole { Client, Server, ServerAndClient, Uninitialized }

    public enum NetMessageContextUDP { ProjectileStart = 1, ProjectileUpdate = 2, Ping = 3, HealthUpdate = 4, HitConfirmation = 5, WorldSound = 6, EntityCustomMessage = 7, FPVCustomMessage = 8 }

    public enum NetMessageContextTCP
    {
        ChangeItemInventory = 0, EntityUsed = 1, OpenContainer = 2, CloseContainer = 3, DropItem = 4, PickUpItem = 5, WorldParameters = 6, PlayerState = 7, EstablishConnection = 8,
        Disconnect = 9, Login = 10, PlayerSpawn = 11, PlayerSuicide = 12, EntityCustomMessage = 13, EntityUncull = 14, EntityDistantUncull = 15, EntityCull = 16, EntityDistantCull = 17,
        PlayerEntityUID = 18, FPVCustomMessage = 19, SplitInventoryItem = 20, ViewPointPosition = 21, GlobalSound = 22, ViewDistance = 23, SpawnpointUpdate = 24, SpawnpointCooldown = 25,
        ParticleEffect = 26, WorldTime = 27, WakeUp = 28
    }

    private const float m_package_wait_time = 0.1f; // request again afer this time

    #region General members

    [Header("Configuration")]
    [SerializeField] private float m_timeBetweenPings = 1f;
    [SerializeField] private float m_disconnectAfterSeconds = 20f; // server disconnect a client and client disconnect from server
    [SerializeField] private float m_timeBetweenHealthUpdates = 1f;
    [SerializeField] private float m_setViewDistanceCooldown = 2f; // how long has a client to wait to set its view distance before beeing able to set it again
    [SerializeField] private bool m_noResponseAutoKicking = true;
    [SerializeField] private bool m_useIPV4 = true;

    [Header("Debug")]
    [SerializeField] private bool m_discardInputPackages = false;
    [SerializeField] private bool m_discardOutputPackages = false;
    [SerializeField] private bool m_DEBUG_showReleasedMessages = false;
    [SerializeField, ReadOnly] private int m_connectedClientsCount = 0;
    [SerializeField, ReadOnly] private float[] m_output_clients_UDPLatency;
    [SerializeField, ReadOnly] private float m_output_client_UDPLatency = 0;

    private System.Security.Cryptography.HashAlgorithm m_SHA256HashAlgorithm = System.Security.Cryptography.SHA256.Create();
    private NetworkMessageManager m_networkMessageManager = new NetworkMessageManager(true);
    private HostRole m_hostRole = HostRole.Uninitialized;
    private bool m_isInitialized = false;
    private int m_myPort = -1;
    private int m_UDPServerLatencyPingPackageCounter = 0;
    private float m_serverLastTimePing = 0;
    private int m_healthPackageCounter = 0;
    private int m_receivedMessagesCounter = 0;
    private int m_receivedBytes = 0;

    #endregion

    #region Server only members

    // gameID is the ID the game uses to identify players (the gameID is smaller and easier to handle and to send, than a long clientID) clientID should only be used to identify the players gameID once they connect
    // think of the client ID as the Login Data and the gameID as the ID that identifies everything that belongs to a player
    private Dictionary<int, IPEndPoint> m_clientID_clientEndPoint = new Dictionary<int, IPEndPoint>();
    private Dictionary<int, int> m_clientID_gameID = new Dictionary<int, int>();
    private Dictionary<int, int> m_gameID_clientID = new Dictionary<int, int>();
    private Dictionary<int, IPAddress> m_gameID_clientIPAdress = new Dictionary<int, IPAddress>();
    private Dictionary<int, IPEndPoint> m_gameID_clientEndPoint = new Dictionary<int, IPEndPoint>();
    private Dictionary<int, uintRef> m_gameID_playerPositionsPackageUID = new Dictionary<int, uintRef>();
    private Dictionary<int, uintRef> m_gameID_playerRotationsPackageUID = new Dictionary<int, uintRef>();
    private Dictionary<int, floatRef> m_gameID_UDPLatency = new Dictionary<int, floatRef>();
    private Dictionary<int, floatRef> m_gameID_timeLastPong = new Dictionary<int, floatRef>(); // ping - pong
    private Dictionary<IPEndPoint, int> m_clientEndPoint_gameID = new Dictionary<IPEndPoint, int>();
    private Dictionary<IPEndPoint, uintRef> m_clientEndPoint_HealthUpdatePackageID = new Dictionary<IPEndPoint, uintRef>(); // Package ID 0 means no messages needs to be send
    private Dictionary<IPEndPoint, floatRef> m_clientEndPoint_PlayerHealth = new Dictionary<IPEndPoint, floatRef>();
    private Dictionary<IPEndPoint, floatRef> m_newTCPConnections_time = new Dictionary<IPEndPoint, floatRef>();
    private Dictionary<IPEndPoint, floatRef> m_newUDPConnections_time = new Dictionary<IPEndPoint, floatRef>();
    private HashSet<IPAddress> m_bannedIPAddresses = new HashSet<IPAddress>();
    private Dictionary<int, ClientUserData> m_clientID_registeredClients = new Dictionary<int, ClientUserData>();
    private Dictionary<IPEndPoint, floatRef> m_clientEndPoint_viewDistanceUpdateTime = new Dictionary<IPEndPoint, floatRef>();

    private UDPInterface m_serverUDPInterface;
    private TCP_Server m_TCPServerInterface;
    private float m_lastTime_healthUpdates = 0;
    private int m_gameIDCounter = 0;

    #endregion

    #region Client only members

    private bool m_clientSendViewDistanceAgain = false;
    private float m_clientSendViewDistanceAgainTime = 0f;
    private UDPInterface m_clientUDPInterface = null;
    private TCP_Client m_TCPClient = null;
    private IPAddress m_externServerIP = null;
    private IPEndPoint m_externServerEndPoint = null;
    private float m_clientLastTimeSendPing = 0;
    private float m_clientLastTime_receivedLatencyPing = 0;
    private float m_lastLatency = 0;
    private int m_clientPingPackageCounter = 0;
    private string m_clientUsername;
    private string m_clientUserPasswordHash;
    private string m_clientUserIngameName;
    private ClientUserData m_client_clientUserData = null;

    #endregion

    #region Unity Methodes (Awake,Update,Destroy...)

    void Awake()
    {
        singleton = this;
    }

    void Update()
    {
        if (m_isInitialized)
        {
            m_receivedMessagesCounter = 0;
            m_receivedBytes = 0;

            if (m_hostRole == HostRole.Client || m_hostRole == HostRole.ServerAndClient)
            {
                m_clientUDPInterface.m_discardInputPackages = m_discardInputPackages; //  Debug
                m_clientUDPInterface.m_discardOutputPackages = m_discardOutputPackages; // Debug

                clientUpdate();
            }
            if (m_hostRole == HostRole.Server || m_hostRole == HostRole.ServerAndClient)
            {
                m_serverUDPInterface.m_discardInputPackages = m_discardInputPackages; //  Debug
                m_serverUDPInterface.m_discardOutputPackages = m_discardOutputPackages; // Debug

                serverUpdate();
            }

            GUIManager.singleton.addStatsPackageCounter(m_receivedMessagesCounter);
            GUIManager.singleton.addStatsReceivedBytes(m_receivedBytes);
        }

        if (m_DEBUG_showReleasedMessages)
        {
            m_DEBUG_showReleasedMessages = false;
            m_networkMessageManager.logReleasedMessages();
        }
    }

    void OnDestroy()
    {
        shutdownNetworkSockets();
    }

    void OnApplicationQuit()
    {
        shutdownNetworkSockets();
    }

    #endregion

    #region Host initialization

    /// <summary>
    /// starts the host as a Server with a client on the same instance
    /// </summary>
    /// <param name="serverPort">Server port.</param>
    public void initializeAsServerAndClient(int port, out ConnectionStartResult result)
    {
        System.Net.Sockets.SocketException UDPInterfaceError;

        result = ConnectionStartResult.Unknown_Failure;

        bool temp_UDPstartSuccess;
        bool temp_TCPstartSuccess;

        shutdownNetworkSockets();

        m_clientUDPInterface = new UDPInterface(m_useIPV4, m_networkMessageManager);
        m_serverUDPInterface = new UDPInterface(port, m_useIPV4, out temp_UDPstartSuccess, out UDPInterfaceError, m_clientUDPInterface, m_networkMessageManager);

        m_clientUDPInterface.localClient_SetServer(m_serverUDPInterface);

        if (temp_UDPstartSuccess)
        {
            if (m_useIPV4)
            {
                m_TCPClient = new TCP_Client(m_networkMessageManager, new IPEndPoint(IPAddress.Any, port));
            }
            else
            {
                m_TCPClient = new TCP_Client(m_networkMessageManager, new IPEndPoint(IPAddress.IPv6Any, port));
            }

            m_TCPServerInterface = new TCP_Server(port, m_useIPV4, out temp_TCPstartSuccess, m_TCPClient, m_networkMessageManager);
            m_TCPClient.localClient_setServer(m_TCPServerInterface);
        }
        else
        {
            result = ConnectionStartResult.UDP_Failed;
            return;
        }

        if (temp_TCPstartSuccess)
        {
            result = ConnectionStartResult.Success;
            m_hostRole = HostRole.ServerAndClient;
            m_myPort = port;
            m_isInitialized = true;
            m_externServerEndPoint = m_TCPServerInterface.localEndPoint;
        }
        else
        {
            result = ConnectionStartResult.TCP_Failed;
            return;
        }

        if (m_useIPV4)
        {
            server_startConnectionForClient(new IPEndPoint(IPAddress.Any, m_myPort), -1, "LOCALPLAYER", "PASSWORD", "LOCALPLAYER");
        }
        else // ipv6
        {
            server_startConnectionForClient(new IPEndPoint(IPAddress.IPv6Any, m_myPort), -1, "LOCALPLAYER", "PASSWORD", "LOCALPLAYER");
        }
    }

    /// <summary>
    /// starts the host as a Server
    /// </summary>
    /// <param name="serverPort">Server port.</param>
    public void initializeAsServer(int port, out ConnectionStartResult result)
    {
        System.Net.Sockets.SocketException UDPInterfaceError;

        result = ConnectionStartResult.Unknown_Failure;

        bool temp_UDPstartSuccess;
        bool temp_TCPstartSuccess;

        shutdownNetworkSockets();

        m_serverUDPInterface = new UDPInterface(port, m_useIPV4, out temp_UDPstartSuccess, out UDPInterfaceError, null, m_networkMessageManager);

        if (temp_UDPstartSuccess)
        {
            m_TCPServerInterface = new TCP_Server(port, m_useIPV4, out temp_TCPstartSuccess, null, m_networkMessageManager);
        }
        else
        {
            result = ConnectionStartResult.UDP_Failed;
            return;
        }

        if (temp_TCPstartSuccess)
        {
            result = ConnectionStartResult.Success;
            m_hostRole = HostRole.Server;
            m_myPort = port;
            m_isInitialized = true;
        }
        else
        {
            result = ConnectionStartResult.TCP_Failed;
            return;
        }
    }

    /// <summary>
    /// starts the host as a a Client
    /// </summary>
    /// <param name="serverIPAdress">Server IP adress.</param>
    /// <param name="serverPort">Server port.</param>
	public void initializeAsClient(IPAddress serverIPAdress, int serverPort, int clientPort, string clientUsername, string clientPasswordHash, string clientIngameName, out ConnectionStartResult result, out System.Net.Sockets.SocketException outputException)
    {
        if (serverIPAdress.Equals(IPAddress.Parse("127.0.0.1")))
        {
            Debug.LogWarning("!!!!!!!!! local client tcp-connection may not work properly. please don't use one server-application and one client-application on the same machine !!!!!!!!!"); // some packets are lost randomly. Tested with server(build) and client(editor) not working.
        }

        // variables
        client_createClientUserData(clientUsername, clientPasswordHash, clientIngameName);

        result = ConnectionStartResult.Unknown_Failure;
        outputException = null;

        bool temp_UDPstartSuccess = false;
        bool temp_TCPstartSuccess = false;
        System.Net.Sockets.SocketException TCPException = null;
        System.Net.Sockets.SocketException UDPException = null;

        shutdownNetworkSockets();

        // start UDP
        m_clientUDPInterface = new UDPInterface(clientPort, m_useIPV4, out temp_UDPstartSuccess, out UDPException, null, m_networkMessageManager);

        if (temp_UDPstartSuccess)
        {
            m_TCPClient = new TCP_Client(clientPort, m_useIPV4, serverPort, serverIPAdress, out temp_TCPstartSuccess, out TCPException, m_networkMessageManager);
        }
        else
        {
            outputException = UDPException;
            result = ConnectionStartResult.UDP_Failed;
            return;
        }

        // start TCP
        if (temp_TCPstartSuccess)
        {
            result = ConnectionStartResult.Success;

            m_hostRole = HostRole.Client;
            m_myPort = clientPort;
            m_externServerIP = serverIPAdress;
            m_clientUsername = clientUsername;
            m_clientUserPasswordHash = clientPasswordHash;
            m_clientUserIngameName = clientIngameName;
            m_externServerEndPoint = new IPEndPoint(serverIPAdress, serverPort);
            m_isInitialized = true;

            m_clientLastTime_receivedLatencyPing = Time.realtimeSinceStartup;

            return;
        }
        else
        {
            outputException = TCPException;
            result = ConnectionStartResult.TCP_Failed;
            return;
        }
    }

    #endregion

    #region Update Routine (serverUpdate,clientUpdate)

    private void serverUpdate()
    {
        server_checkConnections();

        serverProcessIncomingUDPData();
        serverProcessIncomingTCPData();

        server_UDPLatencyPingManagement();

        if (m_noResponseAutoKicking)
        {
            server_autoKickManagement();
        }
    }

    private void clientUpdate()
    {
        clientProcessIncomingUDPData();
        client_checkTCPConnectionLoss();
        clientProcessIncomingTCPData();

        client_UDPLatencyPingManagement();
        if (m_hostRole != HostRole.ServerAndClient)
        {
            if (Time.realtimeSinceStartup > m_clientLastTime_receivedLatencyPing + m_disconnectAfterSeconds)
            {
                GameManager_Custom.singleton.client_disconnected("Server is not responding (UDP)");
            }
        }

        if (m_clientSendViewDistanceAgain)
        {
            if (Time.realtimeSinceStartup > m_clientSendViewDistanceAgainTime)
            {
                client_sendViewDistance(ClientSettingsManager.singleton.viewDistance);
                m_clientSendViewDistanceAgain = false;
            }
        }
    }

    #endregion

    #region Message Handling

    private void serverProcessIncomingUDPData()
    {
        Queue<NetworkMessage> inputData = m_serverUDPInterface.colletDataBuffer();
        NetMessageContextUDP currentContext;
        NetworkMessage currentMessage;
        m_receivedMessagesCounter += inputData.Count;

        while (inputData.Count > 0)
        {
            currentMessage = inputData.Dequeue();
            m_receivedBytes += currentMessage.getByteLength();

            if (!m_gameID_clientEndPoint.ContainsValue(currentMessage.iPEndPoint)) // if client not connected 
            {
                if (!m_newUDPConnections_time.ContainsKey(currentMessage.iPEndPoint))
                {
                    m_newUDPConnections_time.Add(currentMessage.iPEndPoint, new floatRef(Time.realtimeSinceStartup));
                }
                m_networkMessageManager.recycleNetworkMessage(currentMessage);
                continue;
            }

            currentContext = (NetMessageContextUDP)currentMessage.messageContextID;

            /*
            if (currentContext != NetMessageContextUDP.Ping)
            {
                Debug.Log("NetworkingManager:serverProcessIncomingUDPData: Context: \"" + currentContext + "\", Data: \"" + currentMessage.ToString());
            }
            */

            switch (currentContext)
            {
                case NetMessageContextUDP.Ping:
                    {
                        if (checkMessage(currentMessage, 2, 0, 0))
                        {
                            if (currentMessage.getIntValue(0) == (int)MessageType.Pong)
                            {
                                if (currentMessage.getIntValue(1) == m_UDPServerLatencyPingPackageCounter)
                                {
                                    floatRef pingTime = m_gameID_UDPLatency[m_clientEndPoint_gameID[currentMessage.iPEndPoint]];
                                    pingTime.m_float = Time.realtimeSinceStartup - m_serverLastTimePing;
                                    m_gameID_timeLastPong[m_clientEndPoint_gameID[currentMessage.iPEndPoint]].m_float = Time.realtimeSinceStartup;
                                }
                                else
                                {
                                    //Debug.Log("NetworkingManager: Server: Pong too late");
                                }
                            }
                            else if (currentMessage.getIntValue(0) == (int)MessageType.Ping)
                            {
                                server_sendPong(currentMessage.iPEndPoint, currentMessage.getIntValue(1));
                            }
                        }
                        break;
                    }
                case NetMessageContextUDP.ProjectileStart:
                    {
                        if (checkMessage(currentMessage, 1, 6, 0))
                        {
                            ProjectileManager.singleton.server_addGunshot(new Vector3(currentMessage.getFloatValue(0), currentMessage.getFloatValue(1), currentMessage.getFloatValue(2)),
                                                                      new Vector3(currentMessage.getFloatValue(3), currentMessage.getFloatValue(4), currentMessage.getFloatValue(5)),
                                                                      currentMessage.getIntValue(0),
                                                                      m_clientEndPoint_gameID[currentMessage.iPEndPoint]);

                            GameManager_Custom.singleton.server_sendSoundToPlayersInRange(ProjectileManager.singleton.getShotSoundIndex(currentMessage.getIntValue(0)),
                                                                                            new Vector3(currentMessage.getFloatValue(0), currentMessage.getFloatValue(1), currentMessage.getFloatValue(2)),
                                                                                            m_clientEndPoint_gameID[currentMessage.iPEndPoint]);

                        }
                        break;
                    }
                case NetMessageContextUDP.HealthUpdate:
                    {
                        if (checkMessage(currentMessage, 1, 0, 0))
                        {
                            if (currentMessage.getIntValue(0) == m_clientEndPoint_HealthUpdatePackageID[currentMessage.iPEndPoint].m_uint) // if latest package
                            {
                                m_clientEndPoint_HealthUpdatePackageID[currentMessage.iPEndPoint].m_uint = 0; // disable requesting confirmation
                            }
                        }
                        break;
                    }
                case NetMessageContextUDP.EntityCustomMessage:
                    {
                        EntityManager.singleton.server_receivedCustomEntityMessage(currentMessage);
                        break;
                    }
                case NetMessageContextUDP.FPVCustomMessage:
                    {
                        FirstPersonViewManager.singleton.server_receivedCustomMessage(currentMessage, m_clientEndPoint_gameID[currentMessage.iPEndPoint]);
                        break;
                    }
                default:
                    {
                        Debug.LogError("Unknown NetMessageContext: \"" + (int)currentContext + "\"");
                        break;
                    }
            }

            m_networkMessageManager.recycleNetworkMessage(currentMessage);
        }
    }

    private void serverProcessIncomingTCPData()
    {
        Queue<NetworkMessage> inputData = m_TCPServerInterface.collectAllClientDataBuffers();

        NetMessageContextTCP currentContext;
        NetworkMessage currentMessage;
        m_receivedMessagesCounter += inputData.Count;

        while (inputData.Count > 0)
        {
            currentMessage = inputData.Dequeue();
            m_receivedBytes += currentMessage.getByteLength();

#if SHOW_INPUT_MESSAGES
            Debug.Log("NetworkManager: Server Input TCP: " + currentMessage.ToString());
#endif

            if (currentMessage.messageType == NetworkMessage.MessageType.TCP_End) // TCP end request
            {
                Debug.Log("NetworkingManager: TCP-closing Request from: " + currentMessage.iPEndPoint.ToString());

                if (m_clientEndPoint_gameID.ContainsKey(currentMessage.iPEndPoint))
                {
                    if (m_gameID_clientID.ContainsKey(m_clientEndPoint_gameID[currentMessage.iPEndPoint]))
                    {
                        server_closeConnectionForClient(m_gameID_clientID[m_clientEndPoint_gameID[currentMessage.iPEndPoint]]);
                    }
                }

                m_TCPServerInterface.closeConnectionForClient(currentMessage.iPEndPoint);

                IPEndPoint disconnectedIP = new IPEndPoint(currentMessage.iPEndPoint.Address, currentMessage.iPEndPoint.Port);
                m_networkMessageManager.recycleNetworkMessage(currentMessage);

                while (inputData.Count > 0)
                {
                    // discard all packages from this client 
                    currentMessage = inputData.Dequeue();
                    if (!disconnectedIP.Equals(currentMessage.iPEndPoint))
                    {
                        break;
                    }
                    m_networkMessageManager.recycleNetworkMessage(currentMessage);
                }

                return; // experimental 24.04.2019
            }

            currentContext = (NetMessageContextTCP)currentMessage.messageContextID;

            //Debug.Log("NetworkingManager:serverProcessIncomingTCPData: Context: \"" + currentContext + "\", Data: \"" + currentMessage.ToString());

            if (!m_gameID_clientEndPoint.ContainsValue(currentMessage.iPEndPoint)) // if client not registered 
            {
                if (currentContext != NetMessageContextTCP.Login) // if client not authorizing with this message
                {
                    server_sendDisconnectCommand(currentMessage.iPEndPoint, "session expired");
                    Debug.Log("NetworkingManager: Server: Unkown client is communicating !");
                    m_networkMessageManager.recycleNetworkMessage(currentMessage);
                    continue;
                }
            }

            switch (currentContext)
            {
                case NetMessageContextTCP.ChangeItemInventory:
                    {
                        if (checkMessage(currentMessage, 5, 0, 0))
                        {
                            EntityManager.singleton.server_switchPlayerInventoryItems(
                                                                                        m_clientEndPoint_gameID[currentMessage.iPEndPoint], currentMessage.getIntValue(4), (GUIRaycastIdentifier.Type)currentMessage.getIntValue(0),
                                                                                        currentMessage.getIntValue(1), (GUIRaycastIdentifier.Type)currentMessage.getIntValue(2), currentMessage.getIntValue(3)
                                                                                        );
                        }
                        break;
                    }
                case NetMessageContextTCP.EntityUsed:
                    {
                        if (checkMessage(currentMessage, 1, 0, 0))
                        {
                            EntityManager.singleton.server_onEntityUsed(m_clientEndPoint_gameID[currentMessage.iPEndPoint], currentMessage.getIntValue(0));
                        }
                        break;
                    }
                case NetMessageContextTCP.CloseContainer:
                    {
                        if (checkMessage(currentMessage, 1, 0, 0))
                        {
                            EntityManager.singleton.server_closeContainerForClient(m_clientEndPoint_gameID[currentMessage.iPEndPoint], currentMessage.getIntValue(0));
                        }
                        break;
                    }
                case NetMessageContextTCP.DropItem:
                    {
                        if (checkMessage(currentMessage, 3, 0, 0))
                        {
                            EntityManager.singleton.server_dropItemRequest(m_clientEndPoint_gameID[currentMessage.iPEndPoint], (GUIRaycastIdentifier.Type)currentMessage.getIntValue(0), currentMessage.getIntValue(1), currentMessage.getIntValue(2));
                        }
                        break;
                    }
                case NetMessageContextTCP.PickUpItem:
                    {
                        if (checkMessage(currentMessage, 1, 0, 0))
                        {
                            EntityManager.singleton.server_itemPickUpRequest(m_clientEndPoint_gameID[currentMessage.iPEndPoint], currentMessage.getIntValue(0));
                        }
                        break;
                    }
                case NetMessageContextTCP.Login:
                    {
                        if (checkMessage(currentMessage, 0, 0, 4))
                        {
                            if (m_gameID_clientEndPoint.ContainsValue(currentMessage.iPEndPoint))
                            {
                                Debug.LogWarning("NetworkingManager: Client send login-data multiple times: " + currentMessage.iPEndPoint.ToString());
                            }
                            else
                            {
                                if (currentMessage.getStringValue(0) != GameManager_Custom.singleton.gameVersion)
                                {
                                    server_closeConnectionForClient(currentMessage.iPEndPoint, "Your game version differs from the servers game version !");
                                }
                                else
                                {
                                    if (currentMessage.getStringValue(1) == "LOCALPLAYER")
                                    {
                                        server_closeConnectionForClient(currentMessage.iPEndPoint, "the username \"LOCALPLAYER\" is forbidden !");
                                    }
                                    else
                                    {
                                        int clientID = System.BitConverter.ToInt32(m_SHA256HashAlgorithm.ComputeHash(System.Text.Encoding.Unicode.GetBytes(currentMessage.getStringValue(1))), 0);
                                        string passwordHash = System.Text.Encoding.Unicode.GetString(m_SHA256HashAlgorithm.ComputeHash(System.Text.Encoding.Unicode.GetBytes(currentMessage.getStringValue(2))));

                                        if (m_clientID_registeredClients.ContainsKey(clientID))
                                        {
                                            if (m_clientID_registeredClients[clientID].passwordHash == passwordHash)
                                            {
                                                server_startConnectionForClient(
                                                                                    currentMessage.iPEndPoint,
                                                                                    clientID,
                                                                                    currentMessage.getStringValue(1),
                                                                                    passwordHash,
                                                                                    currentMessage.getStringValue(3)
                                                                                    );
                                                server_sendAllPrerequisitesData(currentMessage.iPEndPoint);
                                            }
                                            else
                                            {
                                                server_closeConnectionForClient(currentMessage.iPEndPoint, "wrong password !");
                                            }
                                        }
                                        else
                                        {
                                            server_startConnectionForClient(
                                                                                    currentMessage.iPEndPoint,
                                                                                    clientID,
                                                                                    currentMessage.getStringValue(1),
                                                                                    passwordHash,
                                                                                    currentMessage.getStringValue(3)
                                                                                    );
                                            server_sendAllPrerequisitesData(currentMessage.iPEndPoint);

                                            Debug.LogWarning("TODO:\"do you want to create a new account\"");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            server_closeConnectionForClient(currentMessage.iPEndPoint, "Wrong Login Format !");
                        }
                        break;
                    }
                case NetMessageContextTCP.PlayerState:
                    {
                        //Debug.Log("NetworkingManager: PlayerState: " + currentMessage.ToString());

                        if (checkMessage(currentMessage, 1, 0, 0))
                        {
                            if (currentMessage.getIntValue(0) == (int)MessageType.Request)
                            {
                                Vector3 playerPosition;

                                GameManager_Custom.PlayerState playerState = GameManager_Custom.singleton.getPlayerState(m_clientEndPoint_gameID[currentMessage.iPEndPoint], out playerPosition);

                                if (playerState == GameManager_Custom.PlayerState.Dead)
                                {
                                    server_sendPlayerDeadMessage(currentMessage.iPEndPoint);
                                }
                                else if (playerState == GameManager_Custom.PlayerState.Asleep)
                                {
                                    server_sendPlayerStateAsleep(currentMessage.iPEndPoint, playerPosition);
                                }
                                else
                                {
                                    Debug.LogWarning("NetworkingManager: Client requested playerState while beeing in state \"" + playerState + "\"");
                                }
                            }
                            else
                            {
                                Debug.LogWarning("NetworkingManager: unknown PlayerState context \"" + currentMessage.getIntValue(0) + "\"");
                            }
                        }
                        break;
                    }
                case NetMessageContextTCP.PlayerSpawn:
                    {
                        //Debug.Log("NetworkingManager: PlayerSpawn: " + currentMessage.ToString());

                        if (currentMessage.integerValuesCount > 0)
                        {
                            if (currentMessage.getIntValue(0) == (int)MessageType.Request)
                            {
                                if (currentMessage.integerValuesCount > 1)
                                {
                                    GameManager_Custom.singleton.server_onReceivedSpawnRequest(m_clientEndPoint_gameID[currentMessage.iPEndPoint], currentMessage.getIntValue(1));
                                }
                            }
                            else if (currentMessage.getIntValue(0) == (int)MessageType.Confirmation)
                            {
                                GameManager_Custom.PlayerState playerState = GameManager_Custom.singleton.getPlayerState(m_clientEndPoint_gameID[currentMessage.iPEndPoint]);

                                if (playerState == GameManager_Custom.PlayerState.Asleep)
                                {
                                    EntityManager.singleton.transformSleepingPlayerActive(m_clientEndPoint_gameID[currentMessage.iPEndPoint]);
                                }
                                else
                                {
                                    GameManager_Custom.singleton.server_onClientSpawnConfirmation(m_clientEndPoint_gameID[currentMessage.iPEndPoint]);
                                }
                            }
                            else if (currentMessage.getIntValue(0) == (int)MessageType.DEBUG_Position)
                            {
                                if (currentMessage.floatValuesCount > 2)
                                {
                                    if (m_clientEndPoint_gameID[currentMessage.iPEndPoint] == -1) // is local player
                                    {
                                        GameManager_Custom.singleton.server_onReceivedSpawnRequest(m_clientEndPoint_gameID[currentMessage.iPEndPoint], new Vector3(currentMessage.getFloatValue(0), currentMessage.getFloatValue(1), currentMessage.getFloatValue(2)));
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogWarning("Unknown PlayerSpawn-Message-Context: \"" + currentMessage.getIntValue(0) + "\"");
                            }
                        }
                        break;
                    }
                case NetMessageContextTCP.PlayerSuicide:
                    {
                        if (checkMessage(currentMessage, 0, 0, 0))
                        {
                            GameManager_Custom.singleton.server_PlayerSuicideRequest(m_clientEndPoint_gameID[currentMessage.iPEndPoint]);
                        }
                        break;
                    }
                case NetMessageContextTCP.EntityCustomMessage:
                    {
                        EntityManager.singleton.server_receivedCustomEntityMessage(currentMessage);
                        break;
                    }
                case NetMessageContextTCP.FPVCustomMessage:
                    {
                        FirstPersonViewManager.singleton.server_receivedCustomMessage(currentMessage, m_clientEndPoint_gameID[currentMessage.iPEndPoint]);
                        break;
                    }
                case NetMessageContextTCP.SplitInventoryItem:
                    {
                        if (checkMessage(currentMessage, 3, 0, 0))
                        {
                            EntityManager.singleton.server_SplitInventoryItems(m_clientEndPoint_gameID[currentMessage.iPEndPoint], currentMessage.getIntValue(2), (GUIRaycastIdentifier.Type)currentMessage.getIntValue(0),
                                                                                        currentMessage.getIntValue(1));
                        }
                        break;
                    }
                case NetMessageContextTCP.ViewPointPosition:
                    {
                        if (currentMessage.checkInputCorrectness(0, 3, 0))
                        {
                            PlayerManager.singleton.getWorldViewPoint(m_clientEndPoint_gameID[currentMessage.iPEndPoint]).transform.position = new Vector3(currentMessage.getFloatValue(0), currentMessage.getFloatValue(1), currentMessage.getFloatValue(2));
                        }
                        else
                        {
                            Debug.LogWarning("NetworkingManager: serverProcessIncomingTCPData: ViewPointPosition: wrong input dimensions: int: " + currentMessage.integerValuesCount + "; float: " + currentMessage.floatValuesCount + "; string: " + currentMessage.stringValuesCount);
                        }

                        break;
                    }
                case NetMessageContextTCP.ViewDistance:
                    {
                        if (currentMessage.checkInputCorrectness(0, 1, 0))
                        {
                            if (Time.realtimeSinceStartup < m_clientEndPoint_viewDistanceUpdateTime[currentMessage.iPEndPoint].m_float + m_setViewDistanceCooldown)
                            {
                                // too fast
                                server_sendViewDistanceTooQuick(m_clientEndPoint_viewDistanceUpdateTime[currentMessage.iPEndPoint].m_float + m_setViewDistanceCooldown - Time.realtimeSinceStartup, currentMessage.iPEndPoint);
                            }
                            else
                            {
                                m_clientEndPoint_viewDistanceUpdateTime[currentMessage.iPEndPoint].m_float = Time.realtimeSinceStartup;
                                m_clientID_registeredClients[m_gameID_clientID[m_clientEndPoint_gameID[currentMessage.iPEndPoint]]].viewDistance = currentMessage.getFloatValue(0);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("NetworkingManager: serverProcessIncomingTCPData: ViewDistance: wrong dimensions");
                        }
                        break;
                    }
                case NetMessageContextTCP.WakeUp:
                    {
                        //Debug.Log("NetworkingManager: serverProcessIncomingTCPData: WakeUp: " + currentMessage.ToString());

                        if (currentMessage.checkInputCorrectness(1, 0, 0))
                        {
                            int messageType = currentMessage.getIntValue(0);

                            if (messageType == (int)MessageType.Request)
                            {
                                Vector3 position;

                                GameManager_Custom.PlayerState state = GameManager_Custom.singleton.getPlayerState(m_clientEndPoint_gameID[currentMessage.iPEndPoint], out position);

                                if(state == GameManager_Custom.PlayerState.Asleep)
                                {
                                    server_sendPlayerSpawnCommand(m_clientEndPoint_gameID[currentMessage.iPEndPoint], position);
                                }
                                else if(state == GameManager_Custom.PlayerState.Dead)
                                {
                                    server_sendPlayerDeadMessage(currentMessage.iPEndPoint);
                                }
                                else
                                {
                                    server_closeConnectionForClient(currentMessage.iPEndPoint, "player state out of sync");
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning("NetworkingManager: serverProcessIncomingTCPData: WakeUp: wrong dimensions");
                        }

                        break;
                    }
                default:
                    {
                        Debug.LogWarning("NetworkingManager: Unknown TCP NetMessageContext: \"" + (int)currentContext + "\" (" + currentContext.ToString() + "): " + currentMessage.ToString());
                        break;
                    }
            }

            m_networkMessageManager.recycleNetworkMessage(currentMessage);
        }
    }

    private void clientProcessIncomingUDPData()
    {
        Queue<NetworkMessage> inputData = m_clientUDPInterface.colletDataBuffer();
        NetMessageContextUDP currentContext;
        NetworkMessage currentMessage;
        m_receivedMessagesCounter += inputData.Count;

        while (inputData.Count > 0)
        {
            currentMessage = inputData.Dequeue();
            m_receivedBytes += currentMessage.getByteLength();

            if (!currentMessage.iPEndPoint.Address.Equals(m_externServerEndPoint.Address)) // if not my servers
            {
                Debug.LogWarning("NetworkingManager: Client: Received package from non-server source ! (" + currentMessage.iPEndPoint.ToString() + ")");
                m_networkMessageManager.recycleNetworkMessage(currentMessage);
                continue;
            }

            currentContext = (NetMessageContextUDP)currentMessage.messageContextID;

            /*
            if (currentContext != NetMessageContextUDP.Ping)
            {
                Debug.Log("NetworkingManager:clientProcessIncomingUDPData: Context: \"" + currentContext + "\", Data: \"" + currentMessage.ToString());
            }
            */

            switch (currentContext)
            {
                case NetMessageContextUDP.Ping:
                    {
                        if (checkMessage(currentMessage, 2, 0, 0))
                        {
                            if (currentMessage.getIntValue(0) == (int)MessageType.Ping)
                            {
                                client_anwserPing(currentMessage.getIntValue(1));
                            }
                            else if (currentMessage.getIntValue(0) == (int)MessageType.Pong)
                            {
                                if (m_clientPingPackageCounter == currentMessage.getIntValue(1))
                                {
                                    m_clientLastTime_receivedLatencyPing = Time.realtimeSinceStartup;
                                    m_lastLatency = Time.realtimeSinceStartup - m_clientLastTimeSendPing;
                                    m_output_client_UDPLatency = m_lastLatency;
                                }
                            }
                        }
                        break;
                    }
                case NetMessageContextUDP.ProjectileStart:
                    {
                        if (checkMessage(currentMessage, 2, 6, 0))
                        {
                            ProjectileManager.singleton.client_addGunshot(
                                                                        new Vector3(currentMessage.getFloatValue(0), currentMessage.getFloatValue(1), currentMessage.getFloatValue(2)),
                                                                        new Vector3(currentMessage.getFloatValue(3), currentMessage.getFloatValue(4), currentMessage.getFloatValue(5)),
                                                                        currentMessage.getIntValue(0),
                                                                        (uint)currentMessage.getIntValue(1)
                                                                        );
                        }
                        break;
                    }
                case NetMessageContextUDP.ProjectileUpdate:
                    {
                        if (checkMessage(currentMessage, 1, 3, 0))
                        {
                            ProjectileManager.singleton.client_updateProjectilePos((uint)currentMessage.getIntValue(0), new Vector3(currentMessage.getFloatValue(0), currentMessage.getFloatValue(1), currentMessage.getFloatValue(2)));
                        }
                        break;
                    }
                case NetMessageContextUDP.HealthUpdate:
                    {
                        if (checkMessage(currentMessage, 1, 1, 0))
                        {
                            //Debug.Log("NetworkingManager: Client: received health update: " + inputData[i].m_data);

                            if (currentMessage.getIntValue(0) > m_healthPackageCounter)
                            {
                                m_healthPackageCounter = currentMessage.getIntValue(0);
                                client_sendHealthUpdateConfirmation(m_healthPackageCounter);
                                GameManager_Custom.singleton.client_updatePlayerHealth(currentMessage.getFloatValue(0));
                            }
                        }
                        break;
                    }
                case NetMessageContextUDP.HitConfirmation:
                    {
                        if (checkMessage(currentMessage, 0, 0, 0))
                        {
                            GUIManager.singleton.showHitmarker();
                            SoundManager.singleton.playGlobalSound(14, Sound.SoundPlaystyle.Once);
                        }
                        break;
                    }
                case NetMessageContextUDP.WorldSound:
                    {
                        if (currentMessage.floatValuesCount == 3 && currentMessage.integerValuesCount > 0)
                        {
                            if (currentMessage.integerValuesCount == 1)
                            {
                                SoundManager.singleton.playSoundAt(currentMessage.getIntValue(0), new Vector3(currentMessage.getFloatValue(0), currentMessage.getFloatValue(1), currentMessage.getFloatValue(2)), Sound.SoundPlaystyle.Once);
                            }
                            else
                            {
                                Sound sound = SoundManager.singleton.playSoundAt(currentMessage.getIntValue(0), new Vector3(currentMessage.getFloatValue(0), currentMessage.getFloatValue(1), currentMessage.getFloatValue(2)), Sound.SoundPlaystyle.Once);
                                Entity_base parent = EntityManager.singleton.getEntity(currentMessage.getIntValue(1));

                                if (parent != null)
                                {
                                    sound.transform.parent = parent.transform;
                                }
                            }
                        }
                        break;
                    }
                case NetMessageContextUDP.EntityCustomMessage:
                    {
                        EntityManager.singleton.client_receivedCustomEntityMessage(currentMessage);
                        break;
                    }
                case NetMessageContextUDP.FPVCustomMessage:
                    {
                        FirstPersonViewManager.singleton.client_receivedCustomMessage(currentMessage);
                        break;
                    }
                default:
                    {
                        Debug.LogError("Unknown UDP NetMessageContext: \"" + (int)currentContext + "\"");
                        break;
                    }
            }

            m_networkMessageManager.recycleNetworkMessage(currentMessage);
        }
    }

    private void clientProcessIncomingTCPData()
    {
        Queue<NetworkMessage> inputData;
        if(m_TCPClient == null) // can happen if tcp disconnected unexpectedly 
        {
            inputData = new Queue<NetworkMessage>();

            NetworkMessage endMessage = getNewNetworkMessage();
            endMessage.messageType = NetworkMessage.MessageType.TCP_End;
            inputData.Enqueue(endMessage);
        }
        else
        {
            inputData = m_TCPClient.colletDataBuffer();
        }

        NetMessageContextTCP currentContext;
        NetworkMessage currentMessage;
        m_receivedMessagesCounter += inputData.Count;

        while (inputData.Count > 0)
        {
            currentMessage = inputData.Dequeue();
            m_receivedBytes += currentMessage.getByteLength();

#if SHOW_INPUT_MESSAGES
            Debug.Log("NetworkManager: Client Input TCP: " + currentMessage.ToString());
#endif

            if (currentMessage.messageType == NetworkMessage.MessageType.TCP_End) // end request
            {
                Debug.Log("NetworkingManager: Client: TCP-closing Request from server");
                GUIManager.singleton.displayMessage("NetworkingManager: Client: TCP-closing Request from server");

                GameManager_Custom.singleton.client_disconnected("Disconnected by Server: TCP-Connection closed");

                m_networkMessageManager.recycleNetworkMessage(currentMessage);

                while (inputData.Count > 0)
                {
                    m_networkMessageManager.recycleNetworkMessage(inputData.Dequeue());
                }
                return;
            }

            currentContext = (NetMessageContextTCP)currentMessage.messageContextID;

            //Debug.Log("NetworkingManager:clientProcessIncomingTCPData: Context: \"" + currentContext + "\", Data: \"" + currentMessage.ToString());

            switch (currentContext)
            {
                case NetMessageContextTCP.OpenContainer:
                    {
                        if (checkMessage(currentMessage, 1, 0, 0))
                        {
                            EntityManager.singleton.client_receiveOpenContainerCommand(currentMessage.getIntValue(0));
                        }
                        break;
                    }
                case NetMessageContextTCP.CloseContainer:
                    {
                        if (checkMessage(currentMessage, 1, 0, 0))
                        {
                            EntityManager.singleton.client_receiveCloseContainerCommand(currentMessage.getIntValue(0));
                        }
                        break;
                    }
                case NetMessageContextTCP.ChangeItemInventory:
                    {
                        if (checkMessage(currentMessage, 5, 0, 0))
                        {
                            StorableItem item = ItemManager.singleton.createNewStorableItem(currentMessage.getIntValue(3), currentMessage.getIntValue(4));
                            EntityManager.singleton.client_receiveItemUpdate((GUIRaycastIdentifier.Type)currentMessage.getIntValue(0), currentMessage.getIntValue(1), currentMessage.getIntValue(2), item);
                        }
                        break;
                    }
                case NetMessageContextTCP.Login:
                    {
                        if (currentMessage.integerValuesCount > 0)
                        {
                            // received login-data command from server => sending login data
                            if ((MessageType)currentMessage.getIntValue(0) == MessageType.Command)
                            {
                                client_sendLoginData();
                            }
                            else
                            {
                                Debug.LogWarning("unknown MessageType received");
                            }
                        }
                        break;
                    }
                case NetMessageContextTCP.WorldParameters:
                    {
                        if (checkMessage(currentMessage, 0, 2, 0))
                        {
                            GameManager_Custom.singleton.client_onReceivedMapParameters(currentMessage.getFloatValue(0), (int)currentMessage.getFloatValue(1));
                        }
                        break;
                    }
                case NetMessageContextTCP.PlayerState:
                    {
                        //Debug.Log("NetworkingManager: client: PlayerState: " + currentMessage.ToString());

                        if (currentMessage.integerValuesCount > 0)
                        {
                            int playerState = currentMessage.getIntValue(0);

                            if (playerState == (int)GameManager_Custom.PlayerState.Dead)
                            {
                                //Debug.Log("NetworkingManager: received player-death message");
                                GameManager_Custom.singleton.client_onPlayerDead();
                            }
                            else if (playerState == (int)GameManager_Custom.PlayerState.Asleep)
                            {
                                if (checkMessage(currentMessage, 1, 3, 0))
                                {
                                    Vector3 playerPosition = new Vector3(currentMessage.getFloatValue(0), currentMessage.getFloatValue(1), currentMessage.getFloatValue(2));

                                    GameManager_Custom.singleton.client_onPlayerAsleep(playerPosition);
                                }
                            }
                            else
                            {
                                Debug.LogWarning("NetworkingManager: clientProcessIncomingTCPData: unknown PlayerState \"" + playerState + "\"");
                            }

                        }
                        break;
                    }
                case NetMessageContextTCP.PlayerSpawn:
                    {
                        if (checkMessage(currentMessage, 0, 3, 0))
                        {
                            GameManager_Custom.singleton.client_onReceivedSpawnCommand(new Vector3(currentMessage.getFloatValue(0), currentMessage.getFloatValue(1), currentMessage.getFloatValue(2)));
                        }
                        break;
                    }
                case NetMessageContextTCP.EstablishConnection:
                    {
                        if (checkMessage(currentMessage, 2, 0, 0))
                        {
                            if (currentMessage.getIntValue(0) == (int)MessageType.Success)
                            {
                                //m_client_myGameID = currentMessage.getIntValue(1);
                                client_sendViewDistance(ClientSettingsManager.singleton.viewDistance);
                            }
                            else
                            {
                                Debug.LogError("NetworkingManager: Client: received unknown EstablishConnection response: \"" + currentMessage.getIntValue(1) + "\"");
                            }
                        }
                        break;
                    }
                case NetMessageContextTCP.Disconnect:
                    {
                        if (checkMessage(currentMessage, 0, 0, 1))
                        {
                            GUIManager.singleton.displayMessage("Disconnected by Server: " + currentMessage.getStringValue(0));
                            Debug.Log("Disconnected by Server: " + currentMessage.getStringValue(0));
                            GameManager_Custom.singleton.client_disconnected("Disconnected by Server (TCP): " + currentMessage.getStringValue(0));
                        }
                        break;
                    }
                case NetMessageContextTCP.EntityCustomMessage:
                    {
                        EntityManager.singleton.client_receivedCustomEntityMessage(currentMessage);
                        break;
                    }
                case NetMessageContextTCP.EntityUncull:
                    {
                        EntityManager.singleton.client_entityCreateCommand(currentMessage);
                        break;
                    }
                case NetMessageContextTCP.EntityDistantUncull:
                    {
                        EntityManager.singleton.client_distantEntityCreateCommand(currentMessage);
                        break;
                    }
                case NetMessageContextTCP.EntityCull:
                    {
                        EntityManager.singleton.client_entityRemoveCommand(currentMessage);
                        break;
                    }
                case NetMessageContextTCP.EntityDistantCull:
                    {
                        EntityManager.singleton.client_distantEntityRemoveCommand(currentMessage);
                        break;
                    }
                case NetMessageContextTCP.PlayerEntityUID:
                    {
                        if (checkMessage(currentMessage, 1, 0, 0))
                        {
                            Player_local player = EntityManager.singleton.getLocalPlayer();

                            if (player == null)
                            {
                                Debug.LogWarning("NetworkingManager: clientProcessIncomingTCPData: PlayerEntityUID: player local is null");
                            }
                            else
                            {
                                EntityManager.singleton.client_setPlayerLocalForID(currentMessage.getIntValue(0));
                                player.setEntityUID(currentMessage.getIntValue(0));
                            }
                        }
                        else
                        {
                            Debug.LogWarning("NetworkingManager: clientProcessIncomingTCPData: PlayerEntityUID: message has wrong values count");
                        }
                        break;
                    }
                case NetMessageContextTCP.FPVCustomMessage:
                    {
                        FirstPersonViewManager.singleton.client_receivedCustomMessage(currentMessage);
                        break;
                    }
                case NetMessageContextTCP.GlobalSound:
                    {
                        if (currentMessage.checkInputCorrectness(1, 0, 0))
                        {
                            SoundManager.singleton.playGlobalSound(currentMessage.getIntValue(0), Sound.SoundPlaystyle.Once);
                        }
                        break;
                    }
                case NetMessageContextTCP.ViewDistance:
                    {
                        if (currentMessage.checkInputCorrectness(0, 1, 0))
                        {
                            m_clientSendViewDistanceAgain = true;
                            m_clientSendViewDistanceAgainTime = Time.realtimeSinceStartup + currentMessage.getFloatValue(0);
                        }
                        else
                        {
                            Debug.LogWarning("NetworkingManager: clientProcessIncomingTCPData: ViewDistance: message has wrong values count");
                        }
                        break;
                    }
                case NetMessageContextTCP.SpawnpointUpdate:
                    {
                        if (currentMessage.checkInputCorrectness(2, 0, 0))
                        {
                            if (currentMessage.getIntValue(0) == (int)MessageType.Add)
                            {
                                m_client_clientUserData.addPlayerSpawnpointEntityID(currentMessage.getIntValue(1));
                            }
                            else if (currentMessage.getIntValue(0) == (int)MessageType.Remove)
                            {
                                m_client_clientUserData.removePlayerSpawnpointEntityID(currentMessage.getIntValue(1));
                            }
                            else
                            {
                                Debug.LogWarning("NetworkingManager: clientProcessIncomingTCPData: SpawnpointUpdate: message has unknwon MessageType: " + currentMessage.getIntValue(0));
                            }
                        }
                        else
                        {
                            Debug.LogWarning("NetworkingManager: clientProcessIncomingTCPData: SpawnpointUpdate: message has wrong values count");
                        }
                        break;
                    }
                case NetMessageContextTCP.SpawnpointCooldown:
                    {
                        if (currentMessage.checkInputCorrectness(1, 1, 0))
                        {
                            m_client_clientUserData.client_updateSpawnpointCooldown(currentMessage.getIntValue(0), currentMessage.getFloatValue(0));
                        }
                        else
                        {
                            Debug.LogWarning("NetworkingManager: clientProcessIncomingTCPData: SpawnpointCooldown: message has wrong values count");
                        }
                        break;
                    }
                case NetMessageContextTCP.ParticleEffect:
                    {
                        if (currentMessage.checkInputCorrectness(1, 6, 0))
                        {
                            ParticleCachedManager.singleton.playParticleEffect(currentMessage.getIntValue(0), new Vector3(currentMessage.getFloatValue(0), currentMessage.getFloatValue(1), currentMessage.getFloatValue(2)),
                                                                                Quaternion.Euler(currentMessage.getFloatValue(3), currentMessage.getFloatValue(4), currentMessage.getFloatValue(5)));
                        }
                        else
                        {
                            Debug.LogWarning("NetworkingManager: clientProcessIncomingTCPData: ParticleEffect: message has wrong values count");
                        }
                        break;
                    }
                case NetMessageContextTCP.WorldTime:
                    {
                        if (currentMessage.checkInputCorrectness(4, 0, 0))
                        {
                            if(GameManager_Custom.singleton.isClient)
                            {
                                EnvironmentManager.singleton.setTime(currentMessage.getIntValue(0), currentMessage.getIntValue(1), currentMessage.getIntValue(2), currentMessage.getIntValue(3));
                            }
                        }
                        else
                        {
                            Debug.LogWarning("NetworkingManager: clientProcessIncomingTCPData: WorldTime: message has wrong values count");
                        }
                        break;
                    }
                default:
                    {
                        Debug.LogError("Unknown TCP NetMessageContext: \"" + (int)currentContext + "\"");
                        break;
                    }
            }

            m_networkMessageManager.recycleNetworkMessage(currentMessage);
        }
    }

    private bool checkMessage(NetworkMessage message, int integerCount, int floatCount, int stringCount)
    {
        if (message.checkInputCorrectness(integerCount, floatCount, stringCount))
        {
            return true;
        }
        else
        {
            Debug.LogWarning("NetworkingManager: checkMessage: message format incorrect: " + message.ToString());
            return false;
        }
    }

    #endregion

    #region Client send messages

    /// <summary>
    /// sends a message to the server via TCP
    /// </summary>
    /// <param name="message">the message to transmit</param>
    public void client_sendCustomMessageTCP(NetworkMessage message)
    {
        m_TCPClient.addDataToSend(message);
    }

    /// <summary>
    /// sends a message to the server via UDP
    /// </summary>
    /// <param name="message">the message to transmit</param>
    public void client_sendCustomMessageUDP(NetworkMessage message)
    {
        m_clientUDPInterface.addDataToSend(message, m_externServerEndPoint);
    }

    public void client_sendPlayerViewpointPosition(Vector3 position)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.ViewPointPosition;
        tempMessage.addFloatValues(position.x, position.y, position.z);
        m_TCPClient.addDataToSend(tempMessage);
    }

    public void client_sendSpawnConfirmation()
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.PlayerSpawn;
        tempMessage.addIntegerValues((int)MessageType.Confirmation);

        //Debug.Log("NetworkingManager: client_sendSpawnConfirmation: " + tempMessage.ToString());

        m_TCPClient.addDataToSend(tempMessage);
    }

    public void client_sendStartProjectile(Vector3 startPosition, Vector3 velocity, int weaponPrefabIndex)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextUDP.ProjectileStart;
        tempMessage.addFloatValues(startPosition.x, startPosition.y, startPosition.z, velocity.x, velocity.y, velocity.z);
        tempMessage.addIntegerValues(weaponPrefabIndex);

        m_clientUDPInterface.addDataToSend(tempMessage, m_externServerEndPoint);
    }

    private void client_anwserPing(int packageID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextUDP.Ping;
        tempMessage.addIntegerValues((int)MessageType.Pong, packageID);

        m_clientUDPInterface.addDataToSend(tempMessage, m_externServerEndPoint);
    }

    private void client_sendHealthUpdateConfirmation(int packageID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextUDP.HealthUpdate;
        tempMessage.addIntegerValues(packageID);

        m_clientUDPInterface.addDataToSend(tempMessage, m_externServerEndPoint);
    }

    private void client_sendPing(int packageID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextUDP.Ping;
        tempMessage.addIntegerValues((int)MessageType.Ping, packageID);

        m_clientUDPInterface.addDataToSend(tempMessage, m_externServerEndPoint);
    }

    public void client_sendItemSwitchRequest(GUIRaycastIdentifier.Type sourceType, int sourceIndex, GUIRaycastIdentifier.Type targetType, int targetIndex, int containerUID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.ChangeItemInventory;
        tempMessage.addIntegerValues((int)sourceType, sourceIndex, (int)targetType, targetIndex, containerUID);

        m_TCPClient.addDataToSend(tempMessage);
    }

    public void client_sendItemSplitRequest(GUIRaycastIdentifier.Type sourceType, int sourceIndex, int containerUID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.SplitInventoryItem;
        tempMessage.addIntegerValues((int)sourceType, sourceIndex, containerUID);

        m_TCPClient.addDataToSend(tempMessage);
    }

    public void client_sendEntityUsed(int entityUID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.EntityUsed;
        tempMessage.addIntegerValues(entityUID);

        m_TCPClient.addDataToSend(tempMessage);
    }

    public void client_sendContainerCloseRequest(int entityUID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.CloseContainer;
        tempMessage.addIntegerValues(entityUID);

        m_TCPClient.addDataToSend(tempMessage);
    }

    public void client_sendEntityDropRequest(GUIRaycastIdentifier.Type type, int itemPos, int containerUID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.DropItem;
        tempMessage.addIntegerValues((int)type, itemPos, containerUID);

        m_TCPClient.addDataToSend(tempMessage);
    }

    public void client_sendItemPickupRequest(int entityUID)
    {
        Debug.Log("client_sendItemPickupRequest: " + entityUID);

        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.PickUpItem;
        tempMessage.addIntegerValues(entityUID);

        m_TCPClient.addDataToSend(tempMessage);
    }

    private void client_sendLoginData()
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.Login;
        tempMessage.addStringValues(GameManager_Custom.singleton.gameVersion, m_clientUsername, m_clientUserPasswordHash, m_clientUserIngameName);

        m_TCPClient.addDataToSend(tempMessage);
    }

    public void client_sendPlayerStateRequest()
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.PlayerState;
        tempMessage.addIntegerValues((int)MessageType.Request);

        m_TCPClient.addDataToSend(tempMessage);
    }

    public void client_sendPlayerSpawnRequest(int spawnPointID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.PlayerSpawn;
        tempMessage.addIntegerValues((int)MessageType.Request, spawnPointID);

        m_TCPClient.addDataToSend(tempMessage);
    }

    public void client_sendPlayerSpawnRequestDebugPosition(Vector3 position)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.PlayerSpawn;
        tempMessage.addIntegerValues((int)MessageType.DEBUG_Position);
        tempMessage.addFloatValues(position.x, position.y, position.z);

        m_TCPClient.addDataToSend(tempMessage);
    }

    public void client_sendPlayerWakeUpRequest()
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.WakeUp;
        tempMessage.addIntegerValues((int)MessageType.Request);

        m_TCPClient.addDataToSend(tempMessage);
    }

    public void client_sendPlayerSuicideRequest()
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.PlayerSuicide;

        m_TCPClient.addDataToSend(tempMessage);
    }

    public void client_sendViewDistance(float viewDistance)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.ViewDistance;
        tempMessage.addFloatValues(viewDistance);

        m_TCPClient.addDataToSend(tempMessage);
    }

    #endregion

    #region Server send messages

    /// <summary>
    /// sends a message to one client via TCP
    /// </summary>
    /// <param name="message">the message to transmit</param>
    /// <param name="gameID">the gameID of receiver of the message</param>
    public void server_sendCustomMessageTCP(NetworkMessage message, int gameID)
    {
        if (m_TCPServerInterface == null)
        {
            Debug.LogWarning("NetworkingManager: server_sendCustomMessageTCP: TCPServerInterface is null !");
        }
        else
        {
            if (m_gameID_clientEndPoint.ContainsKey(gameID))
            {
                m_TCPServerInterface.addDataToSend_OneClient(message, m_gameID_clientEndPoint[gameID]);
            }
            else
            {
                //Debug.LogWarning("NetworkingManager: server_sendCustomMessageTCP: Client not found: "+ gameID);
            }
        }
    }

    /// <summary>
    /// sends a message to all client via TCP
    /// </summary>
    /// <param name="message">the message to transmit</param>
    public void server_sendCustomMessageTCP_ToAllClients(NetworkMessage message)
    {
        m_TCPServerInterface.addDataToSend_AllClients(message);
    }

    /// <summary>
    /// sends a message to one client via UDP
    /// </summary>
    /// <param name="message">the message to transmit</param>
    /// <param name="gameID">the gameID of receiver of the message</param>
    public void server_sendCustomMessageUDP(NetworkMessage message, int gameID)
    {
        m_serverUDPInterface.addDataToSend(message, m_gameID_clientEndPoint[gameID]);
    }

    public void server_sendClientSpawnpointAdd(int entityUID, int clientID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.SpawnpointUpdate;
        tempMessage.addIntegerValues((int)MessageType.Add);
        tempMessage.addIntegerValues(entityUID);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, m_clientID_clientEndPoint[clientID]);
    }

    public void server_sendClientSpawnpointRemove(int entityUID, int clientID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.SpawnpointUpdate;
        tempMessage.addIntegerValues((int)MessageType.Remove);
        tempMessage.addIntegerValues(entityUID);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, m_clientID_clientEndPoint[clientID]);
    }

    private void server_sendViewDistanceTooQuick(float waitTime, IPEndPoint receiver)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.ViewDistance;
        tempMessage.addFloatValues(waitTime);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, receiver);
    }

    public void server_sendSpawnpointCooldown(int playerGameID, int spawnPointEntityID, float remainingCooldown)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.SpawnpointCooldown;
        tempMessage.addIntegerValues(spawnPointEntityID);
        tempMessage.addFloatValues(remainingCooldown);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, m_clientID_clientEndPoint[m_gameID_clientID[playerGameID]]);
    }

    private void server_sendConnectionSuccess(IPEndPoint receiver, int gameID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.EstablishConnection;
        tempMessage.addIntegerValues((int)MessageType.Success, gameID);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, receiver);
    }

    private void server_sendSeedSize(IPEndPoint receiver)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.WorldParameters;
        tempMessage.addFloatValues(WorldManager.singleton.seed, WorldManager.singleton.size);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, receiver);
    }

    private void server_sendAllPrerequisitesData(IPEndPoint receiver)
    {
        server_sendSeedSize(receiver);
        server_sendEnvironmentTime(receiver);
        server_sendAllSpawnpoints(receiver);
    }

    private void server_sendAllSpawnpoints(IPEndPoint receiver)
    {
        int clientID = m_gameID_clientID[m_clientEndPoint_gameID[receiver]];

        ClientUserData userData = m_clientID_registeredClients[clientID];

        List<int> spawnpoints = userData.playerSpawnpointsEntityIDs;

        for(int i = 0; i < spawnpoints.Count; i++)
        {
            server_sendClientSpawnpointAdd(spawnpoints[i], clientID);
        }
    }

    private void server_sendDisconnectCommand(IPEndPoint receiver, string message)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.Disconnect;
        tempMessage.addStringValues(message);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, receiver);
    }

    public void server_sendPlayerSpawnCommand(int gameID, Vector3 position)
    {
        //Debug.Log("NetworkingManager: server_sendPlayerSpawnCommand: gameID: " + gameID);

        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.PlayerSpawn;
        tempMessage.addFloatValues(position.x, position.y, position.z);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, m_gameID_clientEndPoint[gameID]);
    }

    private void server_sendUDPLatencyPing(IPEndPoint receiver, int packageID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextUDP.Ping;
        tempMessage.addIntegerValues((int)MessageType.Ping, packageID);

        m_serverUDPInterface.addDataToSend(tempMessage, receiver);
    }

    public void server_sendNewProjectile(int gameIDReceiver, int weaponIndex, int projectileID, Vector3 startPosition, Vector3 direction)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextUDP.ProjectileStart;
        tempMessage.addIntegerValues(weaponIndex, projectileID);
        tempMessage.addFloatValues(startPosition.x, startPosition.y, startPosition.z, direction.x, direction.y, direction.z);

        m_serverUDPInterface.addDataToSend(tempMessage, m_gameID_clientEndPoint[gameIDReceiver]);
    }

    public void server_sendProjectileUpdate(int gameIDReceiver, int projectileID, Vector3 position)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextUDP.ProjectileUpdate;
        tempMessage.addIntegerValues(projectileID);
        tempMessage.addFloatValues(position.x, position.y, position.z);

        m_serverUDPInterface.addDataToSend(tempMessage, m_gameID_clientEndPoint[gameIDReceiver]);
    }

    public void server_sendHealthUpdate(IPEndPoint receiver)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextUDP.HealthUpdate;
        tempMessage.addIntegerValues((int)m_clientEndPoint_HealthUpdatePackageID[receiver].m_uint);
        tempMessage.addFloatValues(m_clientEndPoint_PlayerHealth[receiver].m_float);

        m_serverUDPInterface.addDataToSend(tempMessage, receiver);
        //Debug.Log("NetworkingManager: Server: sent health-update: \"" + dataToSend + "\" to " + receiver.ToString());
    }

    public void server_sendPlayerDeadMessage(int gameID)
    {
        server_sendPlayerDeadMessage(m_gameID_clientEndPoint[gameID]);
    }
    private void server_sendPlayerDeadMessage(IPEndPoint client)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.PlayerState;
        tempMessage.addIntegerValues((int)GameManager_Custom.PlayerState.Dead);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, client);
    }

    public void server_sendPong(IPEndPoint client, int packageID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextUDP.Ping;
        tempMessage.addIntegerValues((int)MessageType.Pong, packageID);

        m_serverUDPInterface.addDataToSend(tempMessage, client);
    }

    public void server_sendHitConfirmation(int gameID)
    {
        if (m_gameID_clientEndPoint.ContainsKey(gameID)) // if shot came from player not NPC
        {
            NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
            tempMessage.messageContextID = (int)NetMessageContextUDP.HitConfirmation;

            m_serverUDPInterface.addDataToSend(tempMessage, m_gameID_clientEndPoint[gameID]);
        }
    }

    public void server_sendWorldSound(int gameID, int soundIndex, Vector3 position)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextUDP.WorldSound;
        tempMessage.addIntegerValues(soundIndex);
        tempMessage.addFloatValues(position.x, position.y, position.z);

        m_serverUDPInterface.addDataToSend(tempMessage, m_gameID_clientEndPoint[gameID]);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="soundIndex">which sound to play</param>
    /// <param name="position">position to play the sound</param>
    /// <param name="parentEntityID">optional: entity to attach the sound to</param>
    public void server_sendWorldSoundToAllInRange(int soundIndex, Vector3 position, int? parentEntityID = null)
    {
        Dictionary<int, Player_external> players = EntityManager.singleton.getPlayerID_ExternalScriptsDict();

        List<int> playersInRange = new List<int>();

        foreach (KeyValuePair<int, Player_external> pair in players)
        {
            if (Vector3.Distance(pair.Value.transform.position, position) < SoundManager.singleton.getMaxHearableDistance(soundIndex))
            {
                playersInRange.Add(pair.Key);
            }
        }

        if (EntityManager.singleton.getLocalPlayer() != null && Vector3.Distance(EntityManager.singleton.getLocalPlayer().transform.position, position) < SoundManager.singleton.getMaxHearableDistance(soundIndex))
        {
            playersInRange.Add(-1);
        }

        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextUDP.WorldSound;
        tempMessage.addIntegerValues(soundIndex);
        tempMessage.addFloatValues(position.x, position.y, position.z);

        if(parentEntityID != null)
        {
            tempMessage.addIntegerValues((int)parentEntityID);
        }

        server_sendUDPMessageMultipleClients(playersInRange, tempMessage);
    }

    private void server_sendUDPMessageMultipleClients(List<int> clientsIDs, NetworkMessage message)
    {
        if (clientsIDs.Count > 1)
        {
            byte[] temp_bytes;
            int temp_int;

            if (message.getOutput(out temp_bytes, out temp_int, true, false))
            {
                for (int i = 0; i < clientsIDs.Count; i++)
                {
                    NetworkMessage temp_message = m_networkMessageManager.getNetworkMessage();
                    temp_message.copyOutputDataFrom(message);
                    m_serverUDPInterface.addDataToSend(temp_message, m_gameID_clientEndPoint[clientsIDs[i]]);
                }
            }
            else
            {
                Debug.LogWarning("NetworkingManager: server_sendUDPMessageMultipleClients: encoding message failed: " + message.getOutputMessageBitView());
            }

            m_networkMessageManager.recycleNetworkMessage(message);
        }
        else if (clientsIDs.Count > 0)
        {
            m_serverUDPInterface.addDataToSend(message, m_gameID_clientEndPoint[clientsIDs[0]]);
        }
    }

    public void server_sendGlobalSoundToAllTCP(int soundIndex)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.GlobalSound;
        tempMessage.addIntegerValues(soundIndex);

        m_TCPServerInterface.addDataToSend_AllClients(tempMessage);
    }

    private void server_sendTCPMessageMultipleClients(List<int> clientsIDs, NetworkMessage message)
    {
        if (clientsIDs.Count > 1)
        {
            byte[] temp_bytes;
            int temp_int;

            if (message.getOutput(out temp_bytes, out temp_int, true, false))
            {
                for (int i = 0; i < clientsIDs.Count; i++)
                {
                    NetworkMessage temp_message = m_networkMessageManager.getNetworkMessage();
                    temp_message.copyOutputDataFrom(message);
                    m_TCPServerInterface.addDataToSend_OneClient(temp_message, m_gameID_clientEndPoint[clientsIDs[i]]);
                }
            }
            else
            {
                Debug.LogWarning("NetworkingManager: server_sendTCPMessageMultipleClients: encoding message failed: " + message.getOutputMessageBitView());
            }

            m_networkMessageManager.recycleNetworkMessage(message);
        }
        else if (clientsIDs.Count > 0)
        {
            m_TCPServerInterface.addDataToSend_OneClient(message, m_gameID_clientEndPoint[clientsIDs[0]]);
        }
    }

    public void server_sendInventoryItemUpdate(int gameID, GUIRaycastIdentifier.Type type, int itemPos, int containerUID, StorableItem item)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.ChangeItemInventory;

        if (item == null)
        {
            tempMessage.addIntegerValues((int)type, containerUID, itemPos, -1, -1);
        }
        else
        {
            tempMessage.addIntegerValues((int)type, containerUID, itemPos, item.itemTemplateIndex, item.m_stackSize);
        }
        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, m_gameID_clientEndPoint[gameID]);
    }

    public void server_sendContainerOpenCommand(int gameID, int containerUID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.OpenContainer;
        tempMessage.addIntegerValues(containerUID);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, m_gameID_clientEndPoint[gameID]);
    }

    public void server_sendContainerCloseCommand(int gameID, int containerUID)
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.CloseContainer;
        tempMessage.addIntegerValues(containerUID);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, m_gameID_clientEndPoint[gameID]);
    }

    private void server_sendLoginCommand(IPEndPoint client)
    {
#if CONNECTION_DEBUG
        Debug.Log("server_sendLoginCommand: " + client.Address.ToString());
#endif

        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.Login;
        tempMessage.addIntegerValues((int)MessageType.Command);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, client);
    }

    private void server_sendPlayerStateAsleep(IPEndPoint client, Vector3 playerPosition)
    {
        //Debug.Log("NetworkingManager: server_sendPlayerStateAsleep");

        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.PlayerState;
        tempMessage.addIntegerValues((int)GameManager_Custom.PlayerState.Asleep);
        tempMessage.addFloatValues(playerPosition.x, playerPosition.y, playerPosition.z);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, client);
    }

    public void server_sendPlayerEntityID(int gameID, int entityID) // sets the local player entity ID for a client
    {
        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.PlayerEntityUID;
        tempMessage.addIntegerValues(entityID);

        m_TCPServerInterface.addDataToSend_OneClient(tempMessage, m_gameID_clientEndPoint[gameID]);
    }

    public void server_sendParticleEffectToAllInRange(int effectID, Vector3 position, Quaternion rotation)
    {
        Dictionary<int, Player_external> players = EntityManager.singleton.getPlayerID_ExternalScriptsDict();

        List<int> playersInRange = new List<int>();

        foreach (KeyValuePair<int, Player_external> pair in players)
        {
            if (Vector3.Distance(pair.Value.transform.position, position) < ParticleCachedManager.singleton.client_maxViewDistance)
            {
                playersInRange.Add(pair.Key);
            }
        }

        if (EntityManager.singleton.getLocalPlayer() != null && Vector3.Distance(EntityManager.singleton.getLocalPlayer().transform.position, position) < ParticleCachedManager.singleton.client_maxViewDistance)
        {
            playersInRange.Add(-1);
        }

        NetworkMessage tempMessage = m_networkMessageManager.getNetworkMessage();
        tempMessage.messageContextID = (int)NetMessageContextTCP.ParticleEffect;
        tempMessage.addIntegerValues(effectID);
        tempMessage.addFloatValues(position.x, position.y, position.z);
        tempMessage.addFloatValues(rotation.eulerAngles.x, rotation.eulerAngles.y, rotation.eulerAngles.z);

        server_sendTCPMessageMultipleClients(playersInRange, tempMessage);
    }

    public void server_sendEnvironmentTime(int day, int hour, int minute, int secound)
    {
        NetworkMessage message = getNewNetworkMessage();
        message.messageContextID = (int)NetMessageContextTCP.WorldTime;
        message.addIntegerValues(day, hour, minute, secound);

        m_TCPServerInterface.addDataToSend_AllClients(message);
    }
    public void server_sendEnvironmentTime(IPEndPoint receiver)
    {
        NetworkMessage message = getNewNetworkMessage();
        message.messageContextID = (int)NetMessageContextTCP.WorldTime;

        DateTime dateTime = EnvironmentManager.singleton.currentDateTime;

        message.addIntegerValues(dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);

        m_TCPServerInterface.addDataToSend_OneClient(message, receiver);
    }

    #endregion

    #region General Methodes

    public NetworkMessage getNewNetworkMessage()
    {
        return m_networkMessageManager.getNetworkMessage();
    }

    public void recyleNetworkMessage(NetworkMessage message)
    {
        m_networkMessageManager.recycleNetworkMessage(message);
    }

    public void shutdownNetworkSockets()
    {
        if (m_clientUDPInterface != null)
        {
            m_clientUDPInterface.dispose();
            m_clientUDPInterface = null;
        }
        if (m_serverUDPInterface != null)
        {
            m_serverUDPInterface.dispose();
            m_serverUDPInterface = null;
        }
        if (m_TCPServerInterface != null)
        {
            m_TCPServerInterface.dispose();
            m_TCPServerInterface = null;
        }
        if (m_TCPClient != null)
        {
            m_TCPClient.dispose();
            m_TCPClient = null;
        }
    }

    #endregion

    #region Server Methodes

    public ClientUserData server_getClientUserData(int playerGameID)
    {
        if (m_gameID_clientID.ContainsKey(playerGameID))
        {
            if (m_clientID_registeredClients.ContainsKey(m_gameID_clientID[playerGameID]))
            {
                return m_clientID_registeredClients[m_gameID_clientID[playerGameID]];
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    public ClientUserData client_getLocalClientUserData()
    {
        return m_client_clientUserData;
    }

    public float server_getClientViewDistance(int gameID)
    {
        if (m_gameID_clientID.ContainsKey(gameID))
        {
            if (m_clientID_registeredClients.ContainsKey(m_gameID_clientID[gameID]))
            {
                if (m_clientID_registeredClients[m_gameID_clientID[gameID]].isConnected)
                {
                    return m_clientID_registeredClients[m_gameID_clientID[gameID]].viewDistance;
                }
                else
                {
                    Debug.LogWarning("NetworkingManager: server_getClientViewDistance: client for gameID \"" + gameID + "\" is not connected !");
                    return -1;
                }
            }
            else
            {
                Debug.LogWarning("NetworkingManager: server_getClientViewDistance: client for gameID \"" + gameID + "\" not found !");
                return -1;
            }
        }
        else
        {
            Debug.LogWarning("NetworkingManager: server_getClientViewDistance: client for gameID \"" + gameID + "\" not found !");
            return -1;
        }
    }

    public void server_UpdateExternalClientHealth(int gameID, float newHealth)
    {
        ClientUserData client = server_getClientUserData(gameID);

        if (client != null && client.isConnected)
        {
            m_healthPackageCounter++;
            m_clientEndPoint_HealthUpdatePackageID[client.lastIPEndPoint].m_uint = (uint)m_healthPackageCounter;
            m_clientEndPoint_PlayerHealth[client.lastIPEndPoint].m_float = newHealth;
            server_sendHealthUpdate(client.lastIPEndPoint);
        }
    }

    public int server_getNewUniqueGameID()
    {
        int returnValue = m_gameIDCounter;
        m_gameIDCounter++;

        return returnValue;
    }

    private void server_UDPLatencyPingManagement()
    {
        if (Time.realtimeSinceStartup > m_serverLastTimePing + m_timeBetweenPings)
        {
            m_output_clients_UDPLatency = new float[m_gameID_UDPLatency.Values.Count];
            int counter1 = 0;

            foreach (floatRef ping in m_gameID_UDPLatency.Values)
            {
                m_output_clients_UDPLatency[counter1] = ping.m_float;
                counter1++;
            }

            m_UDPServerLatencyPingPackageCounter++;
            m_serverLastTimePing = Time.realtimeSinceStartup;

            foreach (IPEndPoint clientEndPoint in m_clientID_clientEndPoint.Values)
            {
                server_sendUDPLatencyPing(clientEndPoint, m_UDPServerLatencyPingPackageCounter);
            }
        }
    }

    private void server_checkConnections()
    {
        // lost tcp connections
        List<IPEndPoint> lostConnections = m_TCPServerInterface.closeLostConnections();
        int temp_gameID;

        for (int i = 0; i < lostConnections.Count; i++)
        {
            Debug.Log("NetworkingManager: Lost TCP-Connection to: " + lostConnections[i].ToString());
            if (m_clientEndPoint_gameID.ContainsKey(lostConnections[i]))
            {
                temp_gameID = m_clientEndPoint_gameID[lostConnections[i]];
                if (m_gameID_clientID.ContainsKey(temp_gameID))
                {
                    server_closeConnectionForClient(m_gameID_clientID[temp_gameID]);
                }
            }
        }

        // new tcp connections

        IPEndPoint[] newConnections = m_TCPServerInterface.collectNewConnections();

        for (int i = 0; i < newConnections.Length; i++)
        {
            if(newConnections[i] == null)
            {
                Debug.LogWarning("NetworkingManager: server_checkConnections: new connection is null");
                continue;
            }

            if (m_newTCPConnections_time.ContainsKey(newConnections[i]))
            {
                Debug.LogWarning("NetworkingManager: server_checkTCPConnections: new TCP-Connection added multiple times: " + newConnections[i].ToString());
            }
            else
            {
                if (m_bannedIPAddresses.Contains(newConnections[i].Address))
                {
                    server_sendDisconnectCommand(newConnections[i], "you are banned from this server");
                    m_TCPServerInterface.closeConnectionForClient(newConnections[i]);
                }
                else
                {
#if CONNECTION_DEBUG
                    Debug.Log("NetworkingManager: server_checkTCPConnections: new TCP-Connection added");
#endif
                    m_newTCPConnections_time.Add(newConnections[i], new floatRef(Time.realtimeSinceStartup));
                }
            }
        }

        // check if in a new connection both TCP and UDP are ready

        List<IPEndPoint> foundEndPoints = new List<IPEndPoint>();

        foreach (KeyValuePair<IPEndPoint, floatRef> keyValuePairTCP in m_newTCPConnections_time)
        {
            foreach (KeyValuePair<IPEndPoint, floatRef> keyValuePairUDP in m_newUDPConnections_time)
            {
                if (keyValuePairTCP.Key.Equals(keyValuePairUDP.Key))
                {
                    // both TCP and UDP are now ready
                    foundEndPoints.Add(keyValuePairTCP.Key);
                }
            }
        }

        for (int i = 0; i < foundEndPoints.Count; i++)
        {
            m_newTCPConnections_time.Remove(foundEndPoints[i]);
            m_newUDPConnections_time.Remove(foundEndPoints[i]);

            server_sendLoginCommand(foundEndPoints[i]);
        }
    }

    private void server_autoKickManagement()
    {
        //  m_gameID_timeLastPong

        List<int> clientIDsToKick = new List<int>();

        foreach (KeyValuePair<int, IPEndPoint> client in m_gameID_clientEndPoint)
        {
            if(client.Key == -1) // dont kick local player
            {
                continue;
            }

            if (Time.realtimeSinceStartup > m_gameID_timeLastPong[client.Key].m_float + m_disconnectAfterSeconds)
            {
                clientIDsToKick.Add(m_gameID_clientID[client.Key]);
            }
        }

        for (int i = 0; i < clientIDsToKick.Count; i++)
        {
            Debug.Log("NetworkingManager: Server: Client (ID " + clientIDsToKick[i] + ") is not responding. Autokicking...");
            server_closeConnectionForClient(clientIDsToKick[i]);
        }
    }

    private void server_healthUpdatesManagement()
    {
        if (Time.realtimeSinceStartup > m_lastTime_healthUpdates + m_timeBetweenHealthUpdates)
        {
            m_lastTime_healthUpdates = Time.realtimeSinceStartup;
            foreach (KeyValuePair<IPEndPoint, uintRef> healthData in m_clientEndPoint_HealthUpdatePackageID)
            {
                if (healthData.Value.m_uint != 0)
                {
                    server_sendHealthUpdate(healthData.Key);
                }
            }
        }
    }

    public bool server_bannPlayerByIP(int gameID, string bannMessage)
    {
        if (m_gameID_clientID.ContainsKey(gameID))
        {
            if (m_clientID_clientEndPoint.ContainsKey(m_gameID_clientID[gameID]))
            {
                if (!m_bannedIPAddresses.Contains(m_clientID_clientEndPoint[m_gameID_clientID[gameID]].Address))
                {
                    m_bannedIPAddresses.Add(m_clientID_clientEndPoint[m_gameID_clientID[gameID]].Address);
                    server_kickPlayer(gameID, "banned from server: " + bannMessage);
                    return true;
                }
            }
        }

        return false;
    }

    public bool server_isClientConnected(int gameID)
    {
        if (m_gameID_clientID.ContainsKey(gameID))
        {
            if (m_clientID_registeredClients.ContainsKey(m_gameID_clientID[gameID]))
            {
                return m_clientID_registeredClients[m_gameID_clientID[gameID]].isConnected;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public bool server_kickPlayer(int gameID, string kickMessage)
    {
        if (m_gameID_clientID.ContainsKey(gameID))
        {
            return server_closeConnectionForClient(m_gameID_clientID[gameID], kickMessage);
        }
        else
        {
            return false;
        }
    }

    private bool server_closeConnectionForClient(IPEndPoint client, string message = "server closing connection")
    {
        Debug.Log("server_closeConnectionForClient: " + message);

        if (m_clientEndPoint_gameID.ContainsKey(client))
        {
            return server_closeConnectionForClient(m_gameID_clientID[m_clientEndPoint_gameID[client]], message);
        }
        else
        {
            server_sendDisconnectCommand(client, message);
            m_TCPServerInterface.closeConnectionForClient(client);
            return true;
        }
    }
    private bool server_closeConnectionForClient(int clientID, string message = "server closing connection")
    {
        if (m_clientID_gameID.ContainsKey(clientID))
        {
            int gameIDClient = m_clientID_gameID[clientID];
            IPEndPoint endPoint = m_clientID_clientEndPoint[clientID];

            server_sendDisconnectCommand(m_clientID_clientEndPoint[clientID], message);
            m_TCPServerInterface.closeConnectionForClient(endPoint);

            m_clientEndPoint_gameID.Remove(m_clientID_clientEndPoint[clientID]);
            m_clientID_clientEndPoint.Remove(clientID);
            m_gameID_clientIPAdress.Remove(gameIDClient);
            m_gameID_clientEndPoint.Remove(gameIDClient);
            m_gameID_playerPositionsPackageUID.Remove(gameIDClient);
            m_gameID_playerRotationsPackageUID.Remove(gameIDClient);
            m_gameID_UDPLatency.Remove(gameIDClient);
            m_gameID_timeLastPong.Remove(gameIDClient);
            m_clientEndPoint_HealthUpdatePackageID.Remove(endPoint);
            m_clientEndPoint_PlayerHealth.Remove(endPoint);
            m_clientEndPoint_viewDistanceUpdateTime.Remove(endPoint);

            m_clientID_registeredClients[clientID].isConnected = false;

            GameManager_Custom.singleton.server_onClientDisconnected(gameIDClient);

            m_connectedClientsCount--;

            return true;
        }
        else
        {
            return false;
        }
    }

    private void server_startConnectionForClient(IPEndPoint clientEndPoint, int clientID, string username, string userPasswordHash, string userIngameName)
    {
        //Debug.Log("NetworkingManager: Adding new Player-interface via TCP: " + clientEndPoint.ToString());

        int gameIDClient;

        if (m_clientID_gameID.ContainsKey(clientID))
        {
            // client was once connected
            gameIDClient = m_clientID_gameID[clientID];
            Debug.Log("Player reconnected \"" + username + "\"(\"" + userIngameName + "\")");
        }
        else
        {
            // client is connecting for the first time

            Debug.Log("A new player connected \"" + username + "\"(\"" + userIngameName + "\")");

            if (clientEndPoint.Equals(m_externServerEndPoint)) // is local player
            {
                gameIDClient = -1;
            }
            else
            {
                gameIDClient = m_gameIDCounter;
                m_gameIDCounter++;
            }
            m_clientID_registeredClients.Add(clientID, new ClientUserData(username, userPasswordHash, userIngameName, clientID));
            m_gameID_clientID.Add(gameIDClient, clientID);
            m_clientID_gameID.Add(clientID, gameIDClient);
        }

        ClientUserData userData = m_clientID_registeredClients[clientID];
        userData.ingameName = userIngameName;
        userData.isConnected = true;
        userData.lastIPEndPoint = clientEndPoint;

        if (clientID == -1)
        {
            m_client_clientUserData = userData;
        }

        m_clientEndPoint_PlayerHealth.Add(clientEndPoint, new floatRef(0));
        m_clientEndPoint_HealthUpdatePackageID.Add(clientEndPoint, new uintRef(0));
        m_clientEndPoint_gameID.Add(clientEndPoint, gameIDClient);
        m_clientID_clientEndPoint.Add(clientID, clientEndPoint);
        m_gameID_clientIPAdress.Add(gameIDClient, clientEndPoint.Address);
        m_gameID_clientEndPoint.Add(gameIDClient, clientEndPoint);
        m_gameID_playerPositionsPackageUID.Add(gameIDClient, new uintRef(0));
        m_gameID_playerRotationsPackageUID.Add(gameIDClient, new uintRef(0));
        m_gameID_UDPLatency.Add(gameIDClient, new floatRef(0));
        m_gameID_timeLastPong.Add(gameIDClient, new floatRef(Time.realtimeSinceStartup));
        m_clientEndPoint_viewDistanceUpdateTime.Add(clientEndPoint, new floatRef(0));

        server_sendConnectionSuccess(clientEndPoint, gameIDClient);

        m_connectedClientsCount++;
    }

    /// <summary>
    /// returns the GameID for a player based on the provieded iPEndPoint. returns -2 if GameID not found
    /// </summary>
    /// <param name="iPEndPoint"></param>
    /// <returns></returns>
    public int server_getPlayerGameIDForIPEndpoint(IPEndPoint iPEndPoint)
    {
        if (m_clientEndPoint_gameID.ContainsKey(iPEndPoint))
        {
            return m_clientEndPoint_gameID[iPEndPoint];
        }
        else
        {
            return -2;
        }
    }

    public List<byte> getGameSaveData()
    {
        List<byte> returnValue = new List<byte>();

        // m_clientID_gameID

        returnValue.AddRange(BitConverter.GetBytes(m_clientID_gameID.Count));

        foreach (KeyValuePair<int, int> pair in m_clientID_gameID)
        {
            returnValue.AddRange(BitConverter.GetBytes(pair.Key));
            returnValue.AddRange(BitConverter.GetBytes(pair.Value));
        }

        //m_bannedIPAddresses

        returnValue.AddRange(BitConverter.GetBytes(m_bannedIPAddresses.Count));

        foreach (IPAddress address in m_bannedIPAddresses)
        {
            byte[] addressStr = System.Text.Encoding.Unicode.GetBytes(address.ToString());

            returnValue.AddRange(BitConverter.GetBytes(addressStr.Length));
            returnValue.AddRange(addressStr);
        }

        // m_clientID_registeredClients

        returnValue.AddRange(BitConverter.GetBytes(m_clientID_registeredClients.Count));

        byte[] tempText;

        foreach (KeyValuePair<int, ClientUserData> client in m_clientID_registeredClients)
        {
            returnValue.AddRange(BitConverter.GetBytes(client.Key));
            returnValue.AddRange(BitConverter.GetBytes(client.Value.isAdmin));
            returnValue.AddRange(BitConverter.GetBytes(client.Value.isBanned));

            tempText = System.Text.Encoding.Unicode.GetBytes(client.Value.ingameName);

            returnValue.AddRange(BitConverter.GetBytes(tempText.Length));
            returnValue.AddRange(tempText);

            tempText = System.Text.Encoding.Unicode.GetBytes(client.Value.passwordHash);

            returnValue.AddRange(BitConverter.GetBytes(tempText.Length));
            returnValue.AddRange(tempText);

            tempText = System.Text.Encoding.Unicode.GetBytes(client.Value.username);

            returnValue.AddRange(BitConverter.GetBytes(tempText.Length));
            returnValue.AddRange(tempText);


            returnValue.AddRange(BitConverter.GetBytes(client.Value.playerSpawnpointsEntityIDs.Count));

            for (int i = 0; i < client.Value.playerSpawnpointsEntityIDs.Count; i++)
            {
                returnValue.AddRange(BitConverter.GetBytes(client.Value.playerSpawnpointsEntityIDs[i]));
            }
        }

        // m_gameIDCounter

        returnValue.AddRange(BitConverter.GetBytes(m_gameIDCounter));

        return returnValue;
    }

    public int loadFromSaveData(byte[] data, int index)
    {
        // m_clientID_gameID

        int tempCount = BitConverter.ToInt32(data, index);
        index += 4;

        m_clientID_gameID = new Dictionary<int, int>();

        for (int i = 0; i < tempCount; i++)
        {
            m_clientID_gameID.Add(BitConverter.ToInt32(data, index), BitConverter.ToInt32(data, index + 4));
            index += 8;
        }

        //m_bannedIPAddresses

        tempCount = BitConverter.ToInt32(data, index);
        index += 4;

        m_bannedIPAddresses = new HashSet<IPAddress>();

        for (int i = 0; i < tempCount; i++)
        {
            int strLength = BitConverter.ToInt32(data, index);
            index += 4;

            string ip = System.Text.Encoding.Unicode.GetString(data, index, strLength);
            m_bannedIPAddresses.Add(IPAddress.Parse(ip));

            index += strLength;
        }

        // m_clientID_registeredClients

        m_clientID_registeredClients = new Dictionary<int, ClientUserData>();

        tempCount = BitConverter.ToInt32(data, index);
        index += 4;

        for (int i = 0; i < tempCount; i++)
        {
            int clientID = BitConverter.ToInt32(data, index);
            index += 4;
            bool isAdmin = BitConverter.ToBoolean(data, index);
            index += 1;
            bool isBanned = BitConverter.ToBoolean(data, index);
            index += 1;

            int strLength = BitConverter.ToInt32(data, index);
            index += 4;

            string ingameName = System.Text.Encoding.Unicode.GetString(data, index, strLength);
            index += strLength;

            strLength = BitConverter.ToInt32(data, index);
            index += 4;

            string passwordHash = System.Text.Encoding.Unicode.GetString(data, index, strLength);
            index += strLength;

            strLength = BitConverter.ToInt32(data, index);
            index += 4;

            string username = System.Text.Encoding.Unicode.GetString(data, index, strLength);
            index += strLength;

            ClientUserData userData = new ClientUserData(username, passwordHash, ingameName, clientID);
            userData.isAdmin = isAdmin;
            userData.isBanned = isBanned;

            int spawnpointsCount = BitConverter.ToInt32(data, index);
            index += 4;

            for (int j = 0; j < spawnpointsCount; j++)
            {
                userData.playerSpawnpointsEntityIDs.Add(BitConverter.ToInt32(data, index));
                index += 4;
            }

            m_clientID_registeredClients.Add(userData.clientID, userData);
        }

        // m_gameIDCounter

        m_gameIDCounter = BitConverter.ToInt32(data, index);
        index += 4;

        if (GameManager_Custom.singleton.isServerAndClient)
        {
            m_clientID_registeredClients[-1].isConnected = true;
        }

        return index;
    }

    public List<string> server_connectionsDiagnostic()
    {
        return m_TCPServerInterface.getClientsDiagnostics();
    }

    #endregion

    #region Client Methodes

    /// <summary>
    /// closes TCP and UDP sockets (connections)
    /// </summary>
    public void client_endConnection()
    {
        m_isInitialized = false;
        m_TCPClient.dispose();
        m_TCPClient = null;
        m_clientUDPInterface.dispose();
        m_clientUDPInterface = null;
    }

    private void client_checkTCPConnectionLoss()
    {
        if (m_TCPClient.lostConnection)
        {
            Debug.LogWarning("NetworkingManager: Client: Lost TCP-Connection");
            GameManager_Custom.singleton.client_disconnected("Lost TCP-Connection");
        }
    }

    private void client_UDPLatencyPingManagement()
    {
        if (m_clientUDPInterface != null && Time.realtimeSinceStartup > m_clientLastTimeSendPing + m_timeBetweenPings)
        {
            m_clientPingPackageCounter++;
            m_clientLastTimeSendPing = Time.realtimeSinceStartup;

            client_sendPing(m_clientPingPackageCounter);
        }
    }

    private void client_createClientUserData(string username, string passwordHash, string ingameName)
    {
        m_client_clientUserData = new ClientUserData(username, passwordHash, ingameName, -1);
    }

    public void client_resetManager()
    {
        m_isInitialized = false;
        m_myPort = -1;
        m_UDPServerLatencyPingPackageCounter = 0;
        m_serverLastTimePing = 0;
        m_healthPackageCounter = 0;
        m_receivedMessagesCounter = 0;

        m_clientSendViewDistanceAgain = false;
        m_clientSendViewDistanceAgainTime = 0f;

        if (m_clientUDPInterface != null)
        {
            m_clientUDPInterface.dispose();
            m_clientUDPInterface = null;
        }

        if (m_TCPClient != null)
        {
            m_TCPClient.dispose();
            m_TCPClient = null;
        }

        m_externServerIP = null;
        m_externServerEndPoint = null;
        m_clientLastTimeSendPing = 0;
        m_clientLastTime_receivedLatencyPing = 0;
        m_lastLatency = 0;
        m_clientPingPackageCounter = 0;

        m_clientUsername = string.Empty;
        m_clientUserPasswordHash = string.Empty;
        m_clientUserIngameName = string.Empty;
        m_client_clientUserData = null;
    }

    #endregion

}
