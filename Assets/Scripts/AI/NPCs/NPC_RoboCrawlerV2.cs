using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MachineLearning;

public class NPC_RoboCrawlerV2 : NPCV2_Base
{
    [SerializeField] private float m_timeBetweenShots = 2f;

    private Vector3 m_lastDamagedPosition;
    private float m_lastTimeDamaged;
    private Entity_damageable m_shootTarget = null;
    private float m_lastTimeShot = 0f;
    private int m_hitEnemyCounter = 0;
    public List<string> m_alliesTags = new List<string>();

    protected void Awake()
    {
        base.Awake();
        m_aiCommunicationOutput = new float[3];
    }

    protected void Start()
    {
        base.Start();

        if (m_neuralNetwork == null)
        {
            m_neuralNetwork = getDefaultNeuralNetwork();
        }

        m_lastDamagedPosition = transform.position - Vector3.right * 700;
        m_lastTimeDamaged = Time.time - AI_TIME_DIVIDER / 2;

        Debug.DrawRay(transform.position, Vector3.up * 1000f, Color.red, 10f);
    }

    protected void Update()
    {
        base.Update();

        if (Time.time > m_lastTimeThink + m_minTimeBetweenThink)
        {
            if (!thinkInProgress)
            {
                setNeuralNetworkInputs();
                scheduleNeuralNetworkThink();
            }
        }

        if (m_health < 0)
        {
            Debug.Log("health < 0");
            Destroy(gameObject);
        }

        if (m_shootTarget != null)
        {
            if (Time.time > m_lastTimeShot + m_timeBetweenShots)
            {
                shootAt(m_shootTarget.transform.position + Vector3.up * m_shootTarget.getAimOffsetY(), 1);
                m_lastTimeShot = Time.time;
            }
        }
    }

    protected void OnDestroy()
    {
        m_fitness = m_hitEnemyCounter * 100 + m_health;

        Debug.Log("NPC_RoboCrawlerV2: OnDestroy: health: " + m_health);

        base.OnDestroy();
    }

    protected void setNeuralNetworkInputs()
    {
        float[] inputValues = new float[NeuralNetworkInputCount()];

        inputValues[0] = 0; // combat role
        inputValues[1] = m_health / m_maxHealth; // health
        inputValues[2] = Vector3.Distance(transform.position, m_lastDamagedPosition) / AI_DISTANCE_DIVIDER; // last damage distance
        inputValues[3] = (Time.time - m_lastTimeDamaged) / AI_TIME_DIVIDER; // last damage time
        inputValues[4] = 1; // high node
        inputValues[5] = 0; // low node

        int indexCounter = 6;

        /*

        float[] enemyData = m_battleTrainer.getEnemyData(this);

        for (int i = 0; i < enemyData.Length; i++)
        {
            inputValues[indexCounter] = enemyData[i];
            indexCounter++;
        }

        float[] allyData = m_battleTrainer.getAllyData(this);

        for (int i = 0; i < allyData.Length; i++)
        {
            inputValues[indexCounter] = allyData[i];
            indexCounter++;
        }
        */

        m_neuralNetwork.setInputValues(inputValues);
    }

    public override void onHitConfirmation()
    {
        base.onHitConfirmation();

        m_hitEnemyCounter++;
    }

    protected override void onNeuralNetworkThinkDone()
    {
        List<float> outputs = m_neuralNetwork.getOutput();

        float attackEnemy = outputs[0];
        float moveTowardEnemy = outputs[1];
        float enemyIndex = outputs[2];
        float moveToCover = outputs[3];
        float findNewCover = outputs[4];
        float moveTowardsAlly = outputs[5];
        float allyIndex = outputs[6];

        if (moveTowardEnemy > 0.5f)
        {
            setMoveDestination(MoveDestination.ToEnemy);
        }
        else if (moveTowardsAlly > 0.5f)
        {
            setMoveDestination(MoveDestination.Stand);
        }
        else
        {
            setMoveDestination(MoveDestination.Stand);
        }

        if (attackEnemy > 0.5f)
        {
            Entity_damageable target = null;

            if (m_enemiesInRange.Count > 0)
            {
                target = m_enemiesInRange[0];
            }

            if (target != null)
            {
                m_shootTarget = target;
            }
        }
        else
        {
            m_shootTarget = null;
        }

        for (int i = 0; i < m_aiCommunicationOutput.Length; i++)
        {
            m_aiCommunicationOutput[i] = outputs[9 + i];
        }
    }

    private static int NeuralNetworkInputCount()
    {
        int inputValuesCount = 0;
        inputValuesCount += 1; // tactical role
        inputValuesCount += 1; // my hp
        inputValuesCount += 1; // distance to point of last damage
        inputValuesCount += 1; // delta time since took damage
        inputValuesCount += 1; // high node
        inputValuesCount += 1; // Low node
        inputValuesCount += NPCBattleTrainer.AI_ENEMY_DATA_SIZE * NPCBattleTrainer.AI_MAX_ENEMIES; // 1 enemies
        inputValuesCount += NPCBattleTrainer.AI_ALLY_DATA_SIZE * NPCBattleTrainer.AI_MAX_ALLIES; // 10 allies

        return inputValuesCount;
    }

    private static int NeuralNetworkOutputCount()
    {
        int outputValuesCount = 0;

        outputValuesCount += 1; // attack lowest health enemy
        outputValuesCount += 1; // attack highest health enemy
        outputValuesCount += 1; // attack most dangerous enemy
        outputValuesCount += 1; // attack least dangerous enemy
        outputValuesCount += 1; // attack nearest enemy
        outputValuesCount += 1; // move towards enemy
        outputValuesCount += 1; // move towards nearest ally
        outputValuesCount += 1; // move to next cover
        outputValuesCount += 1; // finde new cover
        outputValuesCount += 3; // ally communication

        return outputValuesCount;
    }

    public static MachineLearning.NeuralNetwork getDefaultNeuralNetwork()
    {
        int deepCountX = 5;
        int deepSizeY = Mathf.Min(NeuralNetworkInputCount(), NeuralNetworkOutputCount());

        return getDefaultNeuralNetworkLayout(NeuralNetworkInputCount(), deepCountX, deepSizeY, NeuralNetworkOutputCount());
    }
}
