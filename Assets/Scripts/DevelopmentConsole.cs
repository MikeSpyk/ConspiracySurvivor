#define WRITE_LOG_FILE
//#undef WRITE_LOG_FILE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DevelopmentConsole : MonoBehaviour
{
    [SerializeField] PanelExpanderText m_GUITextInterface;
    [SerializeField] GameObject m_ConsoleGUIObject;
    [SerializeField] InputField m_ConsoleGUIInputField;
    [SerializeField] private string m_DEBUG_Input = "";
    [SerializeField] private bool m_DEBUG_applyInput = false;
    [SerializeField] private KeyCode m_ConsoleKey = KeyCode.F1;
    [SerializeField] private int m_maxCharsPerMessage = 100;
    [SerializeField] private string[] m_startUpCommands;

    private int m_inputFieldLastFrameFocused = 0;
    private Stack<string> m_lastInputMessages = new Stack<string>();
    private Stack<string> m_usedInputMessages = new Stack<string>();

    private Queue<string> m_threadedLogCondition = new Queue<string>();
    private Queue<string> m_threadedLogStacktrace = new Queue<string>();
    private Queue<LogType> m_threadedLogType = new Queue<LogType>();

    private void Awake()
    {
        //Application.logMessageReceived += Application_logMessageReceived;
        Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;
    }

    private void Start()
    {
        if (m_startUpCommands != null)
        {
            for (int i = 0; i < m_startUpCommands.Length; i++)
            {
                applyCommand(m_startUpCommands[i]);
            }
        }
    }

    private void Update()
    {
        lock (m_threadedLogCondition)
        {
            while (m_threadedLogCondition.Count > 0)
            {
                Application_logMessageReceived(m_threadedLogCondition.Dequeue(), m_threadedLogStacktrace.Dequeue(), m_threadedLogType.Dequeue());
            }
        }

        if (m_DEBUG_applyInput)
        {
            m_DEBUG_applyInput = false;
            applyCommand(m_DEBUG_Input);
        }

        if (Input.GetKeyDown(m_ConsoleKey))
        {
            if (m_ConsoleGUIObject.activeSelf)
            {
                m_ConsoleGUIObject.SetActive(false);
            }
            else
            {
                m_ConsoleGUIObject.SetActive(true);
                m_ConsoleGUIInputField.Select();
                m_ConsoleGUIInputField.ActivateInputField();

                setCursorActive();
            }
        }

        if (m_ConsoleGUIInputField.isFocused)
        {
            m_inputFieldLastFrameFocused = Time.frameCount;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (Time.frameCount < m_inputFieldLastFrameFocused + 2)
            {
                if (m_ConsoleGUIInputField.text != string.Empty)
                {
                    applyCommand(m_ConsoleGUIInputField.text);
                    m_ConsoleGUIInputField.text = "";
                }

                m_ConsoleGUIInputField.Select();
                m_ConsoleGUIInputField.ActivateInputField();
            }
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (m_lastInputMessages.Count != 0 || m_usedInputMessages.Count != 0)
            {
                if (m_lastInputMessages.Count < 1)
                {
                    // reset
                    while (m_usedInputMessages.Count > 0)
                    {
                        m_lastInputMessages.Push(m_usedInputMessages.Pop());
                    }
                }

                string currentMessage = m_lastInputMessages.Pop();
                m_ConsoleGUIInputField.text = currentMessage;
                m_usedInputMessages.Push(currentMessage);
            }
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (m_lastInputMessages.Count != 0 || m_usedInputMessages.Count != 0)
            {
                if (m_usedInputMessages.Count < 1)
                {
                    // reset
                    while (m_lastInputMessages.Count > 0)
                    {
                        m_usedInputMessages.Push(m_lastInputMessages.Pop());
                    }
                }

                string currentMessage = m_usedInputMessages.Pop();
                m_ConsoleGUIInputField.text = currentMessage;
                m_lastInputMessages.Push(currentMessage);
            }
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            m_ConsoleGUIInputField.text = "";
        }
    }

    private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
    {
        string messageType = "UnityLog (";

        switch (type)
        {
            case LogType.Assert:
                {
                    messageType += "Assert)";
                    break;
                }
            case LogType.Error:
                {
                    messageType += "Error)";
                    break;
                }
            case LogType.Exception:
                {
                    messageType += "Exception)";
                    break;
                }
            case LogType.Log:
                {
                    messageType += "Log)";
                    break;
                }
            case LogType.Warning:
                {
                    messageType += "Warning)";
                    break;
                }
            default:
                {
                    messageType += "Assert)";
                    break;
                }
        }

        printText(string.Format("{0}: {1}: {2}", messageType, condition.Trim().Replace(System.Environment.NewLine, " "), stackTrace.Trim().Replace(System.Environment.NewLine, " ")));
    }

    private void Application_logMessageReceivedThreaded(string condition, string stackTrace, LogType type)
    {
        lock (m_threadedLogCondition)
        {
            m_threadedLogCondition.Enqueue(condition);
            m_threadedLogStacktrace.Enqueue(stackTrace);
            m_threadedLogType.Enqueue(type);
        }
    }

    private void setCursorActive()
    {
        // TODO: remove null check and make an event for activate and make the guimanager subscribe to the event
        GUIManager.singleton.setCursorActive(true);
    }

    private void printText(string text)
    {
        System.DateTime time = System.DateTime.Now;

        text = string.Format("[{0}:{1}:{2}] {3}", time.Hour, time.Minute, time.Second, text);

#if WRITE_LOG_FILE
        DevAnalytics.writeToLogFile(text);
#endif

        List<string> subStrings = new List<string>();

        for (int i = 0; i < text.Length; i += m_maxCharsPerMessage)
        {
            int length = Mathf.Min(m_maxCharsPerMessage, text.Length - i);

            subStrings.Add(text.Substring(i, length));
        }

        for (int i = 0; i < subStrings.Count; i++)
        {
            m_GUITextInterface.addNewText(subStrings[i]);
        }
    }

    private void applyCommand(string userInput)
    {
        //Debug.Log("Console Command: \"" + command + "\"");
        printText(userInput);

        // expand last messages stack
        while (m_usedInputMessages.Count > 0)
        {
            m_lastInputMessages.Push(m_usedInputMessages.Pop());
        }
        m_lastInputMessages.Push(userInput);

        string[] commandSplitted = userInput.Split(' '); // [0] = command, rest = arguments
        commandSplitted[0] = commandSplitted[0].ToLower();

        string command = commandSplitted[0];
        string[] arguments = new string[commandSplitted.Length - 1];

        for (int i = 1; i < commandSplitted.Length; i++)
        {
            arguments[i - 1] = commandSplitted[i];
        }

        switch (commandSplitted[0])
        {
            default:
                {
                    printText(string.Format("Unkown Command \"{0}\"", commandSplitted[0]));
                    break;
                }
            case "kill":
                {
                    if (commandSplitted.Length > 1)
                    {
                        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                        {
                            int parseResult;
                            if (int.TryParse(commandSplitted[1], out parseResult))
                            {
                                printText(GameManager_Custom.singleton.Cmd_killPlayer(parseResult));
                            }
                            else
                            {
                                printText("argument isnt of type int");
                            }
                        }
                        else
                        {
                            printServerUseOnlyText();
                        }
                    }
                    else // length = 1
                    {
                        GameManager_Custom.singleton.killLocalPlayer();
                    }
                    break;
                }
            case "kick":
                {
                    if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                    {
                        if (commandSplitted.Length > 1)
                        {
                            int parseResult;
                            if (int.TryParse(commandSplitted[1], out parseResult))
                            {
                                if (parseResult == -1)
                                {
                                    printText("local player can't get kicked");
                                }
                                else if (NetworkingManager.singleton.server_isClientConnected(parseResult))
                                {
                                    if (commandSplitted.Length > 2)
                                    {
                                        if (NetworkingManager.singleton.server_kickPlayer(parseResult, commandSplitted[2]))
                                        {
                                            printText("kicked player gameID \"" + parseResult + "\"");
                                        }
                                        else
                                        {
                                            printText("could'nt kick player gameID \"" + parseResult + "\"");
                                        }
                                    }
                                    else
                                    {
                                        if (NetworkingManager.singleton.server_kickPlayer(parseResult, "kicked by server"))
                                        {
                                            printText("kicked player gameID \"" + parseResult + "\"");
                                        }
                                        else
                                        {
                                            printText("could'nt kick player gameID \"" + parseResult + "\"");
                                        }
                                    }
                                }
                                else
                                {
                                    printText("player gameID \"" + parseResult + "\" not found");
                                }
                            }
                            else
                            {
                                printText("argument isnt of type int");
                            }
                        }
                    }
                    else
                    {
                        printText("only servers can use this command");
                    }
                    break;
                }
            case "ban":
                {
                    if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                    {
                        if (commandSplitted.Length > 1)
                        {
                            int parseResult;
                            if (int.TryParse(commandSplitted[1], out parseResult))
                            {
                                if (commandSplitted.Length > 2)
                                {
                                    if (NetworkingManager.singleton.server_bannPlayerByIP(parseResult, commandSplitted[2]))
                                    {
                                        printText("banned player gameID \"" + parseResult + "\" by IP");
                                    }
                                    else
                                    {
                                        printText("could'nt bann player gameID \"" + parseResult + "\"");
                                    }
                                }
                                else
                                {
                                    if (NetworkingManager.singleton.server_bannPlayerByIP(parseResult, "banned by server"))
                                    {
                                        printText("banned player gameID \"" + parseResult + "\" by IP");
                                    }
                                    else
                                    {
                                        printText("could'nt bann player gameID \"" + parseResult + "\"");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        printText("only servers can use this command");
                    }
                    break;
                }
            case "getposition":
                {
                    if (EntityManager.singleton.isLocalPlayerActive())
                    {
                        printText(EntityManager.singleton.getLocalPlayerPosition().ToString());
                    }
                    else
                    {
                        printText("No player object active !");
                    }
                    break;
                }
            case "teleport":
                {
                    if (commandSplitted.Length > 3)
                    {
                        if (EntityManager.singleton.isLocalPlayerActive())
                        {
                            float parseResult1 = 0;
                            float parseResult2 = 0;
                            float parseResult3 = 0;

                            bool parseSuccess = true;

                            parseSuccess = parseSuccess && float.TryParse(commandSplitted[1], out parseResult1);
                            parseSuccess = parseSuccess && float.TryParse(commandSplitted[2], out parseResult2);
                            parseSuccess = parseSuccess && float.TryParse(commandSplitted[3], out parseResult3);

                            if (parseSuccess)
                            {
                                EntityManager.singleton.getLocalPlayer().transform.position = new Vector3(parseResult1, parseResult2, parseResult3);
                            }
                            else
                            {
                                printText("command arguments must be of type float (x3)");
                            }
                        }
                        else
                        {
                            printText("No player object active !");
                        }
                    }
                    else if (commandSplitted.Length > 1)
                    {
                        List<Vector3> playerPostitions;
                        List<int> playerIDs;
                        List<float> clientViewDistances;

                        PlayerManager.singleton.getAllViewPointsPositions(out playerPostitions, out playerIDs, out clientViewDistances);

                        int intParse;

                        if (int.TryParse(commandSplitted[1], out intParse))
                        {
                            if (intParse > -1 && intParse < playerPostitions.Count)
                            {
                                EntityManager.singleton.getLocalPlayer().transform.position = playerPostitions[intParse];
                                printText("teleported to player " + intParse);
                            }
                        }
                    }
                    break;
                }
            case "spawnitem":
                {
                    if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                    {
                        if (EntityManager.singleton.isLocalPlayerActive())
                        {
                            if (commandSplitted.Length > 2)
                            {
                                int parseResultID;
                                int parseResultStackSize;

                                if (int.TryParse(commandSplitted[1], out parseResultID) && int.TryParse(commandSplitted[2], out parseResultStackSize))
                                {
                                    StorableItem item = ItemManager.singleton.createNewStorableItem(parseResultID);
                                    item.m_stackSize = parseResultStackSize;
                                    GameObject player = EntityManager.singleton.getLocalPlayer().gameObject;
                                    EntityManager.singleton.server_spawnDroppedItemWorldPlayer(item.WorldPrefabIndex, item, player.transform.position, player.transform.forward);

                                    printText("spawned item \"" + item.displayName + "\" x" + parseResultStackSize);
                                }
                                else
                                {
                                    printText("arguments invalid !");
                                }
                            }
                            else if (commandSplitted.Length > 1)
                            {
                                int parseResult;

                                if (int.TryParse(commandSplitted[1], out parseResult))
                                {
                                    StorableItem item = ItemManager.singleton.createNewStorableItem(parseResult);
                                    GameObject player = EntityManager.singleton.getLocalPlayer().gameObject;
                                    EntityManager.singleton.server_spawnDroppedItemWorldPlayer(item.WorldPrefabIndex, item, player.transform.position, player.transform.forward);

                                    printText("spawned item \"" + item.displayName + "\"");
                                }
                                else
                                {
                                    printText("argument must be a number !");
                                }
                            }
                        }
                        else
                        {
                            printText("No player object active !");
                        }
                    }
                    else
                    {
                        printText("only servers can use this command");
                    }
                    break;
                }
            case "checkentityuid":
                {
                    if (commandSplitted.Length > 1)
                    {
                        int parseResult;

                        if (int.TryParse(commandSplitted[1], out parseResult))
                        {
                            if (EntityManager.singleton.getEntity(parseResult) == null)
                            {
                                printText("Entity not found !");
                            }
                            else
                            {
                                printText("Entity found !");
                            }
                        }
                        else
                        {
                            printText("argument must be a number !");
                        }
                    }
                    else
                    {
                        printText("need at least 1 argument !");
                    }

                    break;
                }
            case "clearbuildingplayers":
                {
                    if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                    {
                        if (commandSplitted.Length > 1)
                        {
                            int parseResult;

                            if (int.TryParse(commandSplitted[1], out parseResult))
                            {
                                PlayerBuildingManager.singleton.clearAllowedPlayersBuilding(parseResult);
                            }
                            else
                            {
                                printText("argument must be a number !");
                            }
                        }
                    }
                    else
                    {
                        printText("only servers can use this command");
                    }
                    break;
                }
            case "countentity":
                {
                    if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                    {
                        if (commandSplitted.Length > 1)
                        {
                            int parseResult;

                            if (int.TryParse(commandSplitted[1], out parseResult))
                            {
                                printText(EntityManager.singleton.countEntites(parseResult).ToString());
                            }
                            else
                            {
                                printText("argument must be a number !");
                            }
                        }
                    }
                    else
                    {
                        printText("only servers can use this command");
                    }
                    break;
                }
            case "setcrosshair":
                {
                    if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                    {
                        if (commandSplitted.Length > 1)
                        {
                            int parseResult;

                            if (int.TryParse(commandSplitted[1], out parseResult))
                            {
                                if (parseResult == 0)
                                {
                                    GUIManager.singleton.setCrosshairVisibility(false);
                                    printText("crosshair is now invisible");
                                }
                                else
                                {
                                    GUIManager.singleton.setCrosshairVisibility(true);
                                    printText("crosshair is now visible");
                                }
                            }
                            else
                            {
                                printText("argument must be a number (0 = false, 1 = true)!");
                            }
                        }
                    }
                    else
                    {
                        printText("only servers can use this command");
                    }
                    break;
                }
            case "getplayers":
                {
                    if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                    {
                        List<Vector3> viewPointPostitions;
                        List<int> playerIDs;
                        List<float> clientViewDistances;

                        PlayerManager.singleton.getAllViewPointsPositions(out viewPointPostitions, out playerIDs, out clientViewDistances);

                        printText("there are " + playerIDs.Count + " client(s) connected");

                        for (int i = 0; i < playerIDs.Count; i++)
                        {
                            bool playerAlive = EntityManager.singleton.getActivePlayer(playerIDs[i]) != null;

                            string outMessage = "Player " + playerIDs[i] + ": Alive: " + playerAlive + ", Viewpoint Position: " + viewPointPostitions[i].ToString();

                            if (playerAlive)
                            {
                                outMessage += ", position: " + viewPointPostitions[i].ToString();
                            }

                            printText(outMessage);
                        }
                    }
                    else
                    {
                        printText("only servers can use this command");
                    }
                    break;
                }
            case "getspawnpoints":
                {
                    if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                    {
                        if (commandSplitted.Length > 1)
                        {
                            int parseResult;

                            if (int.TryParse(commandSplitted[1], out parseResult))
                            {
                                ClientUserData clientData = NetworkingManager.singleton.server_getClientUserData(parseResult);

                                if (clientData == null)
                                {
                                    printText("Player not found!");
                                }
                                else
                                {
                                    List<int> spawnpointIDs;
                                    List<float> cooldownTimes;
                                    List<Vector3> positions;

                                    EntityManager.singleton.getSpawnpoints(parseResult, out spawnpointIDs, out cooldownTimes, out positions);

                                    printText("there are " + spawnpointIDs.Count + " spawnpoints registered for this player :");

                                    for (int i = 0; i < spawnpointIDs.Count; i++)
                                    {
                                        printText("at " + positions[i].ToString() + "; cooldown: " + cooldownTimes[i] + ",");
                                    }
                                }
                            }
                            else
                            {
                                printText("argument must be a number !");
                            }
                        }
                    }
                    else
                    {
                        printText("only servers can use this command");
                    }
                    break;
                }
            case "savegame":
                {
                    GameManager_Custom.singleton.server_createGameSaveFile();
                    printText("created world save file");
                    break;
                }
            case "connectionsdiagnostic":
                {
                    if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                    {
                        List<string> diagnostics = NetworkingManager.singleton.server_connectionsDiagnostic();

                        printText("there are " + diagnostics.Count + " clients connected to this server: ");

                        for (int i = 0; i < diagnostics.Count; i++)
                        {
                            printText(diagnostics[i]);
                        }
                    }
                    else
                    {
                        printText("only servers can use this command");
                    }
                    break;
                }
            case "settime":
                {
                    if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                    {
                        if (commandSplitted.Length > 4)
                        {
                            int day;
                            int hour;
                            int minute;
                            int secound;

                            if (int.TryParse(commandSplitted[1], out day) && int.TryParse(commandSplitted[2], out hour) && int.TryParse(commandSplitted[3], out minute) && int.TryParse(commandSplitted[4], out secound))
                            {
                                EnvironmentManager.singleton.setTime(day, hour, minute, secound);
                            }
                            else
                            {
                                printText("arguments must be integers");
                            }
                        }
                        else if (commandSplitted.Length > 1)
                        {
                            int hour;

                            if (int.TryParse(commandSplitted[1], out hour))
                            {
                                EnvironmentManager.singleton.setTime(hour, 0, 0);
                            }
                            else
                            {
                                printText("argument must be integers");
                            }
                        }
                        else
                        {
                            printText("need 1 or 4 argument(s) (hour)/(day,hour,minute,secound)");
                        }
                    }
                    else
                    {
                        printText("only servers can use this command");
                    }
                    break;
                }
            case "settimescale":
                {
                    if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                    {
                        if (commandSplitted.Length > 0)
                        {
                            int scale;

                            if (int.TryParse(commandSplitted[1], out scale))
                            {
                                EnvironmentManager.singleton.timeScale = scale;
                            }
                            else
                            {
                                printText("arguments must be integers");
                            }
                        }
                        else
                        {
                            printText("need 1 argument");
                        }
                    }
                    else
                    {
                        printText("only servers can use this command");
                    }
                    break;
                }
            case "spawnlocalplayer":
                {
                    if (checkAdminRights())
                    {
                        Vector3 position;

                        VectorTools.Vector3TryParse(out position, arguments);

                        GameManager_Custom.singleton.DEBUG_spawnLocalPlayer(position);
                    }

                    break;
                }
            case "startdebugserver":
                {
                    int port;

                    if (arguments.Length > 0 && int.TryParse(arguments[0], out port))
                    {
                        GameManager_Custom.singleton.startAsDebugServerWithLocalClient(port);
                    }
                    else
                    {
                        GameManager_Custom.singleton.startAsDebugServerWithLocalClient(2302);
                    }

                    break;
                }
            case "npc_trainingbattle":
                {
                    if (checkAdminRights())
                    {
                        Vector3 position;

                        VectorTools.Vector3TryParse(out position, arguments);

                        NPCManager.singleton.DEBUG_startTrainingBattle(position);

                        printText("battle started");
                    }

                    break;
                }
            case "setworldviewpoint":
                {
                    if (checkAdminRights())
                    {
                        if (arguments.Length < 4)
                        {
                            printText("need id(1) position(3)");
                        }
                        else
                        {
                            int gameID;

                            if (int.TryParse(arguments[0], out gameID))
                            {
                                Vector3 position;

                                if (VectorTools.Vector3TryParse(out position, arguments))
                                {
                                    WorldViewPoint viewPoint = PlayerManager.singleton.getWorldViewPoint(gameID);

                                    viewPoint.transform.position = position;

                                    printText("viewpoint moved !");
                                }
                            }
                        }
                    }

                    break;
                }
            case "mastertexturelimit":
                {
                    int result;

                    if (int.TryParse(arguments[0], out result))
                    {
                        QualitySettings.masterTextureLimit = result;

                        printText("set masterTextureLimit to " + result);
                    }
                    else
                    {
                        printText("arguments must be integers");
                    }

                    break;
                }
            case "lodbias":
                {
                    if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                    {
                        if (commandSplitted.Length > 1)
                        {
                            float parseResult;

                            if (float.TryParse(commandSplitted[1], out parseResult))
                            {
                                QualitySettings.lodBias = parseResult;
                                printText("LODBias is now " + parseResult);
                            }
                            else
                            {
                                printText("argument must be a number !");
                            }
                        }
                    }
                    else
                    {
                        printText("only servers can use this command");
                    }
                    break;
                }
        }

    }

    private void printServerUseOnlyText()
    {
        printText("Only the server can use this command !");
    }

    private bool checkAdminRights()
    {
        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            return true;
        }
        else
        {
            printText("only admins can use this command");

            return false;
        }
    }

}
