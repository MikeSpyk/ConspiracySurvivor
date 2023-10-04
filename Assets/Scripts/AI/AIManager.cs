using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIManager : MonoBehaviour
{
    public static AIManager singleton;

    [Header("Debug")]
    [SerializeField] private bool m_spawnBattleField = false;
    [SerializeField] private bool m_spawnNPC = false;
    [SerializeField] private bool m_NPCsAreEnemies = false;
    [SerializeField] private bool m_TrainingArena_State = false;
    [SerializeField] private bool m_TrainingArena_create = false;
    [SerializeField] private int m_TrainingArena_GenerationCount = 0; // output
    [SerializeField] private int m_TrainingArena_SpeciesCount = 0; // output
    [SerializeField] private int m_TrainingArena_maxSpeciesCount = 2;
    [SerializeField] private int m_TrainingArena_speciesMemberCount = 3;
    [SerializeField] private float m_TrainingArena_speciesTime = 10f;
    [SerializeField] private bool m_createCoverRaster = false;
    [Header("Configuration")]
    [SerializeField] private GameObject m_navMeshSurfacePrefab;
    [SerializeField] private GameObject m_TestNPCPrefab;
    [SerializeField] private Vector2 m_battlefieldSize = new Vector2(10, 10);
    [SerializeField] private float m_RasterCoverDistance = 1;
    [SerializeField] private float m_TimeUpdateEnemiesInRange = 2;
    [SerializeField] private float m_maxHeightDeltaWalkable = 0.2f;
    
    private float m_TrainingArenaLastSpeciesTime = 0;
    private List<Vector3> m_coverSpots = new List<Vector3>();
    private List<Vector3> m_TrainingArenaSpawns = new List<Vector3>();
    private List<Genome> m_deadGenomes = new List<Genome>();
    private List<Genome> m_availableGenomes = new List<Genome>();

    private float m_LastTimeUpdatePlayersInRange = 0; 
    private List<GameObject> m_NPCObjects = new List<GameObject>();
    private List<NPC_base> m_NPCBaseScripts = new List<NPC_base>();
    private List<GameObject> m_playerObjects = new List<GameObject>();
    private List<Entity_damageable> m_playerEntityDamageableScripts = new List<Entity_damageable>();
    private List<GameObject> m_navMeshSurfaceObjs = new List<GameObject>();

    private float[,] m_rasterCoverHeightmap = null;

    private void Awake()
    {
        singleton = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.LogWarning("AIManager: this script is obsolete. remove soon !");
    }

    // Update is called once per frame
    void Update()
    {
        if(Time.time > m_LastTimeUpdatePlayersInRange + m_TimeUpdateEnemiesInRange)
        {
            m_LastTimeUpdatePlayersInRange = Time.time;
            updateNPCsEnemiesInRange();
        }

        if(m_spawnBattleField)
        {
            m_spawnBattleField = false;

            RaycastHit[] hit = Physics.RaycastAll(transform.position, Vector3.down);
            Vector3 sufaceMiddle = transform.position;

            for(int i = 0; i < hit.Length; i++)
            {
                if(hit[i].collider.gameObject.layer == 10)
                {
                    sufaceMiddle.y = hit[i].point.y;
                    break;
                }
            }

            createBattlefield(sufaceMiddle, m_battlefieldSize);
        }

        if(m_spawnNPC)
        {
            m_spawnNPC = false;

            RaycastHit[] hit = Physics.RaycastAll(transform.position, Vector3.down);
            Vector3 sufaceMiddle = transform.position;

            for (int i = 0; i < hit.Length; i++)
            {
                if (hit[i].collider.gameObject.layer == 10)
                {
                    sufaceMiddle.y = hit[i].point.y;
                    break;
                }
            }

            spawnNPC(sufaceMiddle + new Vector3(0, m_TestNPCPrefab.GetComponent<NPC_base>().spawnHeight,0));
        }

        if(m_TrainingArena_create)
        {
            m_TrainingArena_create = false;
            createTrainingArena();
        }

        if(m_TrainingArena_State)
        {
            if(m_TrainingArenaSpawns.Count < m_TrainingArena_speciesMemberCount)
            {
                createTrainingArena();
            }

            if(Time.time > m_TrainingArenaLastSpeciesTime + m_TrainingArena_speciesTime)
            {
                m_TrainingArenaLastSpeciesTime = Time.time;

                if(m_TrainingArena_SpeciesCount >= m_TrainingArena_maxSpeciesCount)
                {
                    trainingArenaNewGeneration();
                    m_TrainingArena_SpeciesCount = 0;
                }

                trainingArenaNewSpecies();
            }
        }

        if(m_createCoverRaster)
        {
            m_createCoverRaster = false;
            createRaster_Cover();
            findCoverSpots();
        }

    }

    private void createRaster_Cover()
    {
        int sizeX = (int)(m_battlefieldSize.x / m_RasterCoverDistance);
        int sizeY = (int)(m_battlefieldSize.y / m_RasterCoverDistance);

        m_rasterCoverHeightmap = new float[sizeX, sizeY];

        Vector3 offset = transform.position - new Vector3(m_battlefieldSize.x / 2, -100, m_battlefieldSize.y / 2);
        int layermask = ~(1 << 20);

        for(int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                RaycastHit hit;
                if( Physics.Raycast(offset + new Vector3(i * m_RasterCoverDistance, 0, j * m_RasterCoverDistance), Vector3.down, out hit,float.MaxValue,layermask))
                {
                    m_rasterCoverHeightmap[i, j] = hit.point.y;
                    Debug.DrawRay(hit.point, Vector3.up, Color.red, 10f);
                }
                else
                {
                    m_rasterCoverHeightmap[i, j] = float.MinValue;
                }
            }
        }

    }

    private void findCoverSpots()
    {
        m_coverSpots.Clear();

        int coverRadius = (int)(m_TestNPCPrefab.GetComponent<NPC_base>().coverRadius / m_RasterCoverDistance);
        if (coverRadius != m_TestNPCPrefab.GetComponent<NPC_base>().coverRadius / m_RasterCoverDistance)
        {
            coverRadius++; // round
        }

        if(coverRadius % 2 == 0)
        {
            coverRadius++; // uneven number needed
        }

        int outerCoverRadius = coverRadius + 2;
        int outerCoverRadiusMinusOne = outerCoverRadius - 1;
        int coverRadiusHalf = coverRadius / 2;

        float coverHeight = m_TestNPCPrefab.GetComponent<NPC_base>().coverHeight;

        float outerBorderMax;
        float innerRectangleMin;
        float innerRectangleMax;

        for (int i = 0; i < m_rasterCoverHeightmap.GetLength(0) - outerCoverRadius; i++)
        {
            for (int j = 0; j < m_rasterCoverHeightmap.GetLength(1) - outerCoverRadius; j++)
            {

                outerBorderMax = float.MinValue;

                innerRectangleMin = float.MaxValue;
                innerRectangleMax = float.MinValue;

                for (int k = 0; k < outerCoverRadius; k++)
                {
                    for (int l = 0; l < outerCoverRadius; l++)
                    {
                        if (k == 0 || k == outerCoverRadiusMinusOne || l == 0 || l == outerCoverRadiusMinusOne) // outer border
                        {
                            if (m_rasterCoverHeightmap[i + k, j + l] > outerBorderMax)
                            {
                                outerBorderMax = m_rasterCoverHeightmap[i + k, j + l];
                            }
                        }
                        else // inner rectangle
                        {
                            if (m_rasterCoverHeightmap[i + k, j + l] < innerRectangleMin)
                            {
                                innerRectangleMin = m_rasterCoverHeightmap[i + k, j + l];
                            }

                            if (m_rasterCoverHeightmap[i + k, j + l] > innerRectangleMax)
                            {
                                innerRectangleMax = m_rasterCoverHeightmap[i + k, j + l];
                            }
                        }
                    }
                }

                if (Mathf.Abs(innerRectangleMin - innerRectangleMax) < m_maxHeightDeltaWalkable) // is walkable
                {
                    if (Mathf.Abs(innerRectangleMax - outerBorderMax) > coverHeight)
                    {
                        m_coverSpots.Add(transform.position - new Vector3(m_battlefieldSize.x / 2, transform.position.y, m_battlefieldSize.y / 2) + new Vector3((i + 1 + coverRadiusHalf) * m_RasterCoverDistance, m_rasterCoverHeightmap[i + 1 + coverRadiusHalf, j + 1 + coverRadiusHalf], (j + 1 + coverRadiusHalf) * m_RasterCoverDistance));
                        Debug.DrawRay(transform.position - new Vector3(m_battlefieldSize.x / 2, transform.position.y, m_battlefieldSize.y / 2) + new Vector3((i + 1 + coverRadiusHalf) * m_RasterCoverDistance, m_rasterCoverHeightmap[i + 1 + coverRadiusHalf, j + 1 + coverRadiusHalf], (j + 1 + coverRadiusHalf) * m_RasterCoverDistance), Vector3.up * 2, Color.green, 12);
                    }
                }

            }
        }

    }

    private void createTrainingArena()
    {
        m_TrainingArenaSpawns.Clear();

        for (int i = 0; i < m_TrainingArena_speciesMemberCount *2; i++)
        {
            Vector3 sufaceMiddle;

            if(i < m_TrainingArena_speciesMemberCount)
            {
                sufaceMiddle = transform.position + Vector3.right * 10 + Vector3.forward * i;
            }
            else
            {
                sufaceMiddle = transform.position + Vector3.right * -10 + Vector3.forward * (i- m_TrainingArena_speciesMemberCount);
            }

            RaycastHit[] hit = Physics.RaycastAll(sufaceMiddle, Vector3.down);

            for (int j = 0; j < hit.Length; j++)
            {
                if (hit[j].collider.gameObject.layer == 10)
                {
                    sufaceMiddle.y = hit[j].point.y;
                    break;
                }
            }

            Debug.DrawRay(sufaceMiddle, Vector3.up, Color.red, 3f);
            m_TrainingArenaSpawns.Add(sufaceMiddle);
        }
    }

    private Genome getRandomAvailableGenome()
    {
        if(m_availableGenomes.Count < 1) // = 0
        {
            return null;
        }
        else
        {
            int listIndex = (int)Mathf.Min(Random.value * m_availableGenomes.Count, m_availableGenomes.Count - 1);
            Genome returnValue = m_availableGenomes[listIndex];
            m_availableGenomes.RemoveAt(listIndex);
            return returnValue;
        }
    }

    private void trainingArenaNewSpecies()
    {
        // destroy current species
        while (m_NPCObjects.Count > 0)
        {
            GameObject tempOjb = m_NPCObjects[0];
            onNPCDied(tempOjb, m_NPCBaseScripts[0]);
            Destroy(tempOjb);
        }

        // create new species

        // team 1
        for(int i = 0; i < m_TrainingArena_speciesMemberCount; i++)
        {
            Genome randomGenome = getRandomAvailableGenome();

            if(randomGenome == null)
            {
                spawnNPC(m_TrainingArenaSpawns[i], "NPC Team 1");
                if(m_TrainingArena_GenerationCount > 0)
                {
                    Debug.LogWarning("AIManager: Not enought genomes available. creating random genome");
                }
            }
            else
            {
                spawnNPC(m_TrainingArenaSpawns[i], randomGenome, "NPC Team 1");
            }
        }

        // team 2
        for (int i = 0; i < m_TrainingArena_speciesMemberCount; i++)
        {
            Genome randomGenome = getRandomAvailableGenome();

            if (randomGenome == null)
            {
                spawnNPC(m_TrainingArenaSpawns[i+ m_TrainingArena_speciesMemberCount], "NPC Team 2");
                if (m_TrainingArena_GenerationCount > 0)
                {
                    Debug.LogWarning("AIManager: Not enought genomes available. creating random genome");
                }
            }
            else
            {
                spawnNPC(m_TrainingArenaSpawns[i + m_TrainingArena_speciesMemberCount], randomGenome, "NPC Team 2");
            }
        }

        m_TrainingArena_SpeciesCount++;
    }

    private void trainingArenaNewGeneration()
    {
        // destroy old gen
        while( m_NPCObjects.Count > 0)
        {
            GameObject tempOjb = m_NPCObjects[0];
            onNPCDied(tempOjb, m_NPCBaseScripts[0]);
            Destroy(tempOjb);
        }

        if(m_availableGenomes.Count > 0)
        {
            Debug.LogWarning("AIManager: not all genomes from last generation have been used");
            m_availableGenomes.Clear();
        }

        // find best half

        List<Genome> bestHalf = new List<Genome>();

        int genomesCount = m_TrainingArena_SpeciesCount * m_TrainingArena_speciesMemberCount; // /2(half) * 2(2 teams)

        for (int i = 0; i < genomesCount; i++)
        {
            if(m_deadGenomes.Count < 1)
            {
                Debug.LogWarning("AIManager: not enough genomes available to create new generation");
                break;
            }

            Genome bestGenome = null;
            float bestFitness = float.MinValue;

            for(int j = 0; j < m_deadGenomes.Count; j++)
            {
                if(m_deadGenomes[j].m_fitness > bestFitness)
                {
                    bestFitness = m_deadGenomes[j].m_fitness;
                    bestGenome = m_deadGenomes[j];
                }
            }

            m_deadGenomes.Remove(bestGenome);
            bestHalf.Add(bestGenome);
        }

        // create next generation genomes

        // copy best half
        for(int i = 0; i < bestHalf.Count; i++)
        {
            m_availableGenomes.Add(new Genome(bestHalf[i]));
        }

        // copy and mutate other half
        for (int i = 0; i < bestHalf.Count; i++)
        {
            Genome newGenome = new Genome(bestHalf[i]);
            newGenome.doRandomChangesWeights(10);
            m_availableGenomes.Add(newGenome);
        }

        m_TrainingArena_GenerationCount++;
    }

    private void createBattlefield(Vector3 position, Vector2 battlefieldSize)
    {
        for (int i = 0; i < m_navMeshSurfaceObjs.Count; i++)
        {
            Destroy(m_navMeshSurfaceObjs[i]);
        }
        m_navMeshSurfaceObjs.Clear();

        GameObject spawnedObj = Instantiate(m_navMeshSurfacePrefab, position, Quaternion.identity);
        spawnedObj.transform.parent = gameObject.transform;

        NavMeshSurface navMeshSurface = spawnedObj.GetComponent<NavMeshSurface>();

        navMeshSurface.collectObjects = CollectObjects.Volume;
        navMeshSurface.size = new Vector3(battlefieldSize.x, navMeshSurface.size.y, battlefieldSize.y);
        navMeshSurface.BuildNavMesh();

        m_navMeshSurfaceObjs.Add(spawnedObj);
    }

    private void spawnNPC(Vector3 position)
    {
        GameObject spawnedObj = Instantiate(m_TestNPCPrefab, position, Quaternion.identity);
        spawnedObj.transform.parent = gameObject.transform;
        m_NPCObjects.Add(spawnedObj);
        NPC_base script = spawnedObj.GetComponent<NPC_base>();
        m_NPCBaseScripts.Add(script);
        script.setCoverSpots(m_coverSpots);
    }
    private void spawnNPC(Vector3 position, string team)
    {
        GameObject spawnedObj = Instantiate(m_TestNPCPrefab, position, Quaternion.identity);
        spawnedObj.transform.parent = gameObject.transform;
        m_NPCObjects.Add(spawnedObj);
        NPC_base script = spawnedObj.GetComponent<NPC_base>();
        m_NPCBaseScripts.Add(script);
        script.setCoverSpots(m_coverSpots);
        script.setGroupName(team);
        script.setNoDamageGroups(new string[] { team });
    }
    private void spawnNPC(Vector3 position, Genome genome, string team)
    {
        GameObject spawnedObj = Instantiate(m_TestNPCPrefab, position, Quaternion.identity);
        spawnedObj.transform.parent = gameObject.transform;
        m_NPCObjects.Add(spawnedObj);
        NPC_base script = spawnedObj.GetComponent<NPC_base>();
        m_NPCBaseScripts.Add(script);
        script.initializeNeuronalNetwork(genome);
        script.setCoverSpots(m_coverSpots);
        script.setGroupName(team);
        script.setNoDamageGroups(new string[] { team });
    }

    private void updateNPCsEnemiesInRange()
    {
        m_playerObjects.Clear();
        m_playerEntityDamageableScripts.Clear();

        foreach (KeyValuePair<int,Player_external> keyPair in EntityManager.singleton.getPlayerID_ExternalScriptsDict())
        {
            m_playerObjects.Add(keyPair.Value.gameObject);
            m_playerEntityDamageableScripts.Add(keyPair.Value);
        }

        for (int i = 0; i < m_NPCObjects.Count; i++)
        {
            List<GameObject> enemiesInRangeObjects = new List<GameObject>();
            List<Entity_damageable> enemiesInRangeScripts = new List<Entity_damageable>();

            for (int j = 0; j < m_playerObjects.Count; j++)
            {
                if(Vector3.Distance(m_NPCObjects[i].transform.position, m_playerObjects[j].transform.position) < 700)
                {
                    enemiesInRangeObjects.Add(m_playerObjects[j]);
                    enemiesInRangeScripts.Add(m_playerEntityDamageableScripts[j]);
                }
            }

            if (m_NPCsAreEnemies)
            {
                for (int j = 0; j < m_NPCObjects.Count; j++)
                {
                    if(m_NPCObjects[i] == m_NPCObjects[j]) // dont attack yourself
                    {
                        continue;
                    }

                    if (m_NPCBaseScripts[i].getGroupName().Equals (m_NPCBaseScripts[j].getGroupName())) // same group arent enemies
                    {
                        continue; 
                    }

                    if (Vector3.Distance(m_NPCObjects[i].transform.position, m_NPCObjects[j].transform.position) < 700)
                    {
                        enemiesInRangeObjects.Add(m_NPCObjects[j]);
                        enemiesInRangeScripts.Add(m_NPCBaseScripts[j]);
                    }
                }
            }

            m_NPCBaseScripts[i].setEnemiesInRange(enemiesInRangeObjects, enemiesInRangeScripts);
        }
    }

    public void onNPCDied(GameObject npc, NPC_base script)
    {   
        if (m_NPCObjects.Contains(npc))
        {
            m_deadGenomes.Add(script.getGenome());
            m_NPCObjects.Remove(npc);
            m_NPCBaseScripts.Remove(script);
        }
    }
}
