using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Text;
using System;

public class GameManager_Custom : MonoBehaviour
{
    private class GameIDVector3 // players game id
    {
        public GameIDVector3(int gameID, Vector3 vector3)
        {
            m_gameID = gameID;
            m_vector3 = vector3;
        }

        public int m_gameID;
        public Vector3 m_vector3;
    }

    public static GameManager_Custom singleton;

    public enum PlayerState { Asleep, Awake, Dead }
    public enum ClientGameState { Uninitialized, AwaitingWorldParameters, LoadingWorld, WaitPlayerState }
    public enum ServerGameState { Uninitialized, ServerCheck, LoadingWorld, Publishing, Running } // TODO: ServerCheck: check if can be bound within this network and check if master server will accept this server ; Publishing: inform master server to add this server

    #region General members

    [SerializeField] private string m_gameVersion = "0.0.1.0"; //Major:Minor:Update:Patch/Hotfix
    [SerializeField] private float m_projectileMaxVisibleDistance = 1000f; // max distance to a player in order to receive an update from the server
    [SerializeField] private GameObject m_playerPrefab;
    [SerializeField] private GameObject m_externalPlayerPrefab;
    [SerializeField] private float m_server_timeSpawnSend = 0.1f; // time between 2 spawn-position-transmissions are send by the server
    [SerializeField] private bool m_DEBUG_SetServer = false;
    [SerializeField, ReadOnly] private NetworkingManager.HostRole m_currentRole = NetworkingManager.HostRole.Uninitialized;
    [Header("Server")]
    [SerializeField] private bool DEBUG_createSaveFileNow = false;
    [SerializeField] private bool DEBUG_startFromSaveFileNow = false;
    [SerializeField] private string m_saveFileDirectory = "Server\\";
    [SerializeField] private string m_saveFileName = "Game.save";
    [SerializeField] private float m_autoSaveFileInterval = 300;
    [SerializeField] private Vector2Int[] m_playerStartItems;

    private bool m_isGameInitialized = false; // is the game running: is connected to a server and the world has loaded..
    public bool isGameInitialized
    {
        get
        {
            return m_isGameInitialized;
        }
    }

    public string gameVersion { get { return m_gameVersion; } }

    private CharakterControl m_playerLocalPrefabScript = null;
    private bool m_startFromFileRunning = false;

    #endregion

    #region Server only variables

    private float m_lastTimeSaveFile = 0;
    private ServerGameState m_serverGameState = ServerGameState.Uninitialized;
    private List<Vector3> m_playerSpawnPoints = new List<Vector3>();
    private List<GameIDVector3> m_clientsToSpawn = new List<GameIDVector3>();
    private System.Random m_random_playerSpawns = new System.Random();
    private float m_lastTimeSpawnPackage = 0;
    private Dictionary<int, GameObject> m_server_gameID_NPCGameObject = new Dictionary<int, GameObject>();
    private Dictionary<int, NPCV2_Base> m_server_gameID_NPCScript = new Dictionary<int, NPCV2_Base>();
    private float m_server_lastTimePositionTransmission = 0;
    private uint m_SpawnPointGroupCounter = 0;
    private bool m_loadedFromSaveFile;

    #endregion

    #region Client only variables

    private ClientGameState m_clientGameState = ClientGameState.Uninitialized;
    private bool m_client_waitLoadingMesh = false;

    #endregion

    #region Unity Methodes (Awake, Start, Update...)

    void Awake()
    {
        Application.runInBackground = true;
        singleton = this;
        m_playerLocalPrefabScript = m_playerPrefab.GetComponent<CharakterControl>();
    }

    void Start()
    {
        if (WorldManager.singleton != null)
        {
            WorldManager.singleton.worldMeshesUpdatedEvent += onUpdateWorldMesh;
        }
    }

    void Update()
    {
        if (m_isGameInitialized)
        {

        }

        if (m_DEBUG_SetServer)
        {
            m_DEBUG_SetServer = false;
            m_currentRole = NetworkingManager.HostRole.Server;
            Debug.Log("DEBUG: Setting HostRole to Server");
        }

        if (DEBUG_createSaveFileNow)
        {
            DEBUG_createSaveFileNow = false;
            server_createGameSaveFile();
        }

        if (DEBUG_startFromSaveFileNow)
        {
            DEBUG_startFromSaveFileNow = false;
            startLoadingFromSaveFile();
        }
    }

    private void FixedUpdate()
    {
        if (isServer || isServerAndClient)
        {
            if (Time.time > m_lastTimeSaveFile + m_autoSaveFileInterval)
            {
                server_createGameSaveFile();
            }
        }
    }

    private void OnDestroy()
    {
        if (WorldManager.singleton != null)
        {
            WorldManager.singleton.worldMeshesUpdatedEvent -= onUpdateWorldMesh;
        }
    }

    #endregion

    #region initialize Host

    public void startAsServerWithLocalClient(float worldSeed, int worldSize, int serverPort, bool loadSaveFile)
    {
        if (m_currentRole != NetworkingManager.HostRole.Uninitialized)
        {
            GUIManager.singleton.displayMessage("can't change current Game Server/Client-Role. Please restart game !");
            Debug.LogError("can't change current Game Server/Client-Role. Please restart game !");
            return;
        }

        m_currentRole = NetworkingManager.HostRole.ServerAndClient;
        m_serverGameState = ServerGameState.LoadingWorld;
        m_clientGameState = ClientGameState.LoadingWorld;
        m_loadedFromSaveFile = loadSaveFile;

        if (loadSaveFile)
        {
            startLoadingFromSaveFile();
        }
        else
        {
            WorldManager.singleton.startBuildWorld(worldSeed, worldSize, true, true);
        }

        NetworkingManager.ConnectionStartResult initResult;

        NetworkingManager.singleton.initializeAsServerAndClient(serverPort, out initResult);

        if (initResult == NetworkingManager.ConnectionStartResult.Success)
        {
            // success
            string successMessage = "GameManager: successfully started as server";

            DevAnalytics.sendPlayerConnectedFeedback("LOCAL_PLAYER");

            GUIManager.singleton.displayMessage(successMessage);
            Debug.Log(successMessage);
        }
        else // failed
        {
            string errorMessage = "GameManager: Starting as server falied: ";

            switch (initResult)
            {
                case NetworkingManager.ConnectionStartResult.TCP_Failed:
                    {
                        errorMessage += " Binding TCP-Server failed. Perhaps the local port is occupied.";
                        break;
                    }
                case NetworkingManager.ConnectionStartResult.UDP_Failed:
                    {
                        errorMessage += " Binding UDP-Socket failed. Perhaps the local port is occupied.";
                        break;
                    }
                case NetworkingManager.ConnectionStartResult.Unknown_Failure:
                    {
                        errorMessage += " An unknown failure occurred.";
                        break;
                    }
                default:
                    {
                        errorMessage += " An not specified failure occurred.";
                        break;
                    }
            }

            GUIManager.singleton.displayMessage(errorMessage);
            Debug.LogWarning(errorMessage);
        }
    }

    public void startAsServer(float worldSeed, int worldSize, int serverPort, bool loadSaveFile)
    {
        if (m_currentRole != NetworkingManager.HostRole.Uninitialized)
        {
            GUIManager.singleton.displayMessage("can't change current Game Server/Client-Role. Please restart game !");
            Debug.LogError("can't change current Game Server/Client-Role. Please restart game !");
            return;
        }

        m_clientGameState = ClientGameState.Uninitialized;
        m_currentRole = NetworkingManager.HostRole.Server;
        m_serverGameState = ServerGameState.LoadingWorld;
        m_loadedFromSaveFile = loadSaveFile;

        if (loadSaveFile)
        {
            startLoadingFromSaveFile();
        }
        else
        {
            WorldManager.singleton.startBuildWorld(worldSeed, worldSize, true, true);
        }

        NetworkingManager.ConnectionStartResult initResult;

        NetworkingManager.singleton.initializeAsServer(serverPort, out initResult);

        if (initResult == NetworkingManager.ConnectionStartResult.Success)
        {
            // success

            string successMessage = "GameManager: successfully started as server";

            GUIManager.singleton.displayMessage(successMessage);
            Debug.Log(successMessage);
        }
        else
        {
            string errorMessage = "GameManager: Starting as server falied: ";

            switch (initResult)
            {
                case NetworkingManager.ConnectionStartResult.TCP_Failed:
                    {
                        errorMessage += " Binding TCP-Server failed. Perhaps the local port is occupied.";
                        break;
                    }
                case NetworkingManager.ConnectionStartResult.UDP_Failed:
                    {
                        errorMessage += " Binding UDP-Socket failed. Perhaps the local port is occupied.";
                        break;
                    }
                case NetworkingManager.ConnectionStartResult.Unknown_Failure:
                    {
                        errorMessage += " An unknown failure occurred.";
                        break;
                    }
                default:
                    {
                        errorMessage += " An not specified failure occurred.";
                        break;
                    }
            }

            GUIManager.singleton.displayMessage(errorMessage);
            Debug.LogWarning(errorMessage);
        }
    }

    public void startAsClient(System.Net.IPAddress serverIPAdress, int serverPort, int clientPort, string clientUsername, string clientUserPasswordHash, string clientUserIngameName)
    {
        if (m_currentRole != NetworkingManager.HostRole.Uninitialized)
        {
            string errorMessage = "can't change current Game Server/Client-Role. Please restart game !";

            GUIManager.singleton.setGUIDisconnectedActive();
            GUIManager.singleton.setGUIDisconnectedInfo(errorMessage);
            GUIManager.singleton.displayMessage(errorMessage);
            Debug.LogError(errorMessage);
            return;
        }

        System.Net.Sockets.SocketException networkException;
        NetworkingManager.ConnectionStartResult initResult;

        NetworkingManager.singleton.initializeAsClient(serverIPAdress, serverPort, clientPort, clientUsername, clientUserPasswordHash, clientUserIngameName, out initResult, out networkException);

        m_serverGameState = ServerGameState.Uninitialized;

        if (initResult == NetworkingManager.ConnectionStartResult.Success)
        {
            string successMessage = "GameManager: successfully started as client";

            DevAnalytics.sendPlayerConnectedFeedback(clientUsername.ToString());

            GUIManager.singleton.displayMessage(successMessage);
            Debug.Log(successMessage);

            m_currentRole = NetworkingManager.HostRole.Client;
            m_clientGameState = ClientGameState.AwaitingWorldParameters;
        }
        else // failed
        {
            string errorMessage;

            if (initResult == NetworkingManager.ConnectionStartResult.TCP_Failed)
            {
                errorMessage = "TCP: ";
            }
            else if (initResult == NetworkingManager.ConnectionStartResult.UDP_Failed)
            {
                errorMessage = "UDP: ";
            }
            else
            {
                errorMessage = "Unknown: ";
            }
            errorMessage += "Starting as client failed:\n";

            switch (networkException.ErrorCode)
            {
                case 10048:
                    {
                        errorMessage += "Socket address is already in use: \n(try to use another client port)";
                        break;
                    }
                case 10061:
                    {
                        errorMessage += "Target host refused connection. \n(check IP-address and port)";
                        break;
                    }
                default:
                    {
                        errorMessage += "ErrorCode " + networkException.ErrorCode + ": " + networkException.ToString();
                        break;
                    }
            }

            GUIManager.singleton.setGUIDisconnectedActive();
            GUIManager.singleton.setGUIDisconnectedInfo(errorMessage);
            GUIManager.singleton.displayMessage(errorMessage);
            Debug.LogWarning(errorMessage);
        }
    }

    public void startAsDebugServerWithLocalClient(int serverPort)
    {
        if (m_currentRole != NetworkingManager.HostRole.Uninitialized)
        {
            GUIManager.singleton.displayMessage("can't change current Game Server/Client-Role. Please restart game !");
            Debug.LogError("can't change current Game Server/Client-Role. Please restart game !");
            return;
        }

        m_currentRole = NetworkingManager.HostRole.ServerAndClient;
        m_serverGameState = ServerGameState.Running;
        m_clientGameState = ClientGameState.AwaitingWorldParameters;
        m_isGameInitialized = true;
        m_loadedFromSaveFile = false;

        NetworkingManager.ConnectionStartResult initResult;

        NetworkingManager.singleton.initializeAsServerAndClient(serverPort, out initResult);

        if (initResult == NetworkingManager.ConnectionStartResult.Success)
        {
            // success
            string successMessage = "GameManager: successfully started as server";

            DevAnalytics.sendPlayerConnectedFeedback("LOCAL_PLAYER");

            if (GUIManager.singleton != null)
            {
                GUIManager.singleton.displayMessage(successMessage);
            }

            Debug.Log(successMessage);
        }
        else // failed
        {
            string errorMessage = "GameManager: Starting as server falied: ";

            switch (initResult)
            {
                case NetworkingManager.ConnectionStartResult.TCP_Failed:
                    {
                        errorMessage += " Binding TCP-Server failed. Perhaps the local port is occupied.";
                        break;
                    }
                case NetworkingManager.ConnectionStartResult.UDP_Failed:
                    {
                        errorMessage += " Binding UDP-Socket failed. Perhaps the local port is occupied.";
                        break;
                    }
                case NetworkingManager.ConnectionStartResult.Unknown_Failure:
                    {
                        errorMessage += " An unknown failure occurred.";
                        break;
                    }
                default:
                    {
                        errorMessage += " An not specified failure occurred.";
                        break;
                    }
            }

            Debug.LogWarning(errorMessage);
        }
    }

    #endregion

    #region server only methods

    /// <summary>
    /// overrides currents player spawn points with the provided spawn points
    /// </summary>
    /// <param name="spawnPoints"></param>
    public void setGlobalPlayerSpawnPoints(List<Vector3> spawnPoints)
    {
        if (isServer || isServerAndClient)
        {
            m_playerSpawnPoints = spawnPoints;
            Debug.Log("GameManager_Custom: " + spawnPoints.Count + " spawnpoints were set as global spawnPoints");
        }
        else
        {
            Debug.LogWarning("GameManager_Custom: Tried to set Spawnpoints while not server");
        }
    }

    public void server_onClientSpawnConfirmation(int gameID)
    {
        //Debug.Log("GameManager_Custom: server_onClientSpawnConfirmation: gameID: " + gameID);

        for (int i = 0; i < m_clientsToSpawn.Count; i++)
        {
            if (m_clientsToSpawn[i].m_gameID == gameID)
            {
                if (gameID == -1) // local player
                {

                }
                else
                {
                    Debug.Log("spawning new player: " + gameID);

                    GameObject tempPlayer = Instantiate(m_externalPlayerPrefab, m_clientsToSpawn[i].m_vector3, Quaternion.identity) as GameObject;
                    tempPlayer.name = "Player \"" + gameID + "\"";
                    Player_external tempPlayerScript = tempPlayer.GetComponent<Player_external>();
                    tempPlayerScript.setGameID(gameID);

                    for (int j = 0; j < m_playerStartItems.Length; j++)
                    {
                        StorableItem item = ItemManager.singleton.createNewStorableItem(m_playerStartItems[j].x, m_playerStartItems[j].y);
                        tempPlayerScript.tryAddItem(item);
                    }
                }

                m_clientsToSpawn.RemoveAt(i);
                return;
            }
        }
    }

    public void server_onReceivedSpawnRequest(int gameID, int spawnLocation)
    {
        for (int i = 0; i < m_clientsToSpawn.Count; i++)
        {
            if (m_clientsToSpawn[i].m_gameID == gameID)
            {
                // already recognized
                return;
            }
        }

        if (EntityManager.singleton.isPlayerAlive(gameID))
        {
            // already spawned
            return;
        }

        bool choosRandomSpawn = false;

        if (spawnLocation == -1) // random
        {
            choosRandomSpawn = true;
        }
        else
        {
            ClientUserData userData = NetworkingManager.singleton.server_getClientUserData(gameID);

            if (userData == null)
            {
                Debug.LogWarning("GameManager_Custom: server_onReceivedSpawnRequest: userData for player " + gameID + " is null");
                choosRandomSpawn = true;
            }
            else
            {
                List<int> entityIDs;
                List<Vector3> playerSpawnpoints;
                List<float> cooldownTimes;

                EntityManager.singleton.getSpawnpoints(gameID, out entityIDs, out cooldownTimes, out playerSpawnpoints);

                if (playerSpawnpoints.Count > 0 && spawnLocation < playerSpawnpoints.Count)
                {
                    if (cooldownTimes[spawnLocation] < 0)
                    {
                        Vector3 spawnPos = playerSpawnpoints[spawnLocation];
                        EntityManager.singleton.setSpawnpointCooldown(entityIDs[spawnLocation]);
                        server_onReceivedSpawnRequest(gameID, spawnPos);
                    }
                    else
                    {
                        NetworkingManager.singleton.server_sendSpawnpointCooldown(gameID, entityIDs[spawnLocation], cooldownTimes[spawnLocation]); // spawnpoint on cooldown -> @client: update your cooldown times
                    }
                }
                else
                {
                    Debug.LogWarning("GameManager_Custom: server_onReceivedSpawnRequest: Spawnpoint index out of range: " + spawnLocation);
                    choosRandomSpawn = true;
                }
            }
        }

        if (choosRandomSpawn)
        {
            Vector3 randomSpawnPos = m_playerSpawnPoints[m_random_playerSpawns.Next(m_playerSpawnPoints.Count)];
            server_onReceivedSpawnRequest(gameID, randomSpawnPos);
        }
    }

    public void server_onReceivedSpawnRequest(int gameID, Vector3 position)
    {
        m_clientsToSpawn.Add(new GameIDVector3(gameID, position));
        NetworkingManager.singleton.server_sendPlayerSpawnCommand(gameID, position);
    }

    public void server_onClientDisconnected(int gameID)
    {
        EntityManager.singleton.server_onPlayerDisconnect(gameID);
        PlayerManager.singleton.removeWorldViewPoint(gameID);
    }

    public void server_sendNewProjectileToPlayersInRange(int weaponIndex, int projectileID, Vector3 startPosition, Vector3 direction)
    {
        foreach (KeyValuePair<int, Player_external> playerToReceive in EntityManager.singleton.getPlayerID_ExternalScriptsDict())
        {
            if (Vector3.Distance(startPosition, playerToReceive.Value.transform.position) < m_projectileMaxVisibleDistance)
            {
                NetworkingManager.singleton.server_sendNewProjectile(playerToReceive.Key, weaponIndex, projectileID, startPosition, direction);
            }
        }
    }

    public void server_sendProjectileUpdateToPlayersInRange(int projectileID, Vector3 position)
    {
        foreach (KeyValuePair<int, Player_external> playerToReceive in EntityManager.singleton.getPlayerID_ExternalScriptsDict())
        {
            if (Vector3.Distance(position, playerToReceive.Value.transform.position) < m_projectileMaxVisibleDistance)
            {
                NetworkingManager.singleton.server_sendProjectileUpdate(playerToReceive.Key, projectileID, position);
            }
        }
    }

    /// <summary>
    /// sends a sound to all players in hear range except ignoreGameID
    /// </summary>
    /// <param name="soundID"></param>
    /// <param name="position"></param>
    /// <param name="ignoreGameID"></param>
    public void server_sendSoundToPlayersInRange(int soundID, Vector3 position, int ignoreGameID)
    {
        foreach (KeyValuePair<int, Player_external> playerToReceive in EntityManager.singleton.getPlayerID_ExternalScriptsDict())
        {
            if (playerToReceive.Key == ignoreGameID)
            {
                continue;
            }

            if (Vector3.Distance(position, playerToReceive.Value.transform.position) < SoundManager.singleton.getMaxHearableDistance(soundID))
            {
                NetworkingManager.singleton.server_sendWorldSound(playerToReceive.Key, soundID, position);
            }
        }
    }

    public void server_onPlayerNoHealth(int gameID)
    {
        Player_base player = EntityManager.singleton.getActivePlayer(gameID);

        if (player == null)
        {
            Debug.LogWarning("GameManager: server_onPlayerNoHealth: unknown Game ID \"" + gameID + "\"");
        }
        else
        {
            player.server_dropAllItems();
            Destroy(player.gameObject);

            NetworkingManager.singleton.server_sendPlayerDeadMessage(gameID);

            // TEST
            //onServerReceivedSpawnRequest(gameID);
            //onServerClientSpawnConfirmation(gameID);
            // end TEST

            Debug.Log("Player (gameID: " + gameID + ") died.");
        }
    }

    /// <summary>
    /// returns new unique game id
    /// </summary>
    /// <param name="NPCObject"></param>
    /// <param name="NPCScript"></param>
    /// <returns></returns>
    public int server_registerNewNPC(GameObject NPCObject, NPCV2_Base NPCScript)
    {
        if (m_server_gameID_NPCGameObject.ContainsValue(NPCObject))
        {
            Debug.LogError("GameManager_Custom: attempt to register same NPC twice !");
            return -2;
        }
        else
        {
            int uniqueID = NetworkingManager.singleton.server_getNewUniqueGameID();
            m_server_gameID_NPCGameObject.Add(uniqueID, NPCObject);
            m_server_gameID_NPCScript.Add(uniqueID, NPCScript);
            return uniqueID;
        }
    }

    public List<NPCV2_Base> getAllNPCs()
    {
        List<NPCV2_Base> npcs = new List<NPCV2_Base>();

        foreach (KeyValuePair<int, NPCV2_Base> pair in m_server_gameID_NPCScript)
        {
            if (pair.Value != null)
            {
                npcs.Add(pair.Value);
            }
        }

        return npcs;
    }

    public void server_releaseNPC(int gameID)
    {
        if (m_server_gameID_NPCGameObject.ContainsKey(gameID))
        {
            m_server_gameID_NPCGameObject.Remove(gameID);
        }

        if (m_server_gameID_NPCScript.ContainsKey(gameID))
        {
            m_server_gameID_NPCScript.Remove(gameID);
        }
    }

    public void server_passOnHitConfirmation(int gameID)
    {
        if (EntityManager.singleton.getPlayerID_ExternalScriptsDict().ContainsKey(gameID) || gameID == -1) // = player
        {
            NetworkingManager.singleton.server_sendHitConfirmation(gameID);
        }
        else if (m_server_gameID_NPCScript.ContainsKey(gameID)) // = NPC
        {
            m_server_gameID_NPCScript[gameID].onHitConfirmation();
        }
    }

    public void server_createGameSaveFile()
    {
        m_lastTimeSaveFile = Time.time;

        float startTime = Time.realtimeSinceStartup;

        GameSaveFile saveFile = new GameSaveFile();

        // game manager

        byte[] gameVersion = Encoding.Unicode.GetBytes(m_gameVersion);

        saveFile.addBytes(BitConverter.GetBytes(gameVersion.Length));
        saveFile.addBytes(gameVersion);

        // networking manager

        saveFile.addBytes(NetworkingManager.singleton.getGameSaveData());

        // Worldmanager

        saveFile.addBytes(WorldManager.singleton.getGameSaveData());

        // catastrophe manager

        // TODO

        // player building manager

        saveFile.addBytes(PlayerBuildingManager.singleton.getGameSaveData());

        // entity manager

        saveFile.addBytes(EntityManager.singleton.getGameSaveData());

        saveFile.writeToDisk(m_saveFileDirectory, m_saveFileName);

        Debug.Log("GameManager_Custom: game saved (" + (Time.realtimeSinceStartup - startTime) + " s)");
    }

    public void startLoadingFromSaveFile()
    {
        if (!m_startFromFileRunning)
        {
            StartCoroutine(server_startFromSaveFile());
        }
    }

    private IEnumerator server_startFromSaveFile()
    {
        m_startFromFileRunning = true;

        GUIManager.singleton.setGUILoadingActive();

        if (checkSaveFileAvailable())
        {
            byte[] saveFile = getSaveFileBytes();
            int index = 0;

            int strLength = BitConverter.ToInt32(saveFile, index);
            index += 4;

            string gameVersion = Encoding.Unicode.GetString(saveFile, index, strLength);
            index += strLength;

            if (gameVersion == m_gameVersion)
            {
                // networking manager

                GUIManager.singleton.setGUILoadingProgressText("Loading Save File: Network Data");
                yield return null;
                index = NetworkingManager.singleton.loadFromSaveData(saveFile, index);

                // Worldmanager

                GUIManager.singleton.setGUILoadingProgressText("Loading Save File: World");
                yield return null;
                index = WorldManager.singleton.loadFromSaveData(saveFile, index); // starts coroutine

                while (!WorldManager.singleton.worldBuildDone)
                {
                    yield return null;
                }
                GUIManager.singleton.setGUILoadingActive();

                // catastrophe manager

                // TODO

                // player building manager

                GUIManager.singleton.setGUILoadingProgressText("Loading Save File: Buildings");
                yield return null;
                index = PlayerBuildingManager.singleton.loadFromSaveData(saveFile, index);

                // entity manager

                GUIManager.singleton.setGUILoadingProgressText("Loading Save File: Entities");
                yield return null;
                index = EntityManager.singleton.loadFromSaveData(saveFile, index);
            }
            else
            {
                string errorText = "GameManager_Custom: server_startFromSaveFile: game versions differ: current version: " + m_gameVersion + ", save file version: " + gameVersion;

                Debug.LogError(errorText);
                GUIManager.singleton.setGUILoadingProgressText(errorText);

                float startWaitTime = Time.time;

                while (Time.time < startWaitTime + 5f)
                {
                    yield return null;
                }
            }
        }
        else
        {
            string errorText = "GameManager_Custom: server_startFromSaveFile: save file not found !";

            Debug.LogError(errorText);
            GUIManager.singleton.setGUILoadingProgressText(errorText);

            float startWaitTime = Time.time;

            while (Time.time < startWaitTime + 5f)
            {
                yield return null;
            }
        }

        if (m_currentRole == NetworkingManager.HostRole.ServerAndClient)
        {
            NetworkingManager.singleton.client_sendPlayerStateRequest();
        }

        m_startFromFileRunning = false;
    }

    private bool checkSaveFileAvailable()
    {
        string basePath = System.IO.Directory.GetCurrentDirectory();
        return System.IO.File.Exists(basePath + "\\" + m_saveFileDirectory + "\\" + m_saveFileName);
    }

    private byte[] getSaveFileBytes()
    {
        string basePath = System.IO.Directory.GetCurrentDirectory();
        return System.IO.File.ReadAllBytes(basePath + "\\" + m_saveFileDirectory + "\\" + m_saveFileName);
    }

    #endregion

    #region client only methods

    public void client_onReceivedMapParameters(float seed, int size)
    {
        if (m_currentRole == NetworkingManager.HostRole.Client) // oppose to server and client
        {
            if (m_clientGameState == ClientGameState.AwaitingWorldParameters)
            {
                GUIManager.singleton.displayMessage("GameManager: received map parameters: seed: " + seed + ", size: " + size);
                m_clientGameState = ClientGameState.LoadingWorld;
                WorldManager.singleton.startBuildWorld(seed, size, false, false);
            }
            else
            {
                Debug.LogWarning("GameManager: onClientReceivedSeed() called twice !");
            }
        }
    }

    public void client_onReceivedSpawnCommand(Vector3 position)
    {
        PlayerManager.singleton.getWorldViewPoint(-1).transform.position = position;

        m_client_waitLoadingMesh = true;

        if (WorldManager.singleton == null)
        {
            onUpdateWorldMesh(this, null);
        }
        else
        {
            WorldManager.singleton.terrainMeshesForceRenderOnce();
            GUIManager.singleton.setGUILoadingActive();
            GUIManager.singleton.setGUILoadingProgressText("Loading Surroundings");
        }
    }

    public void client_updatePlayerHealth(float newHealth)
    {
        if (EntityManager.singleton.getLocalPlayer() != null)
        {
            EntityManager.singleton.getLocalPlayer().setHealth(newHealth);
        }
    }

    public void client_disconnected(string disconnectInfo)
    {
        Debug.Log("Client disconnected: " + disconnectInfo);

        resetAllManagers();

        GUIManager.singleton.setGUIDisconnectedInfo(disconnectInfo);
        GUIManager.singleton.setGUIDisconnectedActive();
    }

    public void client_onPlayerDead()
    {
        GUIManager.singleton.setGUIDeadScreenActive();

        if (isClient) // and not server and client
        {
            if (EntityManager.singleton.isLocalPlayerActive())
            {
                Destroy(EntityManager.singleton.getLocalPlayer().gameObject);
            }
        }

        EntityManager.singleton.unregisterLocalPlayer();
    }

    public void client_onPlayerAsleep(Vector3 playerPosition)
    {
        GUIManager.singleton.setGUIAsleepScreenActive();

        if (isClient) // and not server and client
        {
            if (EntityManager.singleton.isLocalPlayerActive())
            {
                Destroy(EntityManager.singleton.getLocalPlayer().gameObject);
            }
        }

        EntityManager.singleton.unregisterLocalPlayer();

        PlayerManager.singleton.getWorldViewPoint(-1).transform.position = playerPosition;
    }

    public void client_spawnRequestRandomLocation()
    {
        NetworkingManager.singleton.client_sendPlayerSpawnRequest(-1);
    }

    public void client_wakeUpRequest()
    {
        NetworkingManager.singleton.client_sendPlayerWakeUpRequest();
    }

    public void client_spawnRequestSavedLocation(int index)
    {
        NetworkingManager.singleton.client_sendPlayerSpawnRequest(index);
    }

    #endregion

    public void onWorldBuilderLoadMapDone()
    {
        m_isGameInitialized = true; // no more loading. normal behavior can begin

        if (m_currentRole == NetworkingManager.HostRole.Client)
        {
            if (m_clientGameState == ClientGameState.LoadingWorld)
            {
                m_clientGameState = ClientGameState.WaitPlayerState;
                NetworkingManager.singleton.client_sendPlayerStateRequest();
            }
            else
            {
                Debug.LogWarning("GameManager: onWorldBuilderLoadMapDone called twice !");
            }
        }
        else if (m_currentRole == NetworkingManager.HostRole.Server)
        {
            m_playerSpawnPoints = WorldManager.singleton.beachSpawnPoint;
            if (m_playerSpawnPoints.Count == 0)
            {
                Debug.LogWarning("Spawnpoint.count = 0. 0,0,0 spawnpoint added");
                m_playerSpawnPoints.Add(new Vector3(0, 0, 0));
            }
        }
        else if (m_currentRole == NetworkingManager.HostRole.ServerAndClient)
        {
            if (!m_loadedFromSaveFile)
            {
                NetworkingManager.singleton.client_sendPlayerStateRequest();
            }

            m_playerSpawnPoints = WorldManager.singleton.beachSpawnPoint;
            if (m_playerSpawnPoints.Count == 0)
            {
                Debug.LogWarning("Spawnpoint.count = 0. 0,0,0 spawnpoint added");
                m_playerSpawnPoints.Add(new Vector3(0, 0, 0));
            }
        }
        else
        {
            Debug.Log("GameManager_Custom: onWorldBuilderLoadMapDone: m_currentRole in unexpected state \"" + m_currentRole + "\"");
        }
    }

    /// <summary>
    /// rather expensive
    /// </summary>
    /// <param name="gameID"></param>
    /// <returns></returns>
    public PlayerState getPlayerState(int gameID)
    {
        Vector3 unused;

        return getPlayerState(gameID, out unused);
    }
    /// <summary>
    /// rather expensive
    /// </summary>
    /// <param name="gameID"></param>
    /// <returns></returns>
    public PlayerState getPlayerState(int gameID, out Vector3 playerPosition)
    {
        Player_base player = EntityManager.singleton.getActivePlayer(gameID);

        if (player == null)
        {
            Player_sleeper sleeperEntity = EntityManager.singleton.getSleepingPlayerEntity(gameID);

            if (sleeperEntity == null)
            {
                DataEntity_Player culledSleeper = EntityManager.singleton.getCulledSleepingPlayer(gameID);

                if (culledSleeper == null)
                {
                    playerPosition = Vector3.zero;
                    return PlayerState.Dead;
                }
                else
                {
                    playerPosition = culledSleeper.m_position;
                    return PlayerState.Asleep;
                }
            }
            else
            {
                playerPosition = sleeperEntity.transform.position;
                return PlayerState.Asleep;
            }
        }
        else
        {
            playerPosition = player.transform.position;
            return PlayerState.Awake;
        }
    }

    public int getUniqueSpawnPointID()
    {
        m_SpawnPointGroupCounter++;
        return (int)m_SpawnPointGroupCounter;
    }

    public bool isServer
    {
        get
        {
            if (m_currentRole == NetworkingManager.HostRole.Server)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool isClient
    {
        get
        {
            if (m_currentRole == NetworkingManager.HostRole.Client)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool isServerAndClient
    {
        get
        {
            if (m_currentRole == NetworkingManager.HostRole.ServerAndClient)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public void killLocalPlayer()
    {
        if (EntityManager.singleton.isLocalPlayerActive())
        {
            if (isServer || isServerAndClient)
            {
                server_onPlayerNoHealth(EntityManager.singleton.getLocalPlayer().m_gameID);
            }
            else
            {
                NetworkingManager.singleton.client_sendPlayerSuicideRequest();
            }
        }
    }

    public string Cmd_killPlayer(int gameID)
    {
        if (isServer || isServerAndClient)
        {
            if (EntityManager.singleton.isPlayerAlive(gameID))
            {
                server_onPlayerNoHealth(gameID);
                return "killed player: gameID \"" + gameID + "\"";
            }
            else
            {
                return "player (gameID \"" + gameID + "\") not found";
            }
        }
        else
        {
            return "only servers can use this command";
        }
    }

    public void server_PlayerSuicideRequest(int gameID)
    {
        server_onPlayerNoHealth(gameID);
    }

    public void onUpdateWorldMesh(object sender, EventArgs args)
    {
        if (m_client_waitLoadingMesh)
        {
            m_client_waitLoadingMesh = false;

            spawnLocalPlayer();
        }
    }

    public Vector3 getPlayerLocalCameraOffset()
    {
        return new Vector3(0, m_playerLocalPrefabScript.getCameraHeightOffset(), 0);
    }

    private void spawnLocalPlayer()
    {
        GameObject player = Instantiate(m_playerPrefab, PlayerManager.singleton.getWorldViewPoint(-1).transform.position, Quaternion.identity) as GameObject;
        player.name = "Player: Local Player";

        if (isServerAndClient)
        {
            Player_local playerScript = player.GetComponent<Player_local>();
            playerScript.setGameID(-1);

            for (int j = 0; j < m_playerStartItems.Length; j++)
            {
                StorableItem item = ItemManager.singleton.createNewStorableItem(m_playerStartItems[j].x, m_playerStartItems[j].y);
                playerScript.tryAddItem(item);
            }
        }

        //WorldManager.singleton.WorldViewpointsList[0].transform.SetParent(player.transform);
        //WorldManager.singleton.WorldViewpointsList[0].transform.localPosition = Vector3.zero;

        GUIManager.singleton.onPlayerSpawn();
        SoundManager.singleton.setAmbientSoundActivity(true);

        NetworkingManager.singleton.client_sendSpawnConfirmation();
    }

    public void DEBUG_spawnLocalPlayer(Vector3 position)
    {
        Debug.LogWarning("GameManager_Custom: DEBUG_spawnLocalPlayer");

        NetworkingManager.singleton.client_sendPlayerSpawnRequestDebugPosition(position);
    }

    private void resetAllManagers()
    {
        EntityManager.singleton.client_resetManager();
        TerrainDetailManager.singleton.setActivity(false);
        ProjectileManager.singleton.client_resetManager();
        SoundManager.singleton.setAmbientSoundActivity(false);
        NetworkingManager.singleton.client_resetManager();

        // gamemanager Reset
        m_currentRole = NetworkingManager.HostRole.Uninitialized;
        m_clientGameState = ClientGameState.Uninitialized;
        m_client_waitLoadingMesh = false;
        m_isGameInitialized = false;
    }
}
