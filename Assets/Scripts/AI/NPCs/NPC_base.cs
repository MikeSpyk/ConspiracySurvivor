using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPC_base : Entity_damageable
{
    public enum MovementMode { StandStill, MoveToClosestCover, MoveRandomly, MoveToClosestEnemy }
    public enum AttackMode { Peaceful, Hostile }

    [Header("Configuration NPC_base")] 
    [SerializeField] protected float m_maxTimeRandomDestination = 1;
    [SerializeField] protected float m_maxDistanceRandomDestination = 5;
    [SerializeField] private Vector3 m_muzzleOffset = Vector3.zero;
    [SerializeField] protected float m_maxHealth = 100;
    [SerializeField] protected float m_maxEnergy = 100;
    [SerializeField] private float m_energyRecoverSpeed = 1;
    [SerializeField] protected float m_maxSpeed = 1;
    [SerializeField] private float m_moveCost = 1;
    [SerializeField] private float m_NNThinkTime = 0.5f; // time between NN updates
    [SerializeField] private float m_shootInaccuracyDegree = 2;
    [SerializeField] private string[] m_noDamageGroups = null;
    [SerializeField] private float m_coverRadius = 3; // how much space is needed in cover location
    [SerializeField] private float m_coverHeight = 2; // how high must a cover be
    [SerializeField] protected bool m_keepUpdatingCoverPositions = false;
    [SerializeField] private float m_updateCoverTime = 1;
    [SerializeField] private float m_shotCooldown = 1;
    [SerializeField] private float m_attackCost = 10;

    [Header("Variables NPC_base")]
    [SerializeField] private int m_gameID = -1;
    [SerializeField] protected float m_timeRandomDestination = 1;
    [SerializeField] protected float m_distanceRandomDestination = 1;
    [SerializeField] protected float m_energy = 1;
    [SerializeField] protected float m_NNFitness = 0;
    [SerializeField] private MovementMode m_currentMovementMode = MovementMode.StandStill;
    [SerializeField] private AttackMode m_currentAttackMode = AttackMode.Peaceful;

    private Vector3 m_positionLastUpdate = Vector3.zero;
    private float m_lastTimeShot = 0;
    private float m_lastTimeRandomDestination = 0;
    private float m_lastTimeNNThought = 0;
    private Entity_damageable m_currentTargetScript = null;

    protected NavMeshAgent m_navMeshAgent = null;
    protected List<GameObject> m_enemiesObjects = null;
    protected List<Entity_damageable> m_enemiesScripts = null;
    private Animator m_animator = null;
    private NeuralNetwork m_neuronNetwork = null;
    protected bool m_isNeuronalNetworkInitialized = false;
    protected float[] m_NNInputs = null;
    protected float[] m_NNOutputs = null;
    protected int m_NNInputsCount = 0;
    protected int m_NNNeuronsCountX = 0;
    protected int m_NNNeuronsCountY = 0;
    protected int m_NNOutputsCount = 0;

    private List<Vector3> m_coverSpots = null;
    private Vector3 m_closestCoverSpot = Vector3.zero;
    private float m_lastTimeUpdateCover = 0;

    public float coverRadius
    {
        get
        {
            return m_coverRadius;
        }
    }

    public float coverHeight
    {
        get
        {
            return m_coverHeight;
        }
    }

    public float spawnHeight
    {
        get
        {
            if (m_navMeshAgent == null)
            {
                m_navMeshAgent = GetComponent<NavMeshAgent>();
                if (m_navMeshAgent == null)
                {
                    return 0;
                }
                else
                {
                    return m_navMeshAgent.baseOffset;
                }
            }
            else
            {
                return m_navMeshAgent.baseOffset;
            }
        }
    }

    virtual protected void Awake()
    {
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        m_animator = GetComponent<Animator>();
    }

    virtual protected void Start()
    {
        Entity_damageable_Start();

        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            //m_gameID = GameManager_Custom.singleton.server_registerNewNPC(gameObject, this);
        }
        m_positionLastUpdate = transform.position;
        m_health = m_maxHealth;
        m_energy = m_maxEnergy;
        m_NNFitness = 0;
        if(getGenome() != null)
        {
            getGenome().m_fitness = 0;
        }

        initializeChildrenHitboxes(transform);
    }

    virtual protected void Update()
    {
        m_energy += m_energyRecoverSpeed * Time.deltaTime;

        if(m_energy > m_maxEnergy)
        {
            m_energy = m_maxEnergy;
        }

        if(m_currentAttackMode == AttackMode.Hostile)
        {
            if(m_currentTargetScript == null)
            {
                int closestIndex = getClosestVisibleEnemyIndex();
                if (closestIndex < 0)
                {
                    m_currentTargetScript = null;
                }
                else
                {
                    m_currentTargetScript = m_enemiesScripts[closestIndex];
                }
            }
            else
            {
                // check if still visible
                if(!checkIfEnemyIsVisible(m_currentTargetScript.getTransform(), m_currentTargetScript))
                {
                    int closestIndex = getClosestVisibleEnemyIndex();
                    if (closestIndex < 0)
                    {
                        m_currentTargetScript = null;
                    }
                    else
                    {
                        m_currentTargetScript = m_enemiesScripts[closestIndex];
                    }
                }
            }

            if(m_currentTargetScript != null)
            {
                if (m_energy > m_attackCost)
                {
                    if (Time.time > m_lastTimeShot + m_shotCooldown)
                    {
                        m_lastTimeShot = Time.time;
                        shootAt(m_currentTargetScript.getTargetPosition(), 1);
                        m_energy -= m_attackCost;
                    }
                }
            }
        }

        if (m_currentMovementMode == MovementMode.MoveRandomly)
        {
            if (Time.time > m_lastTimeRandomDestination + m_timeRandomDestination)
            {
                m_lastTimeRandomDestination = Time.time;
                goToRandomPosition();
            }
        }

        if (m_currentMovementMode != MovementMode.StandStill)
        {
            if(m_navMeshAgent.speed <= 0)
            {
                stopMoving();
            }

            float movedDistance = Vector3.Distance(m_positionLastUpdate, transform.position);
            m_energy -= m_moveCost * movedDistance * Time.deltaTime;
            m_positionLastUpdate = transform.position;

            if (m_energy < 0)
            {
                stopMoving();
            }

            if (m_navMeshAgent != null && m_navMeshAgent.destination != null)
            {
                if (Vector3.Distance(m_navMeshAgent.destination, transform.position) <= m_navMeshAgent.stoppingDistance)
                {
                    stopMoving();
                }
            }
        }

        if(m_keepUpdatingCoverPositions)
        {
            if(Time.time > m_lastTimeUpdateCover + m_updateCoverTime)
            {
                m_lastTimeUpdateCover = Time.time;
                updateClosestsCoverSpot();
            }
        }

        if(Time.time > m_lastTimeNNThought + m_NNThinkTime)
        {
            m_lastTimeNNThought = Time.time;
            think();
        }

    }

    protected override void onDamaged(float damage)
    {
        base.onDamaged(damage);

        if(m_health < 0)
        {
            Destroy(gameObject);
        }
    }

    public virtual void onHitConfirmation()
    {
        // NPC successfully hit its opponent
        //Debug.Log("NPC_base: onHitConfirmation");
    }

    protected void initializeNeuronalNetwork()
    {
        m_neuronNetwork = new NeuralNetwork();
        m_NNInputs = new float[m_NNInputsCount];
        m_neuronNetwork.setInputsCount(m_NNInputsCount);
        m_neuronNetwork.setNeuronsCount(m_NNNeuronsCountX, m_NNNeuronsCountY);
        m_neuronNetwork.setOutputsCount(m_NNOutputsCount);
        m_isNeuronalNetworkInitialized = true;
    }
    public void initializeNeuronalNetwork(Genome genome)
    {
        m_neuronNetwork = new NeuralNetwork();
        m_NNInputs = new float[genome.m_inputsWeights.GetLength(0)];
        m_neuronNetwork.setInputsCount(m_NNInputsCount);
        m_neuronNetwork.setNeuronsCount(m_NNNeuronsCountX, m_NNNeuronsCountY);
        m_neuronNetwork.setOutputsCount(m_NNOutputsCount);
        m_neuronNetwork.setInputWeights(genome.m_inputsWeights);
        m_neuronNetwork.setNeuronWeights(genome.m_neuronsWeights);
        m_neuronNetwork.setOutputWeights(genome.m_outputsWeights);
        m_isNeuronalNetworkInitialized = true;
    }

    protected void setNNGenomeWeightsRandom()
    {
        if (m_neuronNetwork != null)
        {
            m_neuronNetwork.setAllWeightsRandom();
        }
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

    protected virtual void onUpdateNNInputs()
    {
        // overide this
        // cant use abstract in monobehaviour
    }

    protected virtual void onNNDone()
    {
        // overide this
        // cant use abstract in monobehaviour
    }

    private void think()
    {
        if (m_neuronNetwork == null)
        {
            Debug.LogError("NPC_base: neuronal network = null");
        }
        else if (m_NNInputs == null)
        {
            Debug.LogError("NPC_base: m_NNInputs = null");
        }
        else
        {
            onUpdateNNInputs();
            m_NNOutputs = m_neuronNetwork.think(m_NNInputs);
            onNNDone();
        }
    }

    public Genome getGenome()
    {
        if(m_neuronNetwork == null)
        {
            return null;
        }
        else
        {
            return m_neuronNetwork.getGenomeRef();
        }
    }

    protected void goToRandomPosition()
    {
        Vector3 randomPos = transform.position;

        RaycastHit[] hit = Physics.RaycastAll(transform.position + new Vector3(Random.Range(-m_distanceRandomDestination, m_distanceRandomDestination), 100, Random.Range(-m_distanceRandomDestination, m_distanceRandomDestination)), Vector3.down);

        for (int i = 0; i < hit.Length; i++)
        {
            if (hit[i].collider.gameObject.layer == 10)
            {
                randomPos = hit[i].point;
                break;
            }
        }

        startMovingTo(randomPos);
    }

    protected void startMovingTo(Vector3 position)
    {
        if(m_navMeshAgent == null)
        {
            Debug.LogWarning("NPC_base: m_navMeshAgent = null");
        }
        else
        {
            m_animator.SetBool("moving", true);
            m_navMeshAgent.SetDestination(position);
        }
    }

    protected void stopMoving()
    {
        m_animator.SetBool("moving", false);
        m_navMeshAgent.destination = transform.position;
        m_currentMovementMode = MovementMode.StandStill;
    }

    public void setEnemiesInRange(List<GameObject> enemiesObjects, List<Entity_damageable> enemiesScripts)
    {
        m_enemiesObjects = enemiesObjects;
        m_enemiesScripts = enemiesScripts;
    }

    public void setCoverSpots(List<Vector3> spots)
    {
        m_coverSpots = spots;
    }

    protected void updateClosestsCoverSpot()
    {
        if (m_coverSpots != null && m_enemiesObjects != null && m_coverSpots.Count > 0 && m_enemiesObjects.Count > 0)
        {
            if(!m_closestCoverSpot.Equals(Vector3.zero) && !checkIfVisiblyByEnemies(m_closestCoverSpot))
            {
                // last determined position is still valid
                return;
            }

            m_closestCoverSpot = Vector3.zero;

            // sort by distance
            List<PositionDistance> positionsDistances = new List<PositionDistance>();

            for (int i = 0; i < m_coverSpots.Count; i++)
            {
                positionsDistances.Add(new PositionDistance(m_coverSpots[i], Vector3.Distance(transform.position, m_coverSpots[i])));
            }

            List<Vector3> sortedSpots = new List<Vector3>();

            int tempClosestIndex;
            float tempClosestsDistance;

            while (positionsDistances.Count > 0)
            {
                tempClosestsDistance = float.MaxValue;
                tempClosestIndex = -1;

                for (int i = 0; i < positionsDistances.Count; i++)
                {
                    if (positionsDistances[i].m_distance < tempClosestsDistance)
                    {
                        tempClosestsDistance = positionsDistances[i].m_distance;
                        tempClosestIndex = i;
                    }
                }

                sortedSpots.Add(positionsDistances[tempClosestIndex].m_position);
                positionsDistances.RemoveAt(tempClosestIndex);
            }

            // select closest pos, that is reachable and not visible by enemies

            bool isVisibleByEnemy;
            int layermask = ~(1 << 20);

            for (int i = 0; i < sortedSpots.Count; i++)
            {
                isVisibleByEnemy = false;

                for (int j = 0; j < m_enemiesObjects.Count; j++)
                {
                    if (m_enemiesObjects[j] == null)
                    {
                        continue;
                    }

                    Vector3 enemyEyePos = m_enemiesObjects[j].transform.position + new Vector3(0, m_enemiesScripts[j].getAimOffsetY(), 0);

                    Vector3 thisNewEyePos = sortedSpots[i] + new Vector3(0, getAimOffsetY(), 0);
                    Vector3 dir = thisNewEyePos - enemyEyePos;

                    //Debug.DrawRay(enemyEyePos, dir);

                    float maxDistance = Vector3.Distance(enemyEyePos, thisNewEyePos);

                    if (Physics.Raycast(enemyEyePos, dir, maxDistance, layermask)) // not in vision by one enemy
                    {

                    }
                    else
                    {
                        isVisibleByEnemy = true;
                        break;
                    }
                }

                if (!isVisibleByEnemy)
                {
                    NavMeshPath result = new NavMeshPath();

                    if (m_navMeshAgent.CalculatePath(m_coverSpots[i], result)) // can move there
                    {
                        m_closestCoverSpot = sortedSpots[i];
                        break;
                    }
                }
            }

            if (m_closestCoverSpot.Equals(Vector3.zero))
            {
                Debug.LogWarning("NPC_base: could not find cover");
            }
        }
    }

    protected void setMovementMode(MovementMode newMode)
    {
        if(m_currentMovementMode != newMode)
        {
            m_currentMovementMode = newMode;

            switch(newMode)
            {
                case MovementMode.MoveToClosestCover:
                    {
                        updateClosestsCoverSpot();
                        //m_navMeshAgent.destination = m_closestCoverSpot;
                        startMovingTo(m_closestCoverSpot);
                        break;
                    }
                case MovementMode.MoveToClosestEnemy:
                    {
                        int closestIndex = getClosestVisibleEnemyIndex();

                        if (closestIndex > -1)
                        {
                            startMovingTo(m_enemiesObjects[closestIndex].transform.position);
                        }
                        break;
                    }
                case MovementMode.StandStill:
                    {
                        stopMoving();
                        break;
                    }
                case MovementMode.MoveRandomly:
                    {
                        break;
                    }
                default:
                    {
                        Debug.LogError("NPC_base: unknown MoveMode: " + newMode);
                        break;
                    }
            }
        }
    }

    protected void setAttackMode(AttackMode newAttackMode)
    {
        if (m_currentAttackMode != newAttackMode)
        {
            m_currentAttackMode = newAttackMode;

            switch (newAttackMode)
            {
                case AttackMode.Peaceful:
                    {
                        break;
                    }
                case AttackMode.Hostile:
                    {
                        int closestIndex = getClosestVisibleEnemyIndex();
                        if (closestIndex < 0)
                        {
                            m_currentTargetScript = null;
                        }
                        else
                        {
                            m_currentTargetScript = m_enemiesScripts[closestIndex];
                        }
                        break;
                    }
                default:
                    {
                        Debug.LogError("NPC_base: unknown AttackMode: " + newAttackMode);
                        break;
                    }
            }
        }
    }

    protected void shootAt(Vector3 target, int weaponIndex)
    {
        Vector3 direction =  (target - getMuzzlePosition()).normalized;

        direction = Quaternion.Euler(
                                        RandomValuesSeed.perlinNoiseRanged(-m_shootInaccuracyDegree, m_shootInaccuracyDegree,Time.realtimeSinceStartup, 1.2f),
                                        RandomValuesSeed.perlinNoiseRanged(-m_shootInaccuracyDegree, m_shootInaccuracyDegree, Time.realtimeSinceStartup, 2.2f),
                                        RandomValuesSeed.perlinNoiseRanged(-m_shootInaccuracyDegree, m_shootInaccuracyDegree, Time.realtimeSinceStartup, 3.2f)
                                    ) * direction;

        ProjectileManager.singleton.server_addGunshot(getMuzzlePosition(), direction.normalized, weaponIndex, m_gameID, m_noDamageGroups);
    }

    private Vector3 getMuzzlePosition()
    {
        return transform.position + transform.forward * m_muzzleOffset.z + transform.right * m_muzzleOffset.x + transform.up * m_muzzleOffset.y;
    }

    private bool checkIfEnemyIsVisible(Transform enemyTransform, Entity_damageable enemyScript)
    {
        Vector3 eyesPosition = getMuzzlePosition();

        RaycastHit hit;
        Physics.Raycast(eyesPosition, eyesPosition - enemyScript.getTargetPosition(), out hit);

        if (hit.transform == enemyTransform)
        {
            return true;

        }
        else
        {
            return false;
        }
    }

    private bool checkIfVisiblyByEnemies(Vector3 position)
    {
        int layermask = ~(1 << 20);

        for (int j = 0; j < m_enemiesObjects.Count; j++)
        {
            if (m_enemiesObjects[j] == null)
            {
                continue;
            }

            Vector3 enemyEyePos = m_enemiesObjects[j].transform.position + new Vector3(0, m_enemiesScripts[j].getAimOffsetY(), 0);

            Vector3 dir = position - enemyEyePos;

            //Debug.DrawRay(enemyEyePos, dir);

            float maxDistance = Vector3.Distance(enemyEyePos, position);

            if (Physics.Raycast(enemyEyePos, dir, maxDistance, layermask)) // not in vision by one enemy
            {

            }
            else
            {
                return true;
            }
        }

        return false;
    }

    protected bool shootClosestVisiblyEnemy(int weaponIndex)
    {
        int closestIndex = getClosestVisibleEnemyIndex();

        if (closestIndex < 0) // -1 = not found
        {
            return false;
        }
        else
        {
            shootAt(m_enemiesObjects[closestIndex].transform.position + Vector3.up * m_enemiesScripts[closestIndex].getAimOffsetY(), weaponIndex);
            return true;
        }
    }

    protected int getClosestVisibleEnemyIndex()
    {
        List<GameObject> visibleEnemies = getVisibleEnemies();

        if (visibleEnemies == null || visibleEnemies.Count <= 0)
        {
            return -1;
        }

        float closestDistance = float.MaxValue;
        int closestIndex = -1;
        float tempDistance;
        Vector3 eyesPosition = getMuzzlePosition();

        for (int i = 0; i < visibleEnemies.Count; i++)
        {
            tempDistance = Vector3.Distance(visibleEnemies[i].transform.position, eyesPosition);
            if (tempDistance < closestDistance)
            {
                closestDistance = tempDistance;
                closestIndex = i;
            }
        }

        // find in global list
        for(int i = 0; i < m_enemiesObjects.Count;i++)
        {
            if(m_enemiesObjects[i] == visibleEnemies[closestIndex])
            {
                closestIndex = i;
                break;
            }
        }

        return closestIndex;
    }

    protected List<GameObject> getVisibleEnemies()
    {
        if (m_enemiesObjects == null)
        {
            return null;
        }

        List<GameObject> returnValue = new List<GameObject>();
        Vector3 eyesPosition = getMuzzlePosition();

        for (int i = 0; i < m_enemiesObjects.Count; i++)
        {
            if (m_enemiesObjects[i] == null) // player got deleted
            {
                continue;
            }

            RaycastHit hit;
            Physics.Raycast(eyesPosition, m_enemiesObjects[i].transform.position + new Vector3(0, m_enemiesScripts[i].getAimOffsetY(), 0) - eyesPosition, out hit);

            if (hit.transform == m_enemiesObjects[i].transform)
            {
                returnValue.Add(m_enemiesObjects[i]);
            }
        }

        return returnValue;
    }

    public void setNoDamageGroups(string[] groups)
    {
        m_noDamageGroups = groups;
    }

    protected virtual void OnDestroy()
    {
        AIManager.singleton.onNPCDied(gameObject, this);
        if (GameManager_Custom.singleton != null)
        {
            GameManager_Custom.singleton.server_releaseNPC(m_gameID);
        }
    }
}
