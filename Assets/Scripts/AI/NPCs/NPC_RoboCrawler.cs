using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_RoboCrawler : NPC_base
{
    [Header("NPC_RoboCrawler")]
    [SerializeField] private bool m_TestShoot = false;
    
    private List<GameObject> m_latestVisibleEnemies = null;

    protected override void Awake()
    {
        base.Awake();
        m_NNInputsCount = 4; // 0: health/maxhealth, 1: energy/maxEngery, 2: visibleEnemyCount/20, 3 distance to closest enemy
        m_NNNeuronsCountX = 3;
        m_NNNeuronsCountY = 2;
        m_NNOutputsCount = 7; // 0: moveRandomly, 1: moveRandomlyDistance, 2: moveRandomlyTime, 3: moveToClosestEnemy, 4: speed, 5: shoot, 6: move to cover
    }

    protected override void Start()
    {
        base.Start();

        m_keepUpdatingCoverPositions = true;

        if (!m_isNeuronalNetworkInitialized)
        {
            initializeNeuronalNetwork();
            setNNGenomeWeightsRandom();
        }
    }

    protected override void Update()
    {
        base.Update();

        if(m_TestShoot)
        {
            m_TestShoot = false;

            shootClosestVisiblyEnemy(1);
        }
    }

    public override void onHitConfirmation()
    {
        getGenome().m_fitness += 5;
        m_NNFitness = getGenome().m_fitness;
    }

    protected override void onDamaged(float damage)
    {
        base.onDamaged(damage);

        getGenome().m_fitness -= 2;
        m_NNFitness = getGenome().m_fitness;
    }

    protected override void OnDestroy()
    {
        getGenome().m_fitness -= 40;
        m_NNFitness = getGenome().m_fitness;

        base.OnDestroy();
    }

    protected override void onUpdateNNInputs()
    {
        m_latestVisibleEnemies = getVisibleEnemies();
        int closestEnemeyIndex = getClosestVisibleEnemyIndex();

        m_NNInputs[0] = m_health / m_maxHealth;
        m_NNInputs[1] = m_energy / m_maxEnergy;

        if (m_latestVisibleEnemies != null)
        {
            m_NNInputs[2] = Mathf.Min((float)m_latestVisibleEnemies.Count / 20, 1f);
        }

        if(closestEnemeyIndex < 0)
        {
            m_NNInputs[3] = 1;
        }
        else
        {
            m_NNInputs[3] = Mathf.Min( Vector3.Distance( m_enemiesObjects[closestEnemeyIndex].transform.position, transform.position) / 700, 1);
        }
    }

    protected override void onNNDone()
    {
        m_distanceRandomDestination = Mathf.Max(m_maxDistanceRandomDestination * m_NNOutputs[1], 0f);

        m_timeRandomDestination = Mathf.Max(m_maxTimeRandomDestination * m_NNOutputs[2], 0f);

        if (m_NNOutputs[6] > 0.5f) // go towards enemy
        {
            setMovementMode(MovementMode.MoveToClosestCover);
        }
        else if (m_NNOutputs[3] > 0.5f)
        {
            setMovementMode(MovementMode.MoveToClosestEnemy);
        }
        else if (m_NNOutputs[0] > 0.5f)
        {
            setMovementMode(MovementMode.MoveRandomly);
        }
        else
        {
            setMovementMode(MovementMode.StandStill);
        }

        m_navMeshAgent.speed = Mathf.Max(m_maxSpeed * m_NNOutputs[4], 0f);

        if (m_NNOutputs[5] > 0.5f)
        {
            setAttackMode(AttackMode.Hostile);
        }
        else
        {
            setAttackMode(AttackMode.Peaceful);
        }
    }

}
