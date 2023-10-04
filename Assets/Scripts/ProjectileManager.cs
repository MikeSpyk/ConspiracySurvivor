#define DEBUG
//#undef DEBUG

#define TRAJECTORY_ANGLE_TRAINING
#undef TRAJECTORY_ANGLE_TRAINING

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Linq;

public class ProjectileManager : MonoBehaviour
{
    private class Projectile
    {
        public Projectile(GameObject trailObject, Vector3 position, Vector3 velocityDirection, float speed, float airResistance, float gravityFactor, float damage, float startTime, uint uniqueID, int gameID)
        {
            m_trailObject = trailObject;
            m_position = position;
            m_velocityDirection = velocityDirection;
            m_airResistance = airResistance;
            m_damage = damage;
            m_speed = speed;
            m_startTime = startTime;
            m_gravityFactor = gravityFactor;
            m_trailRenderer = m_trailObject.GetComponent<TrailRenderer>();
            m_gameID = gameID;
            m_ID = uniqueID;
            m_noDamageGroups = new string[] { "" };
        }

        public Projectile(GameObject trailObject, Vector3 position, Vector3 velocityDirection, float speed, float airResistance, float gravityFactor, float damage, float startTime, uint uniqueID, int gameID, string[] noDamageGroups)
        {
            m_trailObject = trailObject;
            m_position = position;
            m_velocityDirection = velocityDirection;
            m_airResistance = airResistance;
            m_damage = damage;
            m_speed = speed;
            m_startTime = startTime;
            m_gravityFactor = gravityFactor;
            m_trailRenderer = m_trailObject.GetComponent<TrailRenderer>();
            m_gameID = gameID;
            m_ID = uniqueID;

            if (noDamageGroups != null)
            {
                m_noDamageGroups = noDamageGroups;
            }
            else
            {
                m_noDamageGroups = new string[] { "" };
            }
        }

        public Projectile(GameObject trailObject, Vector3 position, Vector3 velocityDirection, float speed, float airResistance, float gravityFactor, float damage, float startTime, uint uniqueID, int gameID, string noDamageGroup)
        {
            m_trailObject = trailObject;
            m_position = position;
            m_velocityDirection = velocityDirection;
            m_airResistance = airResistance;
            m_damage = damage;
            m_speed = speed;
            m_startTime = startTime;
            m_gravityFactor = gravityFactor;
            m_trailRenderer = m_trailObject.GetComponent<TrailRenderer>();
            m_gameID = gameID;
            m_ID = uniqueID;

            if (noDamageGroup != null)
            {
                m_noDamageGroups = new string[1];
                m_noDamageGroups[0] = noDamageGroup;
            }
            else
            {
                m_noDamageGroups = new string[] { "" };
            }
        }

        public GameObject m_trailObject;
        public TrailRenderer m_trailRenderer;
        public Vector3 m_position;
        public Vector3 m_velocityDirection;
        public Vector3 m_startPosition;
        public float m_speed;
        public float m_airResistance;
        public uint m_ID;
        public int m_gameID; // the owner
        public float m_damage;
        public float m_startTime;
        public float m_gravityFactor;
        public string[] m_noDamageGroups;

#if TRAJECTORY_ANGLE_TRAINING
        public Genome m_trajectoryAngle_Genome;
        public Vector3 m_trajectoryAngle_Target;
#endif
    }

    private static readonly int PROJECTILE_LAYER_MASK = System.BitConverter.ToInt32(new byte[] {
                                                                                                BitByteTools.getByte(true,false,false,false,false,false,false,false), // default layer
                                                                                                BitByteTools.getByte(true,false,true,true,true,true,true,false), // no bulding preview, not first person
                                                                                                byte.MaxValue,
                                                                                                byte.MaxValue
                                                                                                }, 0); // everything but terrain
    public static ProjectileManager singleton;

    [Header("Configurations")]
    [SerializeField] private GameObject[] m_weaponsPrefabs;
    [SerializeField] private GameObject m_trailObject_prefab;
    [SerializeField] private float m_projektileMaxLifetime = 100;
    [SerializeField] private int m_rayCastHitCacheSize = 100; // used for raycast non alloc size
    [SerializeField] private float m_server_sendProjectileUpdateRate = 0.05f;
    [SerializeField] private float m_minDistanceBeforeTrailVisible = 10f;
    [Header("Debug")]
    [SerializeField] private bool m_hideTrailsHirachy = true;
    [SerializeField] private bool m_noCache = false;
    [SerializeField] private bool m_DrawHitRay = false;
    [SerializeField] private Color m_hitRayColor = Color.red;
    [SerializeField] private bool m_DrawStartRay = false;
    [SerializeField] private Color m_StartRayColor = Color.green;
    [SerializeField] private bool m_trainTrajectoryNN = false;
    [SerializeField] private bool m_saveTrajectoryNNGenome = false;
    [SerializeField] private int m_trainTrajectoryGenMemeberCount = 100;
    [SerializeField] private float m_trainTrajectoryBestFitness = 0;
    [SerializeField] private int m_trainTrajectorySaveCount = 100;
    [SerializeField] private int m_trainTrajectoryMutationsCount = 10;
    [SerializeField] [Range(0, 0.5f)] private float m_trainTrajectoryMutationsShare = 0.5f;
    [SerializeField] private bool m_trainTrajectoryMix = false;
    [SerializeField] private bool m_trainTrajectoryDiversity = false;
    [SerializeField] private float m_trainTrajectoryDiversityWeight = 1;
    [SerializeField] private bool m_trainTrajectoryTestVeryBest = false;

    private bool m_trainTrajectoryNewGen = true;
    private EvolutionaryAlgorithm m_trainTrajectoryEvoAlgo = null;

    private RaycastHit[] m_temp_rayCastHits;
    private List<RaycastHit> m_tempRayCastHitsSorted = new List<RaycastHit>();
    private List<Projectile> m_free_projectiles = new List<Projectile>();
    private List<Projectile> m_active_projectiles = new List<Projectile>();
    private Dictionary<uint, Projectile> m_projectilesUID_Projectile = new Dictionary<uint, Projectile>();
    private List<float> m_prefabTimeBetweenItemUsage = new List<float>();
    private List<float> m_prefabAirResistance = new List<float>();
    private List<float> m_prefabGravityFactor = new List<float>();
    private List<float> m_prefabDamage = new List<float>();
    private List<float> m_prefabStartSpeed = new List<float>();
    private List<int> m_prefabShotSoundIndex = new List<int>();
    private NeuralNetwork m_trajectoryAngleCalculator = null;
    private int m_trainTrajectoryStage = 0;
    private float m_trainTrajectoryDistance = -700;
    private Genome[] currentGenGenomes = null;
    private float m_server_lastTimeSendProjectileUpdate = 0;
    private bool m_serverSendProjectileUpdate = false;

    private uint uniqueProjectileID = 0;

    void Awake()
    {
        singleton = this;
        m_trajectoryAngleCalculator = new NeuralNetwork();
        m_trajectoryAngleCalculator.setInputsCount(4); // 0: delta Y, 1: delta X, 2: speed, 3: gravity
        m_trajectoryAngleCalculator.setNeuronsCount(10, 4);
        m_trajectoryAngleCalculator.setOutputsCount(1); //0: angle, 1: arc(sin/Cos/Tan)

        m_trainTrajectoryEvoAlgo = new EvolutionaryAlgorithm(4, 5, 4, 2);
        m_trainTrajectoryEvoAlgo.m_desiredMemberCount = m_trainTrajectoryGenMemeberCount;
        m_trainTrajectoryEvoAlgo.loadGenomesFromDisk(@"C:\UnityTestOutputs\Genomes");
    }

    void Start()
    {
        m_temp_rayCastHits = new RaycastHit[m_rayCastHitCacheSize];
        FPVItem_GunBase temp_item;
        for (int i = 0; i < m_weaponsPrefabs.Length; i++)
        {
            temp_item = m_weaponsPrefabs[i].GetComponent<FPVItem_GunBase>();
            m_prefabTimeBetweenItemUsage.Add(temp_item.timeBetweenItemUsagePrimary);
            m_prefabAirResistance.Add(temp_item.airResistance);
            m_prefabGravityFactor.Add(temp_item.gravityFactor);
            m_prefabDamage.Add(temp_item.damage);
            m_prefabStartSpeed.Add(temp_item.projectileSpeed);
            m_prefabShotSoundIndex.Add(temp_item.shotSoundIndex);
        }
    }

    void LateUpdate()
    {
        if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            lateUpdateServer();
        }
        else if (GameManager_Custom.singleton.isClient)
        {
            lateUpdateClient();
        }

#if TRAJECTORY_ANGLE_TRAINING
        if(m_trainTrajectoryNN)
        {
            if (m_trainTrajectoryNewGen)
            {
                m_trainTrajectoryNewGen = false;

                Vector3 randomPos = getRandomPosOnGeomatry(transform.position, 700);

                if (m_trainTrajectoryStage == 0)
                {
                    currentGenGenomes = m_trainTrajectoryEvoAlgo.getAllPreparedGenomes();

                    for (int i = 0; i < currentGenGenomes.Length; i++)
                    {
                        currentGenGenomes[i].m_fitness = 0;
                    }

                    m_trainTrajectoryDistance = -700;

                    Vector3 raycastOrigin = transform.position + new Vector3(m_trainTrajectoryDistance, 2000, 0);

                    RaycastHit hit;

                    Physics.Raycast(raycastOrigin, Vector3.down, out hit);

                    randomPos = hit.point;

                    m_trainTrajectoryStage++;
                }
                else
                {
                    m_trainTrajectoryDistance += 101;

                    Vector3 raycastOrigin = transform.position + new Vector3(m_trainTrajectoryDistance, 2000, 0);

                    RaycastHit hit;

                    Physics.Raycast(raycastOrigin, Vector3.down, out hit);

                    randomPos = hit.point;

                    if (m_trainTrajectoryDistance > 700) // == 700
                    {
                        m_trainTrajectoryStage = 0;
                    }
                }

                //float randomSpeed = Mathf.Max(Random.value * 2000, 800);
                float randomSpeed = 800;
                //float randomGravityFactor = Mathf.Max( Random.value * 1.2f,0.0001f);
                //float randomGravityFactor = 1.2f;
                float randomGravityFactor = 1f;

                Debug.DrawRay(randomPos, Vector3.up * 30, Color.blue, m_projektileMaxLifetime);
                Debug.DrawRay(randomPos, Vector3.right * 30, Color.blue, m_projektileMaxLifetime);


                if (m_trainTrajectoryTestVeryBest)
                {
                    float deltaY = randomPos.y - transform.position.y;
                    float hypotenuse = Vector3.Distance(transform.position, randomPos);
                    float deltaX = Mathf.Sqrt(Mathf.Pow(hypotenuse, 2) - Mathf.Pow(deltaY, 2)); //pythagoras

                    NeuralNetwork tempNeuronalNetwork = new NeuralNetwork(m_trainTrajectoryEvoAlgo.m_bestGenomeLastGen);
                    float[] outputs = tempNeuronalNetwork.think(new float[] { deltaY / 1000, deltaX / 1000, randomSpeed / 1000, -Physics.gravity.magnitude * randomGravityFactor / 12 }); // 0: delta Y, 1: delta X, 2: speed, 3: gravity

                    Vector3 dir = randomPos - transform.position;

                    if (outputs[1] < -0.33f)
                    {
                        dir.y = Mathf.Tan(Mathf.Asin(outputs[0])) * deltaX;
                    }
                    else if (outputs[1] < 0.33f)
                    {
                        dir.y = Mathf.Tan(Mathf.Acos(outputs[0])) * deltaX;
                    }
                    else
                    {
                        dir.y = Mathf.Tan(Mathf.Atan(outputs[0] * 10)) * deltaX;
                    }

                    GameObject tempTrailObj = Instantiate(m_trailObject_prefab, transform.position, Quaternion.identity);

                    uniqueProjectileID++;

                    Projectile tempCurrentProjectile = new Projectile(tempTrailObj, transform.position, dir.normalized, randomSpeed, 1, randomGravityFactor, 1, Time.time, uniqueProjectileID, -2);
                    tempCurrentProjectile.m_trajectoryAngle_Genome = m_trainTrajectoryEvoAlgo.m_bestGenomeLastGen;
                    tempCurrentProjectile.m_trajectoryAngle_Target = randomPos;

                    m_active_projectiles.Add(tempCurrentProjectile);
                }
                else
                {
                    for (int i = 0; i < currentGenGenomes.Length; i++)
                    {
                        float deltaY = randomPos.y - transform.position.y;
                        float hypotenuse = Vector3.Distance(transform.position, randomPos);
                        float deltaX = Mathf.Sqrt(Mathf.Pow(hypotenuse, 2) - Mathf.Pow(deltaY, 2)); //pythagoras

                        Genome evoGenome = currentGenGenomes[i];

                        NeuralNetwork tempNeuronalNetwork = new NeuralNetwork(evoGenome);
                        float[] outputs = tempNeuronalNetwork.think(new float[] { deltaY / 1000, deltaX / 1000, randomSpeed / 1000, -Physics.gravity.magnitude * randomGravityFactor / 12 }); // 0: delta Y, 1: delta X, 2: speed, 3: gravity

                        Vector3 dir = randomPos - transform.position;

                        if (outputs[1] < -0.33f)
                        {
                            dir.y = Mathf.Tan(Mathf.Asin(outputs[0])) * deltaX;
                        }
                        else if (outputs[1] < 0.33f)
                        {
                            dir.y = Mathf.Tan(Mathf.Acos(outputs[0])) * deltaX;
                        }
                        else
                        {
                            dir.y = Mathf.Tan(Mathf.Atan(outputs[0] * 10)) * deltaX;
                        }

                        GameObject tempTrailObj = Instantiate(m_trailObject_prefab, transform.position, Quaternion.identity);

                        uniqueProjectileID++;

                        Projectile tempCurrentProjectile = new Projectile(tempTrailObj, transform.position, dir.normalized, randomSpeed, 1, randomGravityFactor, 1, Time.time, uniqueProjectileID, -2);
                        tempCurrentProjectile.m_trajectoryAngle_Genome = evoGenome;
                        tempCurrentProjectile.m_trajectoryAngle_Target = randomPos;

                        m_active_projectiles.Add(tempCurrentProjectile);
                    }
                }
            }

            if(m_active_projectiles.Count == 0)
            {
                m_trainTrajectoryNewGen = true;
                if (m_trainTrajectoryStage == 0)
                {
                    if (m_saveTrajectoryNNGenome)
                    {
                        m_saveTrajectoryNNGenome = false;
                        m_trainTrajectoryEvoAlgo.nextGenWriteBestGenomesToDisk(m_trainTrajectorySaveCount, @"C:\UnityTestOutputs\Genomes");
                    }
                    m_trainTrajectoryEvoAlgo.disposeGenomes(currentGenGenomes);
                    m_trainTrajectoryEvoAlgo.m_desiredMemberCount = m_trainTrajectoryGenMemeberCount;
                    m_trainTrajectoryEvoAlgo.m_mutationsCount = m_trainTrajectoryMutationsCount;
                    m_trainTrajectoryEvoAlgo.m_mutatedMemberShare = m_trainTrajectoryMutationsShare;
                    m_trainTrajectoryEvoAlgo.m_mix = m_trainTrajectoryMix;
                    m_trainTrajectoryEvoAlgo.m_useDiversity = m_trainTrajectoryDiversity;
                    m_trainTrajectoryEvoAlgo.m_diversityWeight = m_trainTrajectoryDiversityWeight;
                    if (!m_trainTrajectoryTestVeryBest)
                    {
                        m_trainTrajectoryEvoAlgo.createNextGeneration();
                    }
                    m_trainTrajectoryBestFitness = m_trainTrajectoryEvoAlgo.bestFitness;
                }
            }

        }
#endif
    }

    private void lateUpdateServer()
    {
        float moveDistance;
        float gravitySpeedToAdd;
        float angleVelocityGravity;
        int hitCount;
        bool removeProjectile;
        Entity_damageable temp_HitObjectCompenentEntityDamageable;
        EntityHitbox temp_HitObjectCompenentEntityHitbox;
        int j;

        if (Time.time > m_server_lastTimeSendProjectileUpdate + m_server_sendProjectileUpdateRate)
        {
            m_serverSendProjectileUpdate = true;
            m_server_lastTimeSendProjectileUpdate = Time.time;
        }
        else
        {
            m_serverSendProjectileUpdate = false;
        }

        for (int i = 0; i < m_active_projectiles.Count; i++)
        {
            if (Time.time > m_active_projectiles[i].m_startTime + m_projektileMaxLifetime)
            {
                recyleProjectile(i);
                i--; // dont skip one item of the list
                continue;
            }

            if (GameManager_Custom.singleton.isServerAndClient)
            {
                if (!m_active_projectiles[i].m_trailRenderer.emitting)
                {
                    if (Vector3.Distance(m_active_projectiles[i].m_startPosition, m_active_projectiles[i].m_position) > m_minDistanceBeforeTrailVisible)
                    {
                        m_active_projectiles[i].m_trailRenderer.emitting = true;
                    }
                }
            }

            moveDistance = m_active_projectiles[i].m_speed * Time.deltaTime;
            hitCount = Physics.RaycastNonAlloc(m_active_projectiles[i].m_position, m_active_projectiles[i].m_velocityDirection, m_temp_rayCastHits, moveDistance, PROJECTILE_LAYER_MASK);
            removeProjectile = false;
            m_tempRayCastHitsSorted.Clear();
            for (j = 0; j < hitCount; j++)
            {
                m_tempRayCastHitsSorted.Add(m_temp_rayCastHits[j]);
            }

            m_tempRayCastHitsSorted = m_tempRayCastHitsSorted.OrderBy(h => h.distance).ToList();

            if (hitCount > m_rayCastHitCacheSize)
            {
                Debug.LogError("ProjectileManager: RayCastHit-cache-size too small !");
                recyleProjectile(i);
                i--; // dont skip one item of the list
                continue;
            }

            for (j = 0; j < hitCount; j++)
            {
                temp_HitObjectCompenentEntityDamageable = m_tempRayCastHitsSorted[j].collider.GetComponent<Entity_damageable>();

                if (temp_HitObjectCompenentEntityDamageable == null)
                {
                    temp_HitObjectCompenentEntityHitbox = m_tempRayCastHitsSorted[j].collider.GetComponent<EntityHitbox>();

                    if (temp_HitObjectCompenentEntityHitbox == null) // neighter Entity_damageable nor EntityHitbox but hit
                    {
                        removeProjectile = true;
                        NetworkingManager.singleton.server_sendWorldSoundToAllInRange(22, m_tempRayCastHitsSorted[j].point); // play default sound
                        NetworkingManager.singleton.server_sendParticleEffectToAllInRange(2, m_tempRayCastHitsSorted[j].point, Quaternion.Euler(-m_active_projectiles[i].m_velocityDirection));
                        break;
                    }
                    else // hitbox
                    {
                        if (temp_HitObjectCompenentEntityHitbox.m_gameID != m_active_projectiles[i].m_gameID) // hit a hitbox, that isnt the shooters
                        {
                            temp_HitObjectCompenentEntityHitbox.addImpact(m_tempRayCastHitsSorted[j].point, m_active_projectiles[i].m_velocityDirection * m_active_projectiles[i].m_speed, m_active_projectiles[i].m_damage);
                            GameManager_Custom.singleton.server_passOnHitConfirmation(m_active_projectiles[i].m_gameID);

                            removeProjectile = true;
                            break;
                        }
                    }
                }
                else if (!temp_HitObjectCompenentEntityDamageable.m_projectileManagerIgnore) // Entity_damageable
                {
                    bool sameGroup = false;

                    for (int k = 0; k < m_active_projectiles[i].m_noDamageGroups.Length; k++)
                    {
                        if (m_active_projectiles[i].m_noDamageGroups[k].Equals(temp_HitObjectCompenentEntityDamageable.getGroupName()))
                        {
                            sameGroup = true;
                            break;
                        }
                    }

                    if (!sameGroup)
                    {
                        temp_HitObjectCompenentEntityDamageable.addImpact(m_tempRayCastHitsSorted[j].point, m_active_projectiles[i].m_velocityDirection * m_active_projectiles[i].m_speed, m_active_projectiles[i].m_damage);
                        if (temp_HitObjectCompenentEntityDamageable.hitCallback)
                        {
                            GameManager_Custom.singleton.server_passOnHitConfirmation(m_active_projectiles[i].m_gameID);
                        }
                    }

                    removeProjectile = true;
                    break;
                }
            }

            if (removeProjectile)
            {
#if DEBUG
                if (m_DrawHitRay)
                {
                    Debug.DrawRay(m_tempRayCastHitsSorted[j].point, Vector3.up, m_hitRayColor, 2f);
                }
#endif
#if TRAJECTORY_ANGLE_TRAINING
                m_active_projectiles[i].m_trajectoryAngle_Genome.m_fitness += 1500 - Vector2.Distance(VectorTools.Vec2FromVec3XZ(m_tempRayCastHitsSorted[j].point), VectorTools.Vec2FromVec3XZ(m_active_projectiles[i].m_trajectoryAngle_Target));
                //m_trainTrajectoryEvoAlgo.disposeGenome(m_active_projectiles[i].m_trajectoryAngle_Genome, VectorTools.Vec2FromVec3XZ(m_tempRayCastHitsSorted[j].point));
                //Debug.DrawLine(m_active_projectiles[i].m_trajectoryAngle_Target, m_tempRayCastHitsSorted[j].point,Color.red,3f);
                Debug.DrawRay(m_tempRayCastHitsSorted[j].point, Vector3.up * 10, Color.green, 3f);
                Debug.DrawRay(m_tempRayCastHitsSorted[j].point, Vector3.right * 10, Color.green, 3f);
#endif
                recyleProjectile(i);
                i--; // dont skip one item of the list
            }
            else
            {
                // translate projectile

                m_active_projectiles[i].m_position += m_active_projectiles[i].m_velocityDirection * moveDistance;
                m_active_projectiles[i].m_trailObject.transform.position = m_active_projectiles[i].m_position;

                angleVelocityGravity = Vector3.Angle(m_active_projectiles[i].m_velocityDirection, Physics.gravity.normalized);

                gravitySpeedToAdd = Mathf.Cos(Mathf.Deg2Rad * angleVelocityGravity) * Physics.gravity.magnitude * m_active_projectiles[i].m_gravityFactor * Time.deltaTime;

                m_active_projectiles[i].m_speed = (m_active_projectiles[i].m_speed / (1 + m_active_projectiles[i].m_airResistance * Time.deltaTime)) + gravitySpeedToAdd;
                //m_active_projectiles[i].m_speed = m_active_projectiles[i].m_speed  + gravitySpeedToAdd; // no air resistance

                m_active_projectiles[i].m_velocityDirection = (m_active_projectiles[i].m_velocityDirection * m_active_projectiles[i].m_speed + Physics.gravity * m_active_projectiles[i].m_gravityFactor * Time.deltaTime).normalized;
                if (m_serverSendProjectileUpdate)
                {
                    GameManager_Custom.singleton.server_sendProjectileUpdateToPlayersInRange((int)m_active_projectiles[i].m_ID, m_active_projectiles[i].m_position);
                }
            }
        }
    }

    private void lateUpdateClient()
    {
        float moveDistance;
        float gravitySpeedToAdd;
        float angleVelocityGravity;

        for (int i = 0; i < m_active_projectiles.Count; i++)
        {
            if (Time.time > m_active_projectiles[i].m_startTime + m_projektileMaxLifetime)
            {
                recyleProjectile(i);
                i--; // dont skip one item of the list
                continue;
            }

            if (!m_active_projectiles[i].m_trailRenderer.emitting)
            {
                if (Vector3.Distance(m_active_projectiles[i].m_startPosition, m_active_projectiles[i].m_position) > m_minDistanceBeforeTrailVisible)
                {
                    m_active_projectiles[i].m_trailRenderer.emitting = true;
                }
            }

            moveDistance = m_active_projectiles[i].m_speed * Time.deltaTime;

            m_active_projectiles[i].m_position += m_active_projectiles[i].m_velocityDirection * moveDistance;
            m_active_projectiles[i].m_trailObject.transform.position = m_active_projectiles[i].m_position;

            angleVelocityGravity = Vector3.Angle(m_active_projectiles[i].m_velocityDirection, Physics.gravity.normalized);

            gravitySpeedToAdd = Mathf.Cos(Mathf.Deg2Rad * angleVelocityGravity) * Physics.gravity.magnitude * m_active_projectiles[i].m_gravityFactor * Time.deltaTime;

            m_active_projectiles[i].m_speed = (m_active_projectiles[i].m_speed / (1 + m_active_projectiles[i].m_airResistance * Time.deltaTime)) + gravitySpeedToAdd;

            m_active_projectiles[i].m_velocityDirection = (m_active_projectiles[i].m_velocityDirection * m_active_projectiles[i].m_speed + Physics.gravity * m_active_projectiles[i].m_gravityFactor * Time.deltaTime).normalized;
        }
    }

    private void recyleProjectile(int listIndex)
    {
        m_projectilesUID_Projectile.Remove(m_active_projectiles[listIndex].m_ID);

        m_free_projectiles.Add(m_active_projectiles[listIndex]);

        m_active_projectiles[listIndex].m_trailRenderer.enabled = false;
        m_active_projectiles[listIndex].m_trailObject.SetActive(false);
        m_active_projectiles[listIndex].m_trailRenderer.emitting = false;

        m_active_projectiles.RemoveAt(listIndex);
    }

    public void server_addGunshot(Vector3 startPos, Vector3 startDirection, int weaponPrefabIndex, int gameID, string[] noDamageGroups)
    {
        uniqueProjectileID++;
        addGunshot(startPos, startDirection, weaponPrefabIndex, gameID, uniqueProjectileID, noDamageGroups);

        GameManager_Custom.singleton.server_sendNewProjectileToPlayersInRange(weaponPrefabIndex, (int)uniqueProjectileID, startPos, startDirection.normalized);
    }

    public void server_addGunshot(Vector3 startPos, Vector3 startVelocity, int weaponPrefabIndex, int gameID)
    {
        uniqueProjectileID++;
        addGunshot(startPos, startVelocity, weaponPrefabIndex, gameID, uniqueProjectileID, null);

        GameManager_Custom.singleton.server_sendNewProjectileToPlayersInRange(weaponPrefabIndex, (int)uniqueProjectileID, startPos, startVelocity.normalized);
    }

    public void client_addGunshot(Vector3 startPos, Vector3 startVelocity, int weaponPrefabIndex, uint projectileID)
    {
        if (!m_projectilesUID_Projectile.ContainsKey(projectileID)) // if projectile not created yet (in UDP messages could be received in wrong order or multiple times)
        {
            addGunshot(startPos, startVelocity, weaponPrefabIndex, -1, projectileID, null);
        }
    }

    public void client_updateProjectilePos(uint projectileID, Vector3 newPosition)
    {
        Projectile temp_projectile;

        if (m_projectilesUID_Projectile.TryGetValue(projectileID, out temp_projectile))
        {
            temp_projectile.m_position = newPosition;
        }
        else
        {
            client_newProjectileWithUpdate(projectileID, newPosition);
        }
    }

    /// <summary>
    /// creates a new projectile for an update message, because the create message was lost
    /// </summary>
    /// <param name="projectileID"></param>
    /// <param name=""></param>
    private void client_newProjectileWithUpdate(uint projectileID, Vector3 position)
    {
        Projectile tempCurrentProjectile;

        if (m_free_projectiles.Count > 0) // create from cache
        {
            tempCurrentProjectile = m_free_projectiles[0];
            m_free_projectiles.RemoveAt(0);

            tempCurrentProjectile.m_ID = projectileID;
            tempCurrentProjectile.m_airResistance = 0;
            tempCurrentProjectile.m_damage = 0;
            tempCurrentProjectile.m_gameID = -1;
            tempCurrentProjectile.m_position = position;
            tempCurrentProjectile.m_velocityDirection = Vector3.zero;
            tempCurrentProjectile.m_speed = 0;
            tempCurrentProjectile.m_trailRenderer.Clear();
            tempCurrentProjectile.m_trailRenderer.enabled = true;
            tempCurrentProjectile.m_trailObject.SetActive(true);
            tempCurrentProjectile.m_startTime = Time.time;
            tempCurrentProjectile.m_gravityFactor = 0;
        }
        else // create new one
        {
            GameObject tempTrailObj = Instantiate(m_trailObject_prefab, position, Quaternion.identity);
            if (m_hideTrailsHirachy)
            {
                tempTrailObj.hideFlags = HideFlags.HideInHierarchy;
            }

            tempCurrentProjectile = new Projectile(tempTrailObj, position, Vector3.zero, 0, 0, 0, 0, Time.time, projectileID, -1);
        }

        tempCurrentProjectile.m_startPosition = position;

        m_active_projectiles.Add(tempCurrentProjectile);
        m_projectilesUID_Projectile.Add(tempCurrentProjectile.m_ID, tempCurrentProjectile);
    }

    public void addGunshot(Vector3 startPos, Vector3 startDirection, int weaponPrefabIndex, int gameID, uint projectileID, string[] noDamageGroups)
    {
        Projectile tempCurrentProjectile;

        if (!m_noCache && m_free_projectiles.Count > 0) // create from cache
        {
            tempCurrentProjectile = m_free_projectiles[0];
            m_free_projectiles.RemoveAt(0);

            tempCurrentProjectile.m_ID = projectileID;
            tempCurrentProjectile.m_airResistance = m_prefabAirResistance[weaponPrefabIndex];
            tempCurrentProjectile.m_damage = m_prefabDamage[weaponPrefabIndex];
            tempCurrentProjectile.m_gameID = gameID;
            tempCurrentProjectile.m_position = startPos;
            tempCurrentProjectile.m_velocityDirection = startDirection.normalized;
            tempCurrentProjectile.m_speed = m_prefabStartSpeed[weaponPrefabIndex];
            tempCurrentProjectile.m_trailRenderer.Clear();
            tempCurrentProjectile.m_trailRenderer.enabled = true;
            tempCurrentProjectile.m_trailObject.SetActive(true);
            tempCurrentProjectile.m_startTime = Time.time;
            tempCurrentProjectile.m_gravityFactor = m_prefabGravityFactor[weaponPrefabIndex];

            if (noDamageGroups != null)
            {
                tempCurrentProjectile.m_noDamageGroups = noDamageGroups;
            }
            else
            {
                tempCurrentProjectile.m_noDamageGroups = new string[] { "" };
            }
        }
        else // create new one
        {
            GameObject tempTrailObj = Instantiate(m_trailObject_prefab, startPos, Quaternion.identity);
            if (m_hideTrailsHirachy)
            {
                tempTrailObj.hideFlags = HideFlags.HideInHierarchy;
            }

            tempCurrentProjectile = new Projectile(tempTrailObj, startPos, startDirection.normalized, m_prefabStartSpeed[weaponPrefabIndex], m_prefabAirResistance[weaponPrefabIndex], m_prefabGravityFactor[weaponPrefabIndex], m_prefabDamage[weaponPrefabIndex], Time.time, projectileID, gameID, noDamageGroups);
        }

        tempCurrentProjectile.m_startPosition = startPos;

#if DEBUG
        if (m_DrawStartRay)
        {
            Debug.DrawRay(startPos, Vector3.up, m_StartRayColor, 1f);
        }
#endif
        m_active_projectiles.Add(tempCurrentProjectile);
        m_projectilesUID_Projectile.Add(tempCurrentProjectile.m_ID, tempCurrentProjectile);
    }

    public void client_createGunShot(Vector3 startPos, Vector3 startVelocity, int weaponPrefabIndex)
    {
        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            NetworkingManager.singleton.client_sendStartProjectile(startPos, startVelocity.normalized, weaponPrefabIndex);
        }
    }

    /// <summary>
    /// OBSOLETE
    /// </summary>
    /// <param name="weaponIndex"></param>
    /// <param name="startPos"></param>
    /// <param name="targetPos"></param>
    /// <returns></returns>
    public Vector3 getDirForTrajectoryTarget(int weaponIndex, Vector3 startPos, Vector3 targetPos)
    {
        Debug.LogWarning("this method is not accurat");

        if (weaponIndex < 0 || weaponIndex >= m_weaponsPrefabs.Length)
        {
            Debug.LogWarning("ProjectileManager: getAngleForTrajectoryTarget: weaponIndex out of range");
            return Vector3.zero;
        }

        float deltaY = targetPos.y - startPos.y;
        float hypotenuse = Vector3.Distance(startPos, targetPos);
        float deltaX = Mathf.Sqrt(Mathf.Pow(hypotenuse, 2) - Mathf.Pow(deltaY, 2)); //pythagoras

        float angle = Mathf.Asin(deltaX * Physics.gravity.magnitude * m_prefabGravityFactor[weaponIndex] / Mathf.Pow(m_prefabStartSpeed[weaponIndex], 2)) / 2; // same height
                                                                                                                                                               //float angle = Mathf.Acos(((1/-deltaY - 1 / deltaX) * Physics.gravity.magnitude * m_prefabGravityFactor[weaponIndex] * deltaX) / (2 * m_prefabStartSpeed[weaponIndex]));

        /*
        float angle = 0.5f * (Mathf.Asin((Physics.gravity.magnitude * m_prefabGravityFactor[weaponIndex] * hypotenuse) / (m_prefabStartSpeed[weaponIndex] * m_prefabStartSpeed[weaponIndex])) * Mathf.Rad2Deg);
        if (float.IsNaN(angle))
        {
            angle = 0;
        }
        */
        Vector3 dir = targetPos - startPos;

        dir.y = Mathf.Tan(angle * Mathf.Deg2Rad) * deltaX;

        return dir;
    }

    private Vector3 getRandomPosOnGeomatry(Vector3 origin, float distance)
    {
        Vector2 randomDir = new Vector2(Random.value - 0.5f, Random.value - 0.5f).normalized * Random.value * distance;

        Vector3 raycastOrigin = origin + new Vector3(randomDir.x, 2000, randomDir.y);

        RaycastHit hit;

        Physics.Raycast(raycastOrigin, Vector3.down, out hit);

        return hit.point;
    }

    public int getShotSoundIndex(int weaponIndex)
    {
        if (weaponIndex >= m_prefabShotSoundIndex.Count || weaponIndex < 0)
        {
            Debug.LogWarning("ProjectileManager: getShotSoundIndex: weaponIndex out of range");
            return 0;
        }
        else
        {
            return m_prefabShotSoundIndex[weaponIndex];
        }
    }

    public void client_resetManager()
    {
        for (int i = m_active_projectiles.Count - 1; i > -1; i--)
        {
            recyleProjectile(i);
        }
    }

    public Vector3? getShotDirectionForTarget(int weaponPrefabIndex, Vector3 origin, Vector3 target)
    {
        Vector3 distanceVec = target - origin;
        Vector2 distanceXZ = new Vector2(distanceVec.x, distanceVec.z);
        float distanceX = distanceXZ.magnitude;

        List<float> angles = getShotAngleForTarget(m_prefabStartSpeed[weaponPrefabIndex], distanceX, distanceVec.y, m_prefabGravityFactor[weaponPrefabIndex] * Physics.gravity.magnitude);

        if (angles.Count > 0)
        {
            float minAngle = float.MaxValue;

            for (int i = 0; i < angles.Count; i++)
            {
                minAngle = Mathf.Min(angles[i], minAngle);
            }

            return new Vector3(distanceVec.x, distanceX * Mathf.Tan(minAngle), distanceVec.z).normalized;
        }
        else
        {
            return null;
        }
    }

    public List<float> getShotAngleForTarget(int weaponPrefabIndex, float x, float y)
    {
        return getShotAngleForTarget(m_prefabStartSpeed[weaponPrefabIndex], x, y, m_prefabGravityFactor[weaponPrefabIndex] * Physics.gravity.magnitude);
    }

    public static List<float> getShotAngleForTarget(float v, float x, float y, float g = 9.81f)
    {
        List<float> returnValue = new List<float>();

        if (g == 0 || x == 0)
        {
            return returnValue;
        }

        float root = Mathf.Sqrt(Mathf.Pow(v, 4) - g * (g * x * x + 2 * y * v * v));

        if (double.IsNaN(root))
        {
            return returnValue;
        }

        float solution1 = Mathf.Atan2(Mathf.Pow(v, 2) + root , g * x);
        float solution2 = Mathf.Atan2(Mathf.Pow(v, 2) - root , g * x);

        if (!float.IsNaN(solution1))
        {
            returnValue.Add(solution1);
        }

        if (!float.IsNaN(solution2))
        {
            returnValue.Add(solution2);
        }

        return returnValue;
    }
}
