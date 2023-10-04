using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MachineLearning;

public class NPCManager : MonoBehaviour, System.IDisposable
{
    public static NPCManager singleton = null;

    [SerializeField] private int m_threadsForNeuralNetwork = 2; // +1 for internal distributer thread
    [SerializeField] private int m_battleManagerCount = 1;
    [SerializeField] private float m_timeBetweenPlayerAttacks = 600;
    [SerializeField] private GameObject m_NPCBattleManagerPrefab;
    [SerializeField] private bool DEBUG_createBattleFieldFirstPlayer = false;

    private List<NPCBattleManager> m_battleManagers = new List<NPCBattleManager>();
    private Dictionary<int, float> m_playerID_lastAttackTime = new Dictionary<int, float>();
    private Queue<NPCBattleManager> m_waitingToCreateBattlefield = new Queue<NPCBattleManager>();
    private Queue<NPCBattleManager> m_waitingToProbeBattlefield = new Queue<NPCBattleManager>();
    private NPCBattleManager m_currentlyCreatingBattlefield = null;
    private NPCBattleManager m_currentlyProbingBattlefield = null;
    private MultiThreadDistributor m_neuralNetworkWorker = null;

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else
        {
            Debug.LogError("NPCManager: Awake: spawned a singleton script multiple times");
        }

        m_neuralNetworkWorker = new MultiThreadDistributor(m_threadsForNeuralNetwork);

        for (int i = 0; i < m_battleManagerCount; i++)
        {
            GameObject battleManager = Instantiate(m_NPCBattleManagerPrefab);
            battleManager.transform.parent = transform;

            m_battleManagers.Add(battleManager.GetComponent<NPCBattleManager>());
        }
    }

    private void Update()
    {
        if(DEBUG_createBattleFieldFirstPlayer)
        {
            DEBUG_createBattleFieldFirstPlayer = false;

            List<Player_base> players = EntityManager.singleton.getAllActivePlayers();

            if(players.Count > 0)
            {
                m_battleManagers[0].attackPlayer(players[0]);
            }
        }
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < m_battleManagers.Count; i++)
        {
            if (!m_battleManagers[i].active)
            {
                Player_base player = getNextPlayerToAttack();

                if (player != null)
                {
                    m_battleManagers[i].attackPlayer(player);
                    setPlayerAttackTime(player.m_gameID);
                }
            }
        }
    }

    private void OnDestroy()
    {
        Dispose();
    }

    public void DEBUG_startTrainingBattle(Vector3 position)
    {
        m_battleManagers[0].startTrainingBattle(position);
    }

    private Player_base getNextPlayerToAttack()
    {
        List<Player_base> players = EntityManager.singleton.getAllActivePlayers();

        float lastTime;

        for (int i = 0; i < players.Count; i++)
        {
            if (m_playerID_lastAttackTime.TryGetValue(players[i].m_gameID, out lastTime))
            {
                if (Time.time > lastTime + m_timeBetweenPlayerAttacks)
                {
                    return players[i];
                }
            }
            else // player was never attacked
            {
                return players[i];
            }
        }

        return null;
    }

    private void setPlayerAttackTime(int playerGameID)
    {
        if (m_playerID_lastAttackTime.ContainsKey(playerGameID))
        {
            m_playerID_lastAttackTime[playerGameID] = Time.time;
        }
        else
        {
            m_playerID_lastAttackTime.Add(playerGameID, Time.time);
        }
    }

    public void createBattlefieldRequest(NPCBattleManager sender)
    {
        if (m_currentlyCreatingBattlefield == null)
        {
            m_currentlyCreatingBattlefield = sender;
            sender.startCreateBattlefield();
        }
        else
        {
            m_waitingToCreateBattlefield.Enqueue(sender);
        }
    }

    public void onCreateBattlefieldDone(NPCBattleManager sender)
    {
        if (m_currentlyCreatingBattlefield == sender)
        {
            if (m_waitingToCreateBattlefield.Count > 0)
            {
                NPCBattleManager waiting = m_waitingToCreateBattlefield.Dequeue();
                m_currentlyCreatingBattlefield = waiting;
                waiting.startCreateBattlefield();
            }
            else
            {
                m_currentlyCreatingBattlefield = null;
            }
        }
        else
        {
            Debug.LogError("NPCManager: onCreateBattlefieldDone: wrong NPCBattleManager responded with done. there is only one NPCBattleManager allowed to create a battlefield at one time");
        }
    }

    public void scheduleNeuralNetworkThink(MachineLearning.NeuralNetwork neuralNetwork)
    {
        m_neuralNetworkWorker.enqueueNeuralNetworkCompute(neuralNetwork);
    }

    public void Dispose()
    {
        m_neuralNetworkWorker.Dispose();
    }
}
