using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MachineLearning;

public class NPCV2_Base : Entity_damageable
{
    public static readonly int AI_TIME_DIVIDER = 1800; // max life time of a npc
    public static readonly int AI_DISTANCE_DIVIDER = 10000; // max distance the npc can think within (at least world size)

    protected enum MoveDestination { Stand, ToEnemy }

    [Header("NPCV2_Base")]
    [SerializeField] protected float m_maxHealth = 100;
    [SerializeField] protected float m_minTimeBetweenThink = 1f;
    [SerializeField] private string[] m_noDamageGroups = null;
    [SerializeField] private Vector3 m_muzzleOffset = Vector3.zero;

    protected NavMeshAgent m_navMeshAgent = null;
    protected Animator m_animator = null;
    public MachineLearning.NeuralNetwork m_neuralNetwork = null;
    public string m_team = "";
    private bool m_thinkInProgress = false;
    protected NPCBattleTrainer m_battleTrainer = null;
    protected float m_fitness = 0;
    protected float m_lastTimeThink = 0;
    protected MoveDestination m_moveDestination = MoveDestination.Stand;
    private float m_lastTimeThinkRealTime = 0;
    private int m_gameID = -1;
    protected float[] m_aiCommunicationOutput = null;
    protected List<Entity_damageable> m_enemiesInRange = new List<Entity_damageable>();
    protected List<Entity_damageable> m_alliesInRange = new List<Entity_damageable>();

    protected bool thinkInProgress { get { return m_thinkInProgress; } }

    public float health { get { return m_health; } }
    public float maxHealth { get { return m_maxHealth; } }
    public float[] aiCommunicationOutput { get { return m_aiCommunicationOutput; } }

    protected void Awake()
    {
        base.Awake();

        m_navMeshAgent = GetComponent<NavMeshAgent>();
        m_animator = GetComponent<Animator>();

        m_health = m_maxHealth;
    }

    protected void Start()
    {
        base.Start();

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            m_gameID = GameManager_Custom.singleton.server_registerNewNPC(gameObject, this);
            initializeChildrenHitboxes(transform);
        }
    }

    protected void Update()
    {
        if (m_neuralNetwork != null)
        {
            if (m_thinkInProgress && m_neuralNetwork.computeCompleted)
            {
                m_thinkInProgress = false;
                //Debug.Log("NPCV2_Base: "+ gameObject.GetHashCode()+": think took " + (Time.realtimeSinceStartup - m_lastTimeThinkRealTime));
                onNeuralNetworkThinkDone();
            }
        }
    }

    protected void OnDestroy()
    {
        if (m_neuralNetwork != null && m_battleTrainer != null)
        {
            m_battleTrainer.onSoliderDestroyed(this, m_fitness);
        }

        base.OnDestroy();
    }

    protected void scheduleNeuralNetworkThink()
    {
        if (m_thinkInProgress)
        {
            throw new System.NotSupportedException("NPCV2_Base: scheduleNeuralNetworkThink: can't start a think process while another one is still running");
        }

        NPCManager.singleton.scheduleNeuralNetworkThink(m_neuralNetwork);
        m_thinkInProgress = true;
        m_lastTimeThink = Time.time;
        m_lastTimeThinkRealTime = Time.realtimeSinceStartup;
    }

    protected virtual void onNeuralNetworkThinkDone() { }

    protected void shootAt(Vector3 target, int weaponIndex)
    {
        Vector3? direction = ProjectileManager.singleton.getShotDirectionForTarget(weaponIndex, getMuzzlePosition(), target);

        if (direction == null)
        {
            Debug.LogError("NPCV2_Base: shootAt: no shot angle found");
            return;
        }

        /*
        direction = Quaternion.Euler(
                                        RandomValuesSeed.perlinNoiseRanged(-m_shootInaccuracyDegree, m_shootInaccuracyDegree, Time.realtimeSinceStartup, 1.2f),
                                        RandomValuesSeed.perlinNoiseRanged(-m_shootInaccuracyDegree, m_shootInaccuracyDegree, Time.realtimeSinceStartup, 2.2f),
                                        RandomValuesSeed.perlinNoiseRanged(-m_shootInaccuracyDegree, m_shootInaccuracyDegree, Time.realtimeSinceStartup, 3.2f)
                                    ) * direction;
                                    */

        direction = Quaternion.Euler(
                                RandomValuesSeed.perlinNoiseRanged(-0, 0, Time.realtimeSinceStartup, 1.2f),
                                RandomValuesSeed.perlinNoiseRanged(-0, 0, Time.realtimeSinceStartup, 2.2f),
                                RandomValuesSeed.perlinNoiseRanged(-0, 0, Time.realtimeSinceStartup, 3.2f)
                            ) * direction;

        ProjectileManager.singleton.server_addGunshot(getMuzzlePosition(), ((Vector3)direction).normalized, weaponIndex, m_gameID, m_noDamageGroups);
    }

    private Vector3 getMuzzlePosition()
    {
        return transform.position + transform.forward * m_muzzleOffset.z + transform.right * m_muzzleOffset.x + transform.up * m_muzzleOffset.y;
    }

    public virtual void onHitConfirmation()
    {
        // NPC successfully hit its opponent
        Debug.Log("NPCV2_Base: onHitConfirmation");
    }

    public void setBattleTrainer(NPCBattleTrainer battleTrainer)
    {
        m_battleTrainer = battleTrainer;
    }

    public void setNoDamageGroups(params string[] groups)
    {
        m_noDamageGroups = groups;
    }

    public void setEnemiesInRange(List<Entity_damageable> enemies)
    {
        m_enemiesInRange.Clear();
        m_enemiesInRange.AddRange(enemies);
    }

    public void setAlliesInRange(List<Entity_damageable> allies)
    {
        m_alliesInRange.Clear();
        m_alliesInRange.AddRange(allies);
    }

    protected void setMoveDestination(MoveDestination destination)
    {
        if (destination == m_moveDestination)
        {
            return;
        }

        switch (destination)
        {
            case MoveDestination.Stand:
                {
                    m_navMeshAgent.destination = transform.position;

                    m_animator.SetBool("moving", false);

                    break;
                }
            case MoveDestination.ToEnemy:
                {
                    int closestIndex = -1;
                    float closestsDistance = float.MaxValue;

                    for (int i = 0; i < m_enemiesInRange.Count; i++)
                    {
                        float distance = Vector3.Distance(transform.position, m_enemiesInRange[i].transform.position);

                        if (distance < closestsDistance)
                        {
                            closestsDistance = distance;
                            closestIndex = i;
                        }
                    }

                    if (closestIndex != -1)
                    {
                        m_navMeshAgent.destination = m_enemiesInRange[closestIndex].transform.position;
                    }

                    m_animator.SetBool("moving", true);

                    break;
                }
            default:
                {
                    throw new System.NotImplementedException();
                }
        }

        m_moveDestination = destination;
    }

    protected override void initializeChildrenHitboxes(Transform parent)
    {
        EntityHitbox hitbox = parent.GetComponent<EntityHitbox>();

        if (hitbox != null)
        {
            hitbox.setParentEntity(this);
            hitbox.m_gameID = m_gameID;
        }

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            initializeChildrenHitboxes(parent.transform.GetChild(i));
        }
    }

    protected static MachineLearning.NeuralNetwork getDefaultNeuralNetworkLayout(int inputCount, int deepXCount, int deepYCount, int outputCount)
    {
        System.Random rand = new System.Random();

        List<InputNeuron> inputNeurons = new List<InputNeuron>();

        for (int i = 0; i < inputCount; i++)
        {
            InputNeuron inputNeuron = new InputNeuron();
            if (i == 0)
            {
                inputNeuron.setValue((float)rand.NextDouble());
            }
            else if (i == 1)
            {
                inputNeuron.setValue(1);
            }
            else
            {
                inputNeuron.setValue(0);
            }
            inputNeurons.Add(inputNeuron);
        }

        Neuron[,] deepNeurons = new Neuron[deepXCount, deepYCount];

        for (int i = 0; i < deepXCount; i++)
        {
            for (int j = 0; j < deepYCount; j++)
            {
                deepNeurons[i, j] = new Neuron();
            }
        }

        for (int i = 0; i < deepYCount; i++)
        {
            for (int j = 0; j < inputNeurons.Count; j++)
            {
                deepNeurons[0, i].addInputConnection(inputNeurons[j]);
                inputNeurons[j].addOutputConnection(deepNeurons[0, i], (float)rand.NextDouble() * 2 - 1f);
            }
        }

        for (int i = 1; i < deepXCount; i++)
        {
            for (int j = 0; j < deepYCount; j++)
            {
                for (int k = 0; k < deepYCount; k++)
                {
                    deepNeurons[i, j].addInputConnection(deepNeurons[i - 1, k]);
                    deepNeurons[i - 1, k].addOutputConnection(deepNeurons[i, j], (float)rand.NextDouble() * 2 - 1f);
                }
            }
        }

        Neuron[] outputNeurons = new Neuron[outputCount];

        for (int i = 0; i < outputCount; i++)
        {
            outputNeurons[i] = new Neuron();

            for (int j = 0; j < deepYCount; j++)
            {
                deepNeurons[deepXCount - 1, j].addOutputConnection(outputNeurons[i], (float)rand.NextDouble() * 2 - 1f);
                outputNeurons[i].addInputConnection(deepNeurons[deepXCount - 1, j]);
            }
        }

        MachineLearning.NeuralNetwork network = new MachineLearning.NeuralNetwork();

        network.addInputNeurons(inputNeurons.ToArray());

        for (int i = 0; i < deepXCount; i++)
        {
            for (int j = 0; j < deepYCount; j++)
            {
                network.addNeurons(i + 1, deepNeurons[i, j]);
            }
        }

        network.addNeurons(deepXCount + 1, outputNeurons);

        return network;
    }

}
