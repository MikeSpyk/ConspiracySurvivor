using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MachineLearning;

public class NPCBattleTrainer : MonoBehaviour
{
    public static readonly int AI_ENEMY_DATA_SIZE = 6; // isPresent(1), distance (1), health (1), danger (1), has line of sight (1), is aiming at me (1)
    public static readonly int AI_ALLY_DATA_SIZE = 7; // isPresent(1), distance (1), health (1), has line of sight (1), communication (3)
    public static readonly int AI_MAX_ENEMIES = 1;
    public static readonly int AI_MAX_ALLIES = 10;

    [Header("Evolutionary Algorithm")]
    [SerializeField] private int m_generationSizeGeneral = 100;
    [SerializeField] private int m_generationSizeSoldier = 100;
    [SerializeField, Range(0, 100)] private float m_evolutionKeep = 90; // how many specimen from the last generation will repopulate (and how many will die out)
    [SerializeField, Range(0, 100)] private float m_mutationProbability = 2;
    [SerializeField, Range(0, 100)] private float m_mutationWeight = 50;
    [Header("NPCs")]
    [SerializeField] private GameObject m_NPCPrefab;
    [SerializeField] private int m_NPCsPerBattle = 10;
    [SerializeField] private float m_maxBattleTime = 300;

    private GeneticAlgorithm m_generalGeneticAlgorithm;
    private GeneticAlgorithm m_soldierGeneticAlgorithm;
    private Vector3 m_battleOrigin = Vector3.zero;
    private float m_battleFieldSize = 0;
    private List<MachineLearning.NeuralNetwork> m_currentSoldierGenerationPool = null;
    private int m_currentSoldierGenerationIndex = 0;
    private bool m_currentSoldierGenerationClone = false;
    private int m_currentSoldierGenerationReturnCount = 0;
    private List<NPCV2_Base> m_currentlyActiveNPCS = new List<NPCV2_Base>();
    private List<Player_base> m_playersInRange = new List<Player_base>();
    private float m_lastBattleStartTime = 0;
    private NPCBattleManager m_battleManager = null;

    private void Awake()
    {
        m_generalGeneticAlgorithm = new GeneticAlgorithm(m_generationSizeGeneral, m_evolutionKeep, RepopulationMethod.CrossoverMutate, m_mutationProbability, m_mutationWeight);
        m_soldierGeneticAlgorithm = new GeneticAlgorithm(m_generationSizeSoldier, m_evolutionKeep, RepopulationMethod.CrossoverMutate, m_mutationProbability, m_mutationWeight);
    }

    private void Start()
    {
        Debug.Log("TODO Mike: save and load NNs from disk");

        m_soldierGeneticAlgorithm.addMember(NPC_RoboCrawlerV2.getDefaultNeuralNetwork(), 0f);
        createNextSoilderGeneration();
    }

    private void Update()
    {
        if (Time.time > m_lastBattleStartTime + m_maxBattleTime)
        {
            if (m_currentlyActiveNPCS.Count > 0)
            {
                cancelCurrentBattle();
            }
        }
    }

    private void FixedUpdate()
    {
        updateBattleParticipants();
    }

    private void updateBattleParticipants()
    {
        m_playersInRange.Clear();

        List<Player_base> players = EntityManager.singleton.getAllActivePlayers();

        for (int i = 0; i < players.Count; i++)
        {
            if (Vector3.Distance(players[i].transform.position, m_battleOrigin) < m_battleFieldSize)
            {
                m_playersInRange.Add(players[i]);
            }
        }
    }

    private void createNextSoilderGeneration()
    {
        System.DateTime startTime = System.DateTime.Now;
        m_currentSoldierGenerationPool = m_soldierGeneticAlgorithm.createNextGeneration();

        Debug.Log("next generation took " + (System.DateTime.Now - startTime).TotalSeconds);

        m_currentSoldierGenerationIndex = 0;
        m_currentSoldierGenerationClone = false;
        m_currentSoldierGenerationReturnCount = 0;
    }

    public void setBattleManager(NPCBattleManager battleManager)
    {
        m_battleManager = battleManager;
    }

    public void cancelCurrentBattle()
    {
        Debug.Log("cancelCurrentBattle");

        bool sendBattleEnded = false;

        for (int i = 0; i < m_currentlyActiveNPCS.Count; i++)
        {
            if (m_currentlyActiveNPCS[i] != null)
            {
                Destroy(m_currentlyActiveNPCS[i].gameObject);
                sendBattleEnded = true;
            }
        }

        m_currentlyActiveNPCS.Clear();

        if (sendBattleEnded)
        {
            m_battleManager.onBattleEnded();
        }
    }

    public void setBattlefieldSize(float size)
    {
        m_battleFieldSize = size;
    }

    public void startBattle(Vector3 position)
    {
        cancelCurrentBattle();

        m_battleOrigin = position;

        for (int i = 0; i < m_NPCsPerBattle; i++)
        {
            NPC_RoboCrawlerV2 npc = spawnSoliderNPC(new Vector2(m_battleOrigin.x + Random.value * m_battleFieldSize - m_battleFieldSize / 2, m_battleOrigin.z + Random.value * m_battleFieldSize - m_battleFieldSize / 2));

            if (npc != null)
            {
                m_currentlyActiveNPCS.Add(npc);
            }
        }

        m_lastBattleStartTime = Time.time;
    }

    public void startTrainingBattle(Vector3 position)
    {
        cancelCurrentBattle();

        m_battleOrigin = position;

        List<Entity_damageable> team1 = new List<Entity_damageable>();

        for (int i = 0; i < m_NPCsPerBattle; i++)
        {
            NPC_RoboCrawlerV2 npc = spawnSoliderNPC(new Vector2(m_battleOrigin.x + (Random.value - 0.5f) * m_battleFieldSize, m_battleOrigin.z + Random.value * 0.5f * m_battleFieldSize), "NPC_Team1");

            if (npc != null)
            {
                m_currentlyActiveNPCS.Add(npc);
                team1.Add(npc);
            }
        }

        List<Entity_damageable> team2 = new List<Entity_damageable>();

        for (int i = 0; i < m_NPCsPerBattle; i++)
        {
            NPC_RoboCrawlerV2 npc = spawnSoliderNPC(new Vector2(m_battleOrigin.x + (Random.value  -0.5f) * m_battleFieldSize, m_battleOrigin.z + Random.value * -0.5f * m_battleFieldSize), "NPC_Team2");

            if (npc != null)
            {
                m_currentlyActiveNPCS.Add(npc);
                team2.Add(npc);
            }
        }

        for (int i = 0; i < m_currentlyActiveNPCS.Count; i++)
        {
            if (m_currentlyActiveNPCS[i].m_team == "NPC_Team1")
            {
                m_currentlyActiveNPCS[i].setAlliesInRange(team1);
                m_currentlyActiveNPCS[i].setEnemiesInRange(team2);
            }
            else // "NPC_Team2"
            {
                m_currentlyActiveNPCS[i].setAlliesInRange(team2);
                m_currentlyActiveNPCS[i].setEnemiesInRange(team1);
            }
        }

        m_lastBattleStartTime = Time.time;
    }

    public NPC_RoboCrawlerV2 spawnSoliderNPC(Vector2 position, string team = "DEFAULT_NPC")
    {
        RaycastHit hit;

        Debug.Log("TODO Mike: fallback if position is invalid");

        if (Physics.Raycast(new Vector3(position.x, 1000, position.y), Vector3.down, out hit))
        {
            GameObject npc = Instantiate(m_NPCPrefab, hit.point + Vector3.up * 0.5f, Quaternion.identity);
            NPC_RoboCrawlerV2 npcScript = npc.GetComponent<NPC_RoboCrawlerV2>();

            if (m_currentSoldierGenerationIndex >= m_currentSoldierGenerationPool.Count)
            {
                m_currentSoldierGenerationIndex = 0;
                m_currentSoldierGenerationClone = true;
            }

            MachineLearning.NeuralNetwork npcNeuralNetwork;

            if (m_currentSoldierGenerationClone)
            {
                npcNeuralNetwork = m_currentSoldierGenerationPool[m_currentSoldierGenerationIndex].createClone();
            }
            else
            {
                npcNeuralNetwork = m_currentSoldierGenerationPool[m_currentSoldierGenerationIndex];
            }

            npcScript.m_neuralNetwork = npcNeuralNetwork;
            npcScript.setNoDamageGroups(team);
            npcScript.m_team = team;
            npcScript.setBattleTrainer(this);

            m_currentSoldierGenerationIndex++;

            return npcScript;
        }
        else
        {
            Debug.LogWarning("no hit");

            return null;
        }
    }

    public void onSoliderDestroyed(NPCV2_Base npc, float fitness)
    {
        if (m_currentSoldierGenerationReturnCount >= m_soldierGeneticAlgorithm.m_desiredMemberCount)
        {
            createNextSoilderGeneration();
        }

        m_soldierGeneticAlgorithm.addMember(npc.m_neuralNetwork, fitness);
        m_currentSoldierGenerationReturnCount++;

        m_currentlyActiveNPCS.Remove(npc);

        if (m_currentlyActiveNPCS.Count == 0)
        {
            cancelCurrentBattle();
        }
    }

    private float[] getAllyData(NPCV2_Base specimen)
    {
        throw new System.NotSupportedException();

        float[] returnValue = new float[AI_MAX_ALLIES * AI_ALLY_DATA_SIZE];

        for (int i = 0; i < m_currentlyActiveNPCS.Count && i < AI_MAX_ALLIES; i++)
        {
            float toNpcDistance = Vector3.Distance(m_currentlyActiveNPCS[i].transform.position, specimen.transform.position);

            returnValue[i * AI_ALLY_DATA_SIZE] = 1; // is present
            returnValue[i * AI_ALLY_DATA_SIZE + 1] = toNpcDistance / NPCV2_Base.AI_DISTANCE_DIVIDER; // distance
            returnValue[i * AI_ALLY_DATA_SIZE + 2] = m_currentlyActiveNPCS[i].health / m_currentlyActiveNPCS[i].maxHealth; // health

            RaycastHit hit;

            if (Physics.Raycast(specimen.transform.position, (specimen.transform.position - m_currentlyActiveNPCS[i].transform.position).normalized, out hit, toNpcDistance))
            {
                NPCV2_Base hitNpc = hit.transform.GetComponent<NPCV2_Base>();

                if (hitNpc == null)
                {
                    returnValue[i * AI_ALLY_DATA_SIZE + 3] = 0f; // has line of sight
                }
                else
                {
                    if (hitNpc == m_currentlyActiveNPCS[i])
                    {
                        returnValue[i * AI_ALLY_DATA_SIZE + 3] = 1f;// has line of sight
                    }
                    else
                    {
                        returnValue[i * AI_ALLY_DATA_SIZE + 3] = 0f;// has line of sight
                    }
                }
            }
            else
            {
                returnValue[i * AI_ALLY_DATA_SIZE + 3] = 1f;// has line of sight
            }

            for (int j = 0; j < 3; j++)
            {
                returnValue[i * AI_ALLY_DATA_SIZE + 4 + j] = m_currentlyActiveNPCS[i].aiCommunicationOutput[j];
            }
        }

        return returnValue;
    }

}
