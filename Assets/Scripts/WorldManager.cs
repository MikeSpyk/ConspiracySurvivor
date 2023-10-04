using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public class WorldManager : MonoBehaviour
{
    public enum PlaceholderTextureNames { Not_Set, Road }
    public enum vertexMapTextureNames { grass = 0, rock = 1, snow = 2, underwater = 3, beachsand = 4, deadGrass = 5, dirt = 6, forest = 7, beachGrassTransition = 8, road_trail = 9, mountain_steep = 10 } // UPDATE ALSO IN TerrainShaderFiller AND IN THE SHADER !!! and texuresCount in m_mainHeightmapRenderer
    private enum HeightMapRendererStage { CreateMeshes, MoveMeshes, FindMeshesToRemove, Wait }
    public enum HeightMapRendererRestartBehaviour { Instantly, AfterMeshDone }
    public enum CombineMethode { Addition, Max }

    public static WorldManager singleton = null;

    private struct fixedWorldPoints
    {
        public int x;
        public int y;
    }

    private const float SampleDistance = 1.1f; // distance between 2 sample point in perlin map
    private const int NumberOfThreadsSmoothing = 4; // number of threads used in world-calculating

    private const float distanceTerrainVertexDistance = 20f;
    private const float distanceTerrainY_Multiplier = 10f;
    private const int distanceTerrainVertexCountQuad = 151;

    private const int worldSubMeshDivider = 10; // divides the world into smaller meshes by this value
    private const int worldSubMeshVertexCountQuad = 12;
    private const float worldSubMeshVertexDistance = .5f;
    public enum randomRockRotIndex { around_Y_axis, no_rotation }

    #region EditorInputs

    [Header("Rocks")]
    [SerializeField] private bool m_createRocks = true;
    [SerializeField] private GameObject[] rocksPrefabs;
    [SerializeField] private int[] rocksBlockRadius;
    [SerializeField] private Vector3[] m_rocksOffset;

    [SerializeField] private int[] rocksPlaceStage;

    [SerializeField] private float[] rocksAngleMin; // remove
    [SerializeField] private Vector3[] rocksOrientations;
    [SerializeField] private Material rocksMaterial; // investigate
    [SerializeField] private randomRockRotIndex[] rocksRandomRotIndex; // remove
    [SerializeField] private bool hideRocksInHierarchy = true;

    [SerializeField] private float rocksDistance = 750;
    [SerializeField] private int rockSpawningRemovingIterations = 1000;
    [SerializeField] private int rocksCalcIterations = 1000;

    [SerializeField] private int m_rocksMountainBlockRadius = 100;
    [SerializeField] private float m_rocksMinThreshold = 1f;
    [SerializeField] private float[] m_rocksStageMin;
    [SerializeField] private float m_rocksRandomAmplitude = 1f;
    [SerializeField] private float m_rocksRandomFreqency = 0.1f;
    [SerializeField] private int m_rocksSmoothCount = 1;

    [SerializeField] private int DEBUG_rockTestIndex = -1;
    [SerializeField] private bool DEBUG_rockTestSpawn = false; 

    [Header("Terrain Objects")]
    [SerializeField] private GameObject[] terrainObjectsPrefabs;
    [SerializeField] private float[] terrainObjectsPrefabsYOffsets;
    [SerializeField] private bool hideTerrainObjsInspector = true;
    [SerializeField] private float terrainObjectsMaxRenderDistance = 750f;
    [SerializeField] private int maxCostTerrainRendering = 1000;
    [Header("Bushes")]
    [SerializeField] private float bushProbabilityPercentBase = 1f;
    [SerializeField] private int bushBlockDistance = 10;
    [Header("Trees")]
    [SerializeField] private float treeRenderDistanceMax = 1000f;
    [SerializeField] private Mesh distantTreeMesh;
    [SerializeField] private Material[] distantTreeMaterials;
    [SerializeField] private float[] distantTreeOffsetY;
    [SerializeField] private float[] distantTreeSize;
    [SerializeField] private int maxDistantTreeManagementActionsPerFrame = 100;
    [SerializeField] private float timeBetweenTreeUpdates = .1f;
    [SerializeField] private GameObject[] treePrefabs;
    [SerializeField] private bool destroyTrees = true;
    [SerializeField] private bool hide3DTreesHierarchy = true;
    [SerializeField] private bool m_createTreeObjects;
    [Header("World-Building")]
    [SerializeField] public AnimationCurve mountainExtendFalloff;
    [SerializeField] private AnimationCurve beachTerrainHeightModifier;
    [SerializeField] private int[] MonumentSpaces; // radius
    [SerializeField] private float[] maxMonumentAngle;
    [SerializeField] private int minMonumentDistanceToBeach = 10;
    [SerializeField] private int monumentSmoothDistance = 10;
    [SerializeField] private int monumentSmoothStages = 1;
    [SerializeField] private int monumentIterationStepLength = 1;
    [SerializeField] private int roadAStarDistance = 2; // 2,4,8,16....
    [SerializeField] private AnimationCurve roadTerrainAngleCost;
    [SerializeField] private float roadsOverlayCostReduction = 2; // should a new road rather join a already avaible road or go its own way
    [SerializeField] private bool noRocks = false;
    [SerializeField] private bool noMonuments = false;
    [SerializeField] private bool noRoads = false;
    [SerializeField] private ComputeShader computeShaderWorldGeneration;
    [SerializeField] private int computeShaderPseudoCurvePrecision = 10;
    [SerializeField] private AnimationCurve m_distanceToWorldEgdeFactor;
    [SerializeField] private float m_distanceToWorldEgdeSummand;
    [SerializeField] private AnimationCurve m_distanceToWorldEgdeSummandCurve;
    [Header("Worldbuilding Mountains")]
    [SerializeField] private int m_mountainsSizePower;
    [SerializeField] private int m_mountainDensityMin;
    [SerializeField] private int m_mountainDensityMax;
    [SerializeField] private float m_minDistanceBetweenMountains;
    [SerializeField] private float m_mountainHeight;
    [SerializeField] private AnimationCurve m_mountainRoughnessStages;
    [SerializeField] private bool m_createMountains;
    [SerializeField] private CombineMethode m_mountainCombineMethode;
    [Header("Worldbuilding Octaves")]
    [SerializeField] private AnimationCurve[] m_worldOctaves_curves;
    [SerializeField] private float[] m_worldOctaves_frequencies;
    [SerializeField] private float[] m_worldOctaves_amplitudes;
    [SerializeField] private int[] m_worldOctaves_smoothCount;
    [SerializeField] private int[] m_worldOctaves_scaleCount;
    [SerializeField] private int m_worldOctaved_startIndex = 0; // to exclude certain octaves
    [SerializeField] private int m_worldOctaved_endIndex = int.MaxValue; // to exclude certain octaves
    [Header("Worldbuilding POIs")]
    [SerializeField] private GameObject[] m_POI_gameobjects;
    [SerializeField] private AnimationCurve m_POI_blurCurve;
    [SerializeField] private float m_POI_minHeight;
    [Header("Worldbuilding Forests")]
    [SerializeField] private bool m_spawnForests = true;
    [SerializeField] private AnimationCurve m_forestsOctave_curves;
    [SerializeField] private float m_forestsOctave_frequency;
    [SerializeField] private float m_minForestsAmplitude;
    [Header("Rendering Terrain Meshes")]
    [SerializeField] private GameObject[] m_WorldTerrainMeshPrefab;
    [SerializeField] private GameObject m_WorldTerrainMeshColliderPrefab;
    [SerializeField] private int[] m_TerrainMeshes_stagesEdgeLength;
    [SerializeField] private int[] m_TerrainMeshes_stagesDiagonalShift;
    [SerializeField] private int m_TerrainMeshes_vertexEdgeLength = 13;
    [SerializeField] private int m_TerrainMeshes_maxMeshesToCreate; // per frame
    [SerializeField] private int m_TerrainMeshes_gridDistance = 1;
    [SerializeField] private float m_TerrainMeshes_maxMeshDistance;
    [SerializeField] private float m_TerrainMeshes_minDistanceToRedraw; // how much can the player move from the current Renderer middle position, before the terrain gets recalculated 
    [SerializeField] private bool m_TerrainMeshes_RendererActive = true;
    [SerializeField] private bool m_TerrainMeshes_ForceRender = false; // ignore player distance to move
    [SerializeField] private bool m_TerrainMeshes_ForceRenderOnce = false;
    [SerializeField] private bool m_TerrainMeshes_hideHierarchy = true;
    [SerializeField] private float m_TerrainMeshes_BackgroundLoading_heightDelta = 100f;
    [SerializeField] private HeightMapRendererRestartBehaviour m_TerrainMeshes_RestartBehaviour = HeightMapRendererRestartBehaviour.Instantly;
    [SerializeField] private int m_playerRadiusTerrainColliderMesh = 57;
    [SerializeField] private float m_minDistanceTerrainColliderUpdate = 10f;
    [SerializeField] private bool m_serverCollidersActice = true; // will the server create colliders around external clients ?
    [SerializeField] private bool m_serverColliderForLocalClient = false;
    [SerializeField] private int m_maxMeshesToCreateServer = 10; // per frame
    [SerializeField] private bool m_TerrainMeshes_hideHierarchyServer = true;
    [SerializeField] private float[] m_textureLODDistances; // obsolete
    [Header("Harvestable Resources")]
    [SerializeField] private float m_barrelRespawnTime = 60f;
    [SerializeField] private float m_timeTreeRegrow = 10f;
    [SerializeField] private float m_treeDistance = 20f; // min distance between 2 trees
    [SerializeField, Range(0f, 1f)] private float m_treeDensity = 1;
    [Header("Terrain Texture")]
    [SerializeField] private float m_moistureFrequency = 1;
    [SerializeField] private float m_texUnderwaterHeight = 22f;
    [SerializeField] private float m_texBeachHeight = 26f;
    [SerializeField] private float m_texBeachTrasitionHeight = 27f;
    [SerializeField] private float m_lowAltitudeHeight = 40;
    [SerializeField] private float m_mediumAltitudeHeight = 60;
    [SerializeField] private float m_highAltitudeHeight = 80;
    [SerializeField] private float m_snowHeightMin = 90;
    [SerializeField] private float m_snowHeightMax = 150;
    [SerializeField] private float m_texAngleMinMountain = 27f;
    [SerializeField] private float m_texAngleMinSteepMountain = 42f;
    [SerializeField] private float m_texAngleMinBeachTransition = 20f;
    [Header("Miscellaneous")]
    [SerializeField] private bool showSpawnPointsOnce = false;
    [SerializeField] private int playerDistanceToWaterMaxIterationsExtend = 1; // this should be the world-distance of the beach ambient sound / vertex-distance in submeshes
    [SerializeField] private float fertilityAngleFactor = 1;
    [SerializeField] private float fertilityHeightFactor = 1;
    [SerializeField] private float deadOctaveFertilityFactor = 1;
    [SerializeField] private AnimationCurve fertilityHeight;
    [SerializeField] private float m_snowDeltaHeightWeight;
    [SerializeField] private AnimationCurve m_snowDeltaCurve;
    [SerializeField] private float fertilityBlurStrength = 1;
    [SerializeField] private float m_frequencyDeadFertility;
    [SerializeField] private float m_amplitudeDeadFertility;
    [SerializeField] private int m_smoothCountDeadFertility;
    [SerializeField] private int m_DownscalingCountDeadFertility;
    [SerializeField] private AnimationCurve m_CurveDeadFertility;
    [Header("test stuff")]
    [SerializeField] public AnimationCurve diamondHeightCurve;
    [SerializeField] public int diamondFalloffPointsCount = 100;
    [SerializeField] private bool stupidUpdateCoroutines = true;
    [SerializeField] private bool m_DEBUG_loadHeightmapFromVoronoi = false;
    [SerializeField] private float m_DEBUG_heightmapOffsetY = 20;

    #endregion

    #region Variables

    private float m_lastTimeBarrelSpawned = 0;

    private WorldRasterStack m_rasterMainStack = null;
    private Dictionary<int, WorldRasterField> m_rasterMainStackFields = null;
    private float m_lastTimeTreeGrow = 0f;

    private PlaceholderTextureNames[,] m_placeholderTextures;

    private Vector3[][] TerrainObjPositions = null;

    private List<GameObject>[] free3DTreeObjs;
    public List<GameObject> WorldViewpointsList;

    private float[] diamondFalloffPoints;

    private float lastTimeSubmeshUpdate = 0;
    private float lastTimeTreeUpdate = 0;

    private int worldVertexCountEdge; // will get set by the buildWorld-Methode

    private GameObject distanceTerrainObj;

    private Vector3[][] rocksPositions;
    private Quaternion[][] rocksRotations;
    private List<GameObject>[] activeRocksObj;
    private List<GameObject>[] freeRocksObj;

    private HeightmapRenderer m_mainHeightmapRenderer; // on client
    private List<WorldMeshData> m_readyWorldMeshData = new List<WorldMeshData>();

    private HeightmapRendererCollider m_serverHeightmapColliderComputer;
    private List<Vector2> playerPositionsLastColliderUpdate = null;
    private List<WorldMeshData> m_serverColliderMeshData = new List<WorldMeshData>();

    /// <summary>
    /// Lists, that hold the positions of the different tree types
    /// </summary>
    private List<Vector3>[] TreePositionsLists;
    private List<GameObject> ActiveTreesCloseList = new List<GameObject>();
    private List<Vector3>[] TreesDistantLists = null;
    private List<GameObject> submeshesList;
    private Matrix4x4[][][] matricesDistantTrees = null;
    private Matrix4x4[][][] tempMatricesDistantTrees = null; // used to save the tree data when the coroutine is running

    private ShortCompressedFloat VertexHeightMapQuality1 = null;
    private byte[,] vertexTextureMapQuality1 = null;

    public float resourceTreeDistance
    {
        get
        {
            return m_treeDistance;
        }
    }

    public float resourceTreeDensity
    {
        get
        {
            return m_treeDensity;
        }
    }

    public float seed
    {
        get { return currentSeed; }
    }
    private float currentSeed;
    public float size
    {
        get { return currentSize; }
    }
    private int currentSize;
    private bool m_startWorldBuildAsServer = false;

    private fixedWorldPoints[] fixedPoints;

    private bool treesInitialSetupPending = true;

    public bool worldBuildDone
    {
        get
        {
            return m_worldBuildDone;
        }
    }
    private bool m_worldBuildDone = false;

    private List<Gameobject_WorldMesh> m_terrainMeshObjects_Active = new List<Gameobject_WorldMesh>(); // enabled and visible
    private List<Gameobject_WorldMesh>[] m_terrainMeshObjects_Free = null;// cache
    private List<Gameobject_WorldMesh> m_terrainMeshObjects_ToBeMovedActive = new List<Gameobject_WorldMesh>(); // created but invisible
    private List<Gameobject_WorldMesh> m_terrainMeshObjects_ToBeRemoved = new List<Gameobject_WorldMesh>(); // visible and to be removed
    private bool m_terrainMeshObjects_MeshCreatingDone = true;
    private HeightMapRendererStage m_terrainMeshObjects_RendererStage = HeightMapRendererStage.Wait;

    private Dictionary<int, WorldMeshColliderOnly> m_serverTerrainHash_object = new Dictionary<int, WorldMeshColliderOnly>();
    private List<WorldMeshColliderOnly> m_freeServerMeshObjects = new List<WorldMeshColliderOnly>();

    public List<Vector3> beachSpawnPoint
    {
        get { return m_beachSpawnPoints.ToArray().ToList(); }
    }
    private List<Vector3> m_beachSpawnPoints = new List<Vector3>();

    private bool buildWorldRunning = false;

    #endregion

    public void setMaxRenderDistance(float newDistance)
    {
        m_TerrainMeshes_maxMeshDistance = newDistance;
        m_TerrainMeshes_ForceRenderOnce = true;
    }

    void Awake()
    {
        m_terrainMeshObjects_Free = new List<Gameobject_WorldMesh>[m_WorldTerrainMeshPrefab.Length]; // cache

        for(int i = 0; i < m_terrainMeshObjects_Free.Length; i++)
        {
            m_terrainMeshObjects_Free[i] = new List<Gameobject_WorldMesh>();
        }

        TerrainObjPositions = new Vector3[terrainObjectsPrefabs.Length][];
        activeTerrainObjs = new List<GameObject>[terrainObjectsPrefabs.Length];
        freeTerrainObjects = new List<GameObject>[terrainObjectsPrefabs.Length];

        for (int i = 0; i < terrainObjectsPrefabs.Length; i++)
        {
            activeTerrainObjs[i] = new List<GameObject>();
            freeTerrainObjects[i] = new List<GameObject>();
        }

        TreePositionsLists = new List<Vector3>[treePrefabs.Length];
        TreesDistantLists = new List<Vector3>[treePrefabs.Length];

        for (int i = 0; i < TreePositionsLists.Length; i++)
        {
            TreePositionsLists[i] = new List<Vector3>();
            TreesDistantLists[i] = new List<Vector3>();
        }

        freeRocksObj = new List<GameObject>[rocksPrefabs.Length];
        for (int i = 0; i < freeRocksObj.Length; i++)
        {
            freeRocksObj[i] = new List<GameObject>();
        }

        activeRocksObj = new List<GameObject>[rocksPrefabs.Length];
        for (int i = 0; i < activeRocksObj.Length; i++)
        {
            activeRocksObj[i] = new List<GameObject>();
        }

        matricesDistantTrees = new Matrix4x4[treePrefabs.Length][][];
        tempMatricesDistantTrees = new Matrix4x4[treePrefabs.Length][][];

        WorldViewpointsList = new List<GameObject>();
        singleton = this;

        free3DTreeObjs = new List<GameObject>[treePrefabs.Length];
        for (int i = 0; i < free3DTreeObjs.Length; i++)
        {
            free3DTreeObjs[i] = new List<GameObject>();
        }

        diamondFalloffPoints = new float[diamondFalloffPointsCount];

        for (int i = 0; i < diamondFalloffPointsCount; i++)
        {
            diamondFalloffPoints[i] = diamondHeightCurve.Evaluate((float)i / diamondFalloffPointsCount);
        }

        updateRockArrays();
    }

    void Start()
    {
#if UNITY_EDITOR
        m_TerrainMeshes_maxMeshesToCreate = 2;
        Debug.LogWarning("WorldManager: setting m_TerrainMeshes_maxMeshesToCreate to 2");
#endif

        IEnumerator tempIEnum = calculatePlayerDistanceToWater();
        StartCoroutine(tempIEnum);
    }

    private bool rockManagementRunning = false;
    private bool treesDistanceManagementRunning = false;
    private bool feedbackAfterNextRendererPass = false;
    private bool feedbackThisRendererPass = false;
    private enum UpdateIEnumeratorPosition { submeshManaging, treeCloseManagement, treesDistanceManagement, rockManagement, terrainObjManagement, done }
    private UpdateIEnumeratorPosition updateIEnumeratorPosition = UpdateIEnumeratorPosition.done;

    private GameObject DEBUG_lastTestRock = null;

    void Update()
    {
        if (showSpawnPointsOnce)
        {
            showSpawnPointsOnce = false;
            foreach (Vector3 pos in m_beachSpawnPoints)
            {
                Debug.DrawRay(pos, Vector3.up * 100f, Color.red, 30f);
            }
            Debug.Log("" + m_beachSpawnPoints.Count + " Beach-Spawn-Points drawn");
        }

        if (m_worldBuildDone)
        {
            terrainMeshesUpdate();

            if (stupidUpdateCoroutines)
            {
                switch (updateIEnumeratorPosition)
                {
                    case UpdateIEnumeratorPosition.done:
                        {
                            if (feedbackThisRendererPass)
                            {
                                //onEnvironmentRendererDone();
                                feedbackThisRendererPass = false;
                            }
                            if (feedbackAfterNextRendererPass)
                            {
                                feedbackAfterNextRendererPass = false;
                                feedbackThisRendererPass = true;
                            }
                            treesDistanceManagementRunning = false;
                            rockManagementRunning = false;
                            //Debug.Log("UpdateIEnumeratorPosition.done " + Time.time);
                            takeCurrentWorldViewpointsSnapshot();
                            updateIEnumeratorPosition = UpdateIEnumeratorPosition.treeCloseManagement;
                            break;
                        }
                    case UpdateIEnumeratorPosition.treeCloseManagement:
                        {
                            if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
                            {
                                //Debug.Log("UpdateIEnumeratorPosition.treeCloseManagement " + Time.time);
                                treesCloseManagement();
                            }
                            updateIEnumeratorPosition = UpdateIEnumeratorPosition.treesDistanceManagement;

                            break;
                        }
                    case UpdateIEnumeratorPosition.treesDistanceManagement:
                        {
                            //Debug.Log("UpdateIEnumeratorPosition.treesDistanceManagement " + Time.time);
                            if (!treesDistanceManagementRunning)
                            {
                                IEnumerator treeDistantIEum = treesDistantManagement();
                                StartCoroutine(treeDistantIEum);
                                treesDistanceManagementRunning = true;
                            }
                            break;
                        }
                    case UpdateIEnumeratorPosition.rockManagement:
                        {
                            if (!rockManagementRunning)
                            {
                                //IEnumerator rockCalcIEnum = rockManagement();
                                //StartCoroutine(rockCalcIEnum);
                                //rockManagementRunning = true;

                                updateIEnumeratorPosition = UpdateIEnumeratorPosition.terrainObjManagement;
                                rockManagementRunning = false;
                            }
                            break;
                        }
                    case UpdateIEnumeratorPosition.terrainObjManagement:
                        {
                            if (!terrainObjRenderingRunnig)
                            {
                                //IEnumerator terrainObjIEnum = terrainObjRenderingCoroutine();
                                //StartCoroutine(terrainObjIEnum);
                                terrainObjRenderingRunnig = false; // TEST
                                updateIEnumeratorPosition = UpdateIEnumeratorPosition.done; // TEST
                            }
                            break;
                        }
                    default:
                        {
                            Debug.LogError("unkown index");
                            break;
                        }
                }

                //subMeshManaging();
                drawMeshesInstancedTrees();
            }
        }

        if (DEBUG_rockTestSpawn)
        {
            if(DEBUG_lastTestRock != null)
            {
                Destroy(DEBUG_lastTestRock);
            }

            Player_local player = EntityManager.singleton.getLocalPlayer();

            Vector3 originPos;
            Vector3 originDir;

            if (player == null)
            {
                originPos = Vector3.zero;
                originDir = Vector3.forward;
            }
            else
            {
                originPos = player.transform.position;
                originDir = player.transform.forward;
            }

            Vector3 spawnPosition = originPos + originDir * 3f;
            Vector3 spawnPositionOffset = spawnPosition + m_rocksOffset[DEBUG_rockTestIndex];

            DEBUG_lastTestRock =Instantiate(rocksPrefabs[DEBUG_rockTestIndex], spawnPositionOffset, Quaternion.Euler(rocksOrientations[DEBUG_rockTestIndex]) * Quaternion.Euler(-90, 0, 0));

            Debug.DrawRay(spawnPosition, Vector3.right * rocksBlockRadius[DEBUG_rockTestIndex] * worldSubMeshVertexDistance, Color.red);
            Debug.DrawRay(spawnPosition, Vector3.left * rocksBlockRadius[DEBUG_rockTestIndex] * worldSubMeshVertexDistance, Color.red);
            Debug.DrawRay(spawnPosition, Vector3.forward * rocksBlockRadius[DEBUG_rockTestIndex] * worldSubMeshVertexDistance, Color.red);
            Debug.DrawRay(spawnPosition, Vector3.back * rocksBlockRadius[DEBUG_rockTestIndex] * worldSubMeshVertexDistance, Color.red);

            Debug.DrawLine(spawnPosition, spawnPositionOffset, Color.yellow);
        }

        resourcesUpdate();
    }

    void OnApplicationQuit()
    {
        if (m_mainHeightmapRenderer != null)
        {
            m_mainHeightmapRenderer.dispose();
            m_mainHeightmapRenderer = null;
        }

        if (m_serverHeightmapColliderComputer != null)
        {
            m_serverHeightmapColliderComputer.dispose();
            m_serverHeightmapColliderComputer = null;
        }
    }

    void OnDestroy()
    {
        if (m_mainHeightmapRenderer != null)
        {
            m_mainHeightmapRenderer.dispose();
            m_mainHeightmapRenderer = null;
        }

        if (m_serverHeightmapColliderComputer != null)
        {
            m_serverHeightmapColliderComputer.dispose();
            m_serverHeightmapColliderComputer = null;
        }
    }

    private void updateRockArrays()
    {
        List<GameObject> rocksPrefabsList = rocksPrefabs.ToList();
        List<int> rocksBlockRadiusList = rocksBlockRadius.ToList();
        List<Vector3> rocksOffsetList = m_rocksOffset.ToList();
        List<int> rocksPlaceStageList = rocksPlaceStage.ToList();
        List<Vector3> rocksOrientationsList = rocksOrientations.ToList();

        for(int i = 0; i < rocksPrefabsList.Count; i++)
        {
            if(rocksPrefabsList[i] == null)
            {
                rocksPrefabsList.RemoveAt(i);
                rocksBlockRadiusList.RemoveAt(i);
                rocksOffsetList.RemoveAt(i);
                rocksPlaceStageList.RemoveAt(i);
                rocksOrientationsList.RemoveAt(i);

                i--;
            }
        }

        rocksPrefabs = rocksPrefabsList.ToArray();
        rocksBlockRadius = rocksBlockRadiusList.ToArray();
        m_rocksOffset = rocksOffsetList.ToArray();
        rocksPlaceStage = rocksPlaceStageList.ToArray();
        rocksOrientations = rocksOrientationsList.ToArray();
    }

    private void resourcesUpdate()
    {
        if (m_startWorldBuildAsServer && m_rasterMainStack != null)
        {
            if (Time.time > m_lastTimeBarrelSpawned + m_barrelRespawnTime)
            {
                m_lastTimeBarrelSpawned = Time.time;
                m_rasterMainStack.addResourceToMostNeeded(FieldResources.ResourceType.RandomLootBarrel);
            }

            if (Time.time > m_lastTimeTreeGrow + m_timeTreeRegrow)
            {
                m_lastTimeTreeGrow = Time.time;
                m_rasterMainStack.addResourceToMostNeeded(FieldResources.ResourceType.Tree);
            }
        }
    }

    public List<byte> getGameSaveData()
    {
        List<byte> returnValue = new List<byte>();

        returnValue.AddRange(BitConverter.GetBytes(currentSeed));
        returnValue.AddRange(BitConverter.GetBytes(currentSize));

        return returnValue;
    }

    public int loadFromSaveData(byte[] data, int index)
    {
        float seed = BitConverter.ToSingle(data, index);
        index += 4;
        int size = BitConverter.ToInt32(data, index);
        index += 4;

        startBuildWorld(seed, size, true, false);

        return index;
    }

    public float getDefaultSubmeshVertDistance()
    {
        return worldSubMeshVertexDistance;
    }

    public void startBuildWorld(float seed, int verticesPerCountEdge, bool isServer, bool spawnAllResources)
    {
        if (!isServer && seed == currentSeed && verticesPerCountEdge == currentSize)
        {
            // keep old world

            m_worldBuildDone = true;
            buildWorldRunning = false;
            SoundManager.singleton.onWorldBuildDone(worldVertexCountEdge * worldSubMeshVertexDistance);
            GameManager_Custom.singleton.onWorldBuilderLoadMapDone();

            GUIManager.singleton.setGUILoadingProgressText("worldbuilding done");
        }
        else
        {
            m_startWorldBuildAsServer = isServer;

            if (buildWorldRunning)
            {
                Debug.LogWarning("trying to start a worldbuilding-process while another one is still running");
            }
            else
            {
                GUIManager.singleton.setGUILoadingActive();
                worldVertexCountEdge = verticesPerCountEdge;
                currentSeed = seed;
                currentSize = verticesPerCountEdge;

                IEnumerator woldBuildingCoroutine = buildWorld();
                StartCoroutine(woldBuildingCoroutine);
            }

            treesInitialSetupPending = spawnAllResources;
        }
    }

    private void terrainMeshesUpdate()
    {
        if (!m_TerrainMeshes_RendererActive)
        {
            return;
        }

        if (m_mainHeightmapRenderer != null) // client meshes
        {
            if (m_mainHeightmapRenderer.isDone && m_terrainMeshObjects_MeshCreatingDone)
            {
                if (
                    m_TerrainMeshes_RestartBehaviour == HeightMapRendererRestartBehaviour.Instantly ||
                    (m_TerrainMeshes_RestartBehaviour == HeightMapRendererRestartBehaviour.AfterMeshDone && m_terrainMeshObjects_RendererStage != HeightMapRendererStage.Wait)
                    )
                {
                    // restart heightmapRender thread if necessary

                    Vector3 viewPointPos = PlayerManager.singleton.getWorldViewPoint(-1).transform.position;
                    Vector2Int heightmapPosition = new Vector2Int((int)(viewPointPos.x / worldSubMeshVertexDistance), (int)(viewPointPos.z / worldSubMeshVertexDistance));

                    float residualX = ((float)heightmapPosition.x - (heightmapPosition.x / m_TerrainMeshes_gridDistance) * m_TerrainMeshes_gridDistance) / m_TerrainMeshes_gridDistance;
                    float residualY = ((float)heightmapPosition.y - (heightmapPosition.y / m_TerrainMeshes_gridDistance) * m_TerrainMeshes_gridDistance) / m_TerrainMeshes_gridDistance;

                    // rounded down
                    heightmapPosition = new Vector2Int((heightmapPosition.x / m_TerrainMeshes_gridDistance) * m_TerrainMeshes_gridDistance, (heightmapPosition.y / m_TerrainMeshes_gridDistance) * m_TerrainMeshes_gridDistance);

                    if (residualX > 0.5f)
                    {
                        heightmapPosition.x += m_TerrainMeshes_gridDistance;
                    }
                    if (residualY > 0.5f)
                    {
                        heightmapPosition.y += m_TerrainMeshes_gridDistance;
                    }

                    if (Vector2Int.Distance(m_mainHeightmapRenderer.currentPosition, heightmapPosition) > m_TerrainMeshes_minDistanceToRedraw || m_TerrainMeshes_ForceRender || m_TerrainMeshes_ForceRenderOnce)
                    {
                        //Debug.Log("m_mainHeightmapRenderer started ");
                        //float startTime = Time.realtimeSinceStartup;
                        m_mainHeightmapRenderer.start(heightmapPosition, m_TerrainMeshes_maxMeshDistance, m_TerrainMeshes_stagesEdgeLength, m_TerrainMeshes_stagesDiagonalShift, m_textureLODDistances);
                        //Debug.Log("m_mainHeightmapRenderer.start: " + (Time.realtimeSinceStartup - startTime));
                        m_terrainMeshObjects_MeshCreatingDone = false;
                        m_TerrainMeshes_ForceRenderOnce = false;
                    }
                }
            }

            switch (m_terrainMeshObjects_RendererStage)
            {
                case HeightMapRendererStage.CreateMeshes:
                    {
                        //float startTime = Time.realtimeSinceStartup;

                        m_readyWorldMeshData.AddRange(m_mainHeightmapRenderer.collectReadyWorldMeshData());

                        for (int i = 0; m_readyWorldMeshData.Count > 0 && i < m_TerrainMeshes_maxMeshesToCreate; i++)
                        {
                            createTerrainMeshObject(m_readyWorldMeshData[0]);
                            m_readyWorldMeshData.RemoveAt(0);
                        }

                        if (m_mainHeightmapRenderer.isDone && m_readyWorldMeshData.Count == 0 && m_mainHeightmapRenderer.readyWorldMeshDataCount == 0)
                        {
                            m_terrainMeshObjects_MeshCreatingDone = true;
                            m_terrainMeshObjects_RendererStage = HeightMapRendererStage.MoveMeshes;
                        }

                        //Debug.Log("CreateMeshes Time: " + (Time.realtimeSinceStartup - startTime));

                        break;
                    }
                case HeightMapRendererStage.MoveMeshes: // move terrain loaded in background up und move current terrain out of sight below und make it invisible
                    {
                        //float startTime = Time.realtimeSinceStartup;

                        UnityEngine.Profiling.Profiler.BeginSample("activate");

                        while (m_terrainMeshObjects_ToBeMovedActive.Count > 0)
                        {
                            // m_terrainMeshObjects_ToBeActivated[0].m_gameObject.SetActive(true);

                            //m_terrainMeshObjects_ToBeMovedActive[0].m_gameObject.transform.position = m_terrainMeshObjects_ToBeMovedActive[0].m_gameObject.transform.position + Vector3.up * m_TerrainMeshes_BackgroundLoading_heightDelta;
                            m_terrainMeshObjects_ToBeMovedActive[0].m_World_Mesh.unhide_TerrainCreation();

                            m_terrainMeshObjects_Active.Add(m_terrainMeshObjects_ToBeMovedActive[0]);
                            m_terrainMeshObjects_ToBeMovedActive.RemoveAt(0);
                        }
                        UnityEngine.Profiling.Profiler.EndSample();
                        UnityEngine.Profiling.Profiler.BeginSample("remove");

                        while (m_terrainMeshObjects_ToBeRemoved.Count > 0)
                        {
                            m_terrainMeshObjects_Free[m_terrainMeshObjects_ToBeRemoved[0].m_World_Mesh.m_worldMeshData.m_LODLevel].Add(m_terrainMeshObjects_ToBeRemoved[0]);
                            //m_terrainMeshObjects_ToBeRemoved[0].m_gameObject.SetActive(false);

                            m_terrainMeshObjects_ToBeRemoved[0].m_World_Mesh.hide_TerrainCreation();
                            //m_terrainMeshObjects_ToBeRemoved[0].m_World_Mesh.setVisibility(false);
                            //m_terrainMeshObjects_ToBeRemoved[0].m_gameObject.transform.position = m_terrainMeshObjects_ToBeRemoved[0].m_gameObject.transform.position + Vector3.down * m_TerrainMeshes_BackgroundLoading_heightDelta;

                            m_mainHeightmapRenderer.recyleWorldMeshData(m_terrainMeshObjects_ToBeRemoved[0].m_World_Mesh.m_worldMeshData);
                            m_terrainMeshObjects_ToBeRemoved[0].m_World_Mesh.m_worldMeshData = null;
                            m_terrainMeshObjects_ToBeRemoved.RemoveAt(0);
                        }
                        UnityEngine.Profiling.Profiler.EndSample();

                        m_terrainMeshObjects_RendererStage = HeightMapRendererStage.FindMeshesToRemove;

                        onWorldMeshUpdated();

                        //Debug.Log("MoveMeshes Time: " + (Time.realtimeSinceStartup - startTime));

                        break;
                    }
                case HeightMapRendererStage.FindMeshesToRemove:
                    {
                        //float startTime = Time.realtimeSinceStartup;

                        for (int i = 0; i < m_terrainMeshObjects_Active.Count; i++)
                        {
                            m_terrainMeshObjects_ToBeRemoved.Add(m_terrainMeshObjects_Active[i]);
                        }

                        m_terrainMeshObjects_Active.Clear();

                        m_terrainMeshObjects_RendererStage = HeightMapRendererStage.Wait;

                        //Debug.Log("FindMeshesToRemove Time: " + (Time.realtimeSinceStartup - startTime));

                        break;
                    }
                case HeightMapRendererStage.Wait:
                    {
                        if (!m_mainHeightmapRenderer.isDone || m_mainHeightmapRenderer.readyWorldMeshDataCount > 0)
                        {
                            m_terrainMeshObjects_RendererStage = HeightMapRendererStage.CreateMeshes;
                        }
                        break;
                    }
                default:
                    {
                        Debug.LogError("WorldManager: Unknown Enum-Index: " + m_terrainMeshObjects_RendererStage.ToString());
                        break;
                    }
            }
        }

        if (m_serverHeightmapColliderComputer != null) // server collider
        {
            List<WorldMeshData> newMeshData = m_serverHeightmapColliderComputer.getAvailableWorldMeshData();

            if (newMeshData != null)
            {
                m_serverColliderMeshData.AddRange(newMeshData);
            }
            //Debug.Log("WorldManager: terrainMeshesUpdate: create: " + newMeshData.Count);

            int createdServerColliders = 0;

            while (m_serverColliderMeshData.Count > 0)
            {
                if (createdServerColliders > m_maxMeshesToCreateServer)
                {
                    break;
                }

                if (m_serverTerrainHash_object.ContainsKey(m_serverColliderMeshData[0].m_ID))
                {
                    Debug.LogWarning("WorldManager: terrainMeshesUpdate: received a new mesh with the same ID as an already existing one: " + m_serverColliderMeshData[0].m_ID);
                    m_serverHeightmapColliderComputer.recyleWorldMeshData(m_serverColliderMeshData[0]);
                }
                else
                {
                    WorldMeshColliderOnly newTerrainMeshObj;

                    if (m_freeServerMeshObjects.Count > 0)
                    {
                        newTerrainMeshObj = m_freeServerMeshObjects[0];
                        m_freeServerMeshObjects.RemoveAt(0);
                        newTerrainMeshObj.gameObject.SetActive(true);
                    }
                    else
                    {
                        GameObject terrainObj = Instantiate(m_WorldTerrainMeshColliderPrefab);
                        newTerrainMeshObj = terrainObj.GetComponent<WorldMeshColliderOnly>();
                    }

                    if (m_TerrainMeshes_hideHierarchyServer)
                    {
                        newTerrainMeshObj.gameObject.hideFlags = HideFlags.HideInHierarchy;
                    }
                    else
                    {
                        newTerrainMeshObj.gameObject.hideFlags = HideFlags.None;
                    }

                    newTerrainMeshObj.initialize(m_serverColliderMeshData[0]);

                    m_serverTerrainHash_object.Add(m_serverColliderMeshData[0].m_ID, newTerrainMeshObj);
                }

                //m_serverTerrainPosition_object.Add(new Vector2(newMeshData[i].m_objectWorldPosition.x, newMeshData[i].m_objectWorldPosition.z), terrainMeshObj);

                m_serverColliderMeshData.RemoveAt(0);
                createdServerColliders++;
            }

            List<int> removeMeshHashes = m_serverHeightmapColliderComputer.getMeshesToRemoveHash();

            if (removeMeshHashes != null)
            {
                //Debug.Log("WorldManager: terrainMeshesUpdate: remove: " + removeMeshHashes.Count);

                for (int i = 0; i < removeMeshHashes.Count; i++)
                {
                    WorldMeshColliderOnly currentObj;

                    if (m_serverTerrainHash_object.TryGetValue(removeMeshHashes[i], out currentObj))
                    {
                        m_freeServerMeshObjects.Add(currentObj);
                        currentObj.transform.position = new Vector3(0, 0, 0);
                        currentObj.gameObject.SetActive(false);
                        currentObj.gameObject.hideFlags = HideFlags.HideInHierarchy;
                        m_serverTerrainHash_object.Remove(removeMeshHashes[i]);
                        m_serverHeightmapColliderComputer.recyleWorldMeshData(currentObj.m_meshData);
                        currentObj.m_meshData = null;
                    }
                    else
                    {
                        Debug.LogWarning("WorldManager: terrainMeshesUpdate: could not find terrain-mesh to remove: " + removeMeshHashes[i]);
                    }
                }
            }

            if (m_serverHeightmapColliderComputer.isDone && m_serverColliderMeshData.Count < 1)
            {
                //Debug.Log("WorldManager: terrainMeshesUpdate: isDone: " + Time.realtimeSinceStartup);

                bool updateColliders = false;

                List<Vector2> playerPositions;

                if (m_serverColliderForLocalClient)
                {
                    playerPositions = PlayerManager.singleton.getAllViewPointsPositionsXZ();
                }
                else
                {
                    playerPositions = PlayerManager.singleton.getAllViewPointsPositionsExceptLocalPlayerXZ();
                }

                if (playerPositionsLastColliderUpdate == null)
                {
                    updateColliders = true;
                }
                else
                {
                    if (playerPositions.Count == playerPositionsLastColliderUpdate.Count)
                    {
                        for (int i = 0; i < playerPositions.Count; i++)
                        {
                            if (Vector2.Distance(playerPositions[i], playerPositionsLastColliderUpdate[i]) > m_minDistanceTerrainColliderUpdate)
                            {
                                updateColliders = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        updateColliders = true;
                    }
                }

                if (updateColliders)
                {
                    playerPositionsLastColliderUpdate = playerPositions;

                    m_serverHeightmapColliderComputer.setPlayerPositions(playerPositions);
                    m_serverHeightmapColliderComputer.setPlayerMeshRadius(m_playerRadiusTerrainColliderMesh);
                    m_serverHeightmapColliderComputer.startMeshCompute();
                    //Debug.Log("WorldManager: terrainMeshesUpdate: startMeshCompute: "+Time.realtimeSinceStartup);
                }
            }
        }
    }

    public void unregisterResource(FieldResources.ResourceType type, int fieldID, Vector2 position)
    {
        if (m_rasterMainStackFields.ContainsKey(fieldID))
        {
            m_rasterMainStackFields[fieldID].removeResource(type, position);
        }
        else
        {
            Debug.LogWarning("WorldManager: unregisterResource: fieldID out of bounds: " + fieldID);
        }
    }

    public void registerResource(FieldResources.ResourceType type, int fieldID, Vector2 position)
    {
        m_rasterMainStackFields[fieldID].registerResource(type, position);
    }

    public void DEBUG_spawnTree(Vector2 position, int fieldID = -1)
    {
        float worldPosX = position.x;
        float worldPosY = position.y;
        float height;
        float fertility;
        Vector3 normal;
        byte texIndex;

        int randomPrefabIndex = (int)Mathf.Min(UnityEngine.Random.value * treePrefabs.Length, treePrefabs.Length - 1);

        getHeightmapPointInfo(ref worldPosX, ref worldPosY, out height, out fertility, out normal, out texIndex);

        GameObject tree = Instantiate(treePrefabs[randomPrefabIndex], new Vector3(worldPosX, height - 0.2f, worldPosY), Quaternion.Euler(0, UnityEngine.Random.value * 360, 0));

        Entity_ResourceTree treeScript = tree.GetComponent<Entity_ResourceTree>();
        treeScript.m_rasterFieldID = fieldID;
    }

    public void spawnRandomLootContainer(Vector2 position, int fieldID = -1)
    {
        float worldPosX = position.x;
        float worldPosY = position.y;
        float height;
        float fertility;
        Vector3 normal;
        byte texIndex;

        getHeightmapPointInfo(ref worldPosX, ref worldPosY, out height, out fertility, out normal, out texIndex);

        GameObject barrel = EntityManager.singleton.spawnEntity(14, new Vector3(worldPosX, height, worldPosY));

        Entity_RandomLootBarrel barrelScript = barrel.GetComponent<Entity_RandomLootBarrel>();
        barrelScript.m_rasterFieldID = fieldID;
    }

    public void spawnBerryPlant(Vector2 position, int fieldID = -1)
    {
        float worldPosX = position.x;
        float worldPosY = position.y;
        float height;
        float fertility;
        Vector3 normal;
        byte texIndex;

        getHeightmapPointInfo(ref worldPosX, ref worldPosY, out height, out fertility, out normal, out texIndex);

        GameObject berryPlant = EntityManager.singleton.spawnEntity(19, new Vector3(worldPosX, height, worldPosY));

        StaticCollectable barrelScript = berryPlant.GetComponent<StaticCollectable>();
        barrelScript.m_rasterFieldID = fieldID;
    }

    private void createTerrainMeshObject(WorldMeshData meshData)
    {
        meshData.m_LODLevel = 0; // multiple lod levels due to different shaders are no longer needed since the new shader has LODs included

        Gameobject_WorldMesh tempObj;
        if (m_terrainMeshObjects_Free[meshData.m_LODLevel].Count > 0)
        {
            tempObj = m_terrainMeshObjects_Free[meshData.m_LODLevel][0];
            m_terrainMeshObjects_Free[meshData.m_LODLevel].RemoveAt(0);
            //tempObj.m_gameObject.transform.position = meshData.m_objectWorldPosition;
            //tempObj.m_World_Mesh.unhide_TerrainCreation();
        }
        else
        {
            tempObj = new Gameobject_WorldMesh();
            GameObject newGameobject = Instantiate(m_WorldTerrainMeshPrefab[meshData.m_LODLevel], meshData.m_objectWorldPosition, Quaternion.identity) as GameObject;

            tempObj.m_gameObject = newGameobject;
            tempObj.m_World_Mesh = newGameobject.GetComponent<World_Mesh>();
        }

        if (m_TerrainMeshes_hideHierarchy)
        {
            tempObj.m_gameObject.hideFlags = HideFlags.HideInHierarchy;
        }
        else
        {
            tempObj.m_gameObject.hideFlags = HideFlags.None;
        }

        m_terrainMeshObjects_ToBeMovedActive.Add(tempObj);

        //tempObj.m_gameObject.transform.position = Vector3.zero; // test
        tempObj.m_gameObject.transform.position = Vector3.down * m_TerrainMeshes_BackgroundLoading_heightDelta; // test

        tempObj.m_World_Mesh.initialize(meshData);
    }

    private IEnumerator buildWorld()
    {
        //----------------------------------------- CG Collect -----------------------------------------------------

        GUIManager.singleton.setGUILoadingProgressText("GC Collect");
        yield return null;
        System.GC.Collect();

        //----------------------------------------- Initialising -----------------------------------------------------

        EntityManager.singleton.onCreateWorld(worldVertexCountEdge * worldSubMeshVertexDistance, worldVertexCountEdge * worldSubMeshVertexDistance);

        buildWorldRunning = true;

        m_placeholderTextures = new PlaceholderTextureNames[worldVertexCountEdge, worldVertexCountEdge];
        for (int i = 0; i < worldVertexCountEdge; i++)
        {
            for (int j = 0; j < worldVertexCountEdge; j++)
            {
                m_placeholderTextures[i, j] = PlaceholderTextureNames.Not_Set;
            }
        }

        //----------------------------------------- world-heightmap -----------------------------------------------------

        GUIManager.singleton.setGUILoadingProgressText("World Heightmap: Setting Up");
        yield return null;

        int mountainDensity = m_mountainDensityMin + (int)((m_mountainDensityMax - m_mountainDensityMin) * RandomValuesSeed.strictNoise(currentSeed, currentSeed));

        long vertexCountEdgeLong = worldVertexCountEdge;

        int mountainCount = (int)((vertexCountEdgeLong * vertexCountEdgeLong) / 16777216) * mountainDensity; // 4096² = 16777216

        //Debug.Log("WorldManager: mountaionCount = " + mountainCount);
        yield return null;

        if (!m_createMountains)
        {
            mountainCount = 0;
        }

        WorldMountainBuilder myMountainBuilder = new WorldMountainBuilder(currentSeed, currentSeed, mountainCount, turbulenceCurve.keys, turbulenceAmplitude, m_mountainsSizePower, m_mountainRoughnessStages.keys);

        myMountainBuilder.start();

        List<WorldLandmassBuilderOctaveProperties> temp_octaves = new List<WorldLandmassBuilderOctaveProperties>();

        for (int i = m_worldOctaved_startIndex; i < m_worldOctaves_frequencies.Length && i <= m_worldOctaved_endIndex && i > -1; i++)
        {
            temp_octaves.Add(new WorldLandmassBuilderOctaveProperties(m_worldOctaves_frequencies[i], m_worldOctaves_amplitudes[i], currentSeed + currentSize * i, currentSeed + currentSize * i, m_worldOctaves_smoothCount[i],
                                                                        m_worldOctaves_curves[i].keys, m_worldOctaves_scaleCount[i]
                                                                        ));
        }

        WorldLandmassBuilder worldBuilder = new WorldLandmassBuilder(temp_octaves.ToArray(), worldVertexCountEdge);

        worldBuilder.start();

        VertexHeightMapQuality1 = new ShortCompressedFloat(worldVertexCountEdge, worldVertexCountEdge);

        GUIManager.singleton.setGUILoadingProgressText("World Heightmap: Octaves");
        yield return null;

        while (!worldBuilder.isDone)
        {
            yield return null;
        }

        GUIManager.singleton.setGUILoadingProgressText("World Heightmap: Border flatten and middle raise");
        yield return null;

        Vector2Int middlePoint = new Vector2Int(worldVertexCountEdge / 2, worldVertexCountEdge / 2);
        float maxDistanceToMid = Vector2Int.Distance(middlePoint, new Vector2Int(0, 0));
        float distanceToMid;

        for (int i = 0; i < worldBuilder.m_result.GetLength(0); i++)
        {
            for (int j = 0; j < worldBuilder.m_result.GetLength(1); j++)
            {
                distanceToMid = (1f - Vector2Int.Distance(new Vector2Int(i, j), middlePoint) / maxDistanceToMid);

                VertexHeightMapQuality1[i, j] = worldBuilder.m_result[i, j];
                VertexHeightMapQuality1[i, j] *= m_distanceToWorldEgdeFactor.Evaluate(distanceToMid); // lower terrain at edges
                VertexHeightMapQuality1[i, j] += m_distanceToWorldEgdeSummandCurve.Evaluate(distanceToMid) * m_distanceToWorldEgdeSummand; // raise terrain in the middle
            }
        }

        worldBuilder.dispose();

        GUIManager.singleton.setGUILoadingProgressText("World Heightmap: Finding highpoints");
        yield return null;

        List<HeightmapPosition> highpoints = ArrayTools.findHighpoints(VertexHeightMapQuality1, 10 + (int)Mathf.Pow(2, m_mountainsSizePower) / 2);

        //Debug.Log("WorldManager: highpoints count = " + highpoints.Count);

        List<HeightmapPosition> sortedHighpoints = highpoints.OrderBy(o => o.m_heightValue).ToList();
        highpoints.Clear();

        GUIManager.singleton.setGUILoadingProgressText("World Heightmap: Generating Mountains");
        yield return null;
        while (!myMountainBuilder.isDone)
        {
            yield return null;
        }

        List<Vector2Int> placedMountains = new List<Vector2Int>();

        bool pointTooClose;

        GUIManager.singleton.setGUILoadingProgressText("World Heightmap: Merging Mountains");
        yield return null;

        if (m_mountainCombineMethode == CombineMethode.Max)
        {
            List<float> TerrainPitsMinValues = new List<float>();

            for (int i = 0; i < myMountainBuilder.m_mountains.Count; i++)
            {
                int mountainSize = myMountainBuilder.m_mountains[i].GetLength(0);
                //float terrainSectorMin = ArrayTools.getMin(VertexHeightMapQuality1, worldVertexCountEdge, sortedHighpoints[sortedHighpoints.Count - 1].m_position - new Vector2Int(mountainSize / 2, mountainSize / 2), sortedHighpoints[sortedHighpoints.Count - 1].m_position + new Vector2Int(mountainSize / 2, mountainSize / 2));
                float terrainSectorMin = ArrayTools.getAverage(VertexHeightMapQuality1, sortedHighpoints[sortedHighpoints.Count - 1].m_position - new Vector2Int(mountainSize / 2, mountainSize / 2), sortedHighpoints[sortedHighpoints.Count - 1].m_position + new Vector2Int(mountainSize / 2, mountainSize / 2));
                TerrainPitsMinValues.Add(terrainSectorMin);
            }
            for (int i = 0; i < myMountainBuilder.m_mountains.Count; i++)
            {
                ArrayTools.createPit(VertexHeightMapQuality1, sortedHighpoints[sortedHighpoints.Count - 1].m_position, myMountainBuilder.m_mountains[i].GetLength(0) / 2, TerrainPitsMinValues[i]);
            }
        }

        for (int i = 0; i < myMountainBuilder.m_mountains.Count; i++)
        {
            pointTooClose = true;

            while (pointTooClose && sortedHighpoints.Count > 0)
            {
                pointTooClose = false;
                for (int j = 0; j < placedMountains.Count; j++)
                {
                    if (Vector2Int.Distance(placedMountains[j], sortedHighpoints[sortedHighpoints.Count - 1].m_position) < m_minDistanceBetweenMountains)
                    {
                        pointTooClose = true;
                        break;
                    }
                }
                if (pointTooClose)
                {
                    sortedHighpoints.RemoveAt(sortedHighpoints.Count - 1);
                }
            }

            if (sortedHighpoints.Count < 1)
            {
                Debug.LogWarning("WorldManager: Not enough highpoints to place mountains.");
                break;
            }

            //Debug.DrawRay(new Vector3(sortedHighpoints[sortedHighpoints.Count - 1].m_position.x, 0, sortedHighpoints[sortedHighpoints.Count - 1].m_position.y) * worldSubMeshVertexDistance, Vector3.up * 1000, Color.red, 100);
            ArrayTools.multiplicateArray(myMountainBuilder.m_mountains[i], m_mountainHeight);
            if (m_mountainCombineMethode == CombineMethode.Addition)
            {
                ArrayTools.combineArray(VertexHeightMapQuality1, myMountainBuilder.m_mountains[i], sortedHighpoints[sortedHighpoints.Count - 1].m_position);
            }
            else
            {
                ArrayTools.addToArrayMax(VertexHeightMapQuality1, myMountainBuilder.m_mountains[i], sortedHighpoints[sortedHighpoints.Count - 1].m_position);
            }
            placedMountains.Add(sortedHighpoints[sortedHighpoints.Count - 1].m_position);

            sortedHighpoints.RemoveAt(sortedHighpoints.Count - 1);

            yield return null;
        }

        myMountainBuilder.dispose();

        if (m_DEBUG_loadHeightmapFromVoronoi)
        {
            VoronoiMapMaker.singelton.compute();

            float[,] heightmap = VoronoiMapMaker.singelton.m_heightmap;

            int textureDimX = heightmap.GetLength(0);
            int textureDimY = heightmap.GetLength(1);

            Debug.Log("textureDimX: " + textureDimX);
            Debug.Log("textureDimY: " + textureDimY);

            for (int i = 0; i < textureDimX; i++)
            {
                for (int j = 0; j < textureDimY; j++)
                {
                    VertexHeightMapQuality1[i, j] = heightmap[i, j] + m_DEBUG_heightmapOffsetY;
                }
            }
        }

        //Debug.Log("WorldManager: size of heightmap-array: " + (sizeof(float) * worldVertexCountEdge * worldVertexCountEdge) + " byte");

        //----------------------------------------- Beaches -----------------------------------------------------

        GUIManager.singleton.setGUILoadingProgressText("smoothing beaches");
        yield return null;

        List<Vector2Int> beachPoints = new List<Vector2Int>();
        float beachHeightDiv = m_texBeachHeight - m_texUnderwaterHeight;

        for (int i = 0; i < worldVertexCountEdge; i++)
        {
            for (int j = 0; j < worldVertexCountEdge; j++)
            {
                if (VertexHeightMapQuality1[i, j] > m_texUnderwaterHeight && VertexHeightMapQuality1[i, j] < m_texBeachHeight)
                {
                    beachPoints.Add(new Vector2Int(i, j));
                }
            }
        }
        yield return null;
        for (int i = 0; i < beachPoints.Count; i++)
        {
            VertexHeightMapQuality1[beachPoints[i].x, beachPoints[i].y] = m_texUnderwaterHeight + beachTerrainHeightModifier.Evaluate((VertexHeightMapQuality1[beachPoints[i].x, beachPoints[i].y] - m_texUnderwaterHeight) / beachHeightDiv) * beachHeightDiv;
        }

        beachPoints.Clear();
        beachPoints = null;

        //----------------------------------------- POIs and Roads -----------------------------------------------------

        if (!noMonuments)
        {
            float monumentStartTime = Time.realtimeSinceStartup;

            GUIManager.singleton.setGUILoadingProgressText("Points Of Interest: Starting");
            yield return null;

            //----------------------------------------- POIs -----------------------------------------------------

            List<Vector2Int> roadConnectionPointsList = new List<Vector2Int>();

            // analyze given POIs

            PointOfInterest temp_POI;
            List<PointOfInterest> inputPOIs = new List<PointOfInterest>();
            int lowestSearchResolution = 1;

            for (int i = 0; i < m_POI_gameobjects.Length; i++)
            {
                if (m_POI_gameobjects[i].GetComponent<PointOfInterest>() == null)
                {
                    Debug.LogError("WorldManager: POI: Missing POI-Reference.");
                }
                else
                {
                    temp_POI = m_POI_gameobjects[i].GetComponent<PointOfInterest>();
                    inputPOIs.Add(temp_POI);

                    lowestSearchResolution = Mathf.Max(lowestSearchResolution, temp_POI.searchResolution);

                    if (worldVertexCountEdge % Mathf.Pow(2, temp_POI.searchResolution) != 0)
                    {
                        Debug.LogError("WorldManager: POI: Search Resolution not fitting. " + worldVertexCountEdge + "/" + Mathf.Pow(2, temp_POI.searchResolution) + " != 0");
                        while (true)
                        {
                            Debug.LogError("WorldManager: POI: waiting to get terminate by user");
                            Debug.Break();
                            yield return null;
                        }
                    }
                }
            }

            // create height-differences map and downscale them to fit the given POIs search-resolution

            GUIManager.singleton.setGUILoadingProgressText("Points Of Interest: Diff-Map");
            yield return null;
            DifferenceSingleCalculator diffCalc = new DifferenceSingleCalculator(0, 0, worldVertexCountEdge, worldVertexCountEdge, VertexHeightMapQuality1, m_POI_minHeight);
            diffCalc.start();

            while (!diffCalc.isDone)
            {
                yield return null;
            }

            MinMaxDiff[][,] diffMaps = new MinMaxDiff[lowestSearchResolution + 1][,];

            diffMaps[0] = diffCalc.m_result_MinMaxDiff;
            diffCalc.dispose();

            int downscaleEdgeLength = worldVertexCountEdge;

            for (int i = 1; i <= lowestSearchResolution; i++)
            {
                downscaleEdgeLength = worldVertexCountEdge / (int)Mathf.Pow(2, i);
                MaxDownscaleSingleCalculator downscaler = new MaxDownscaleSingleCalculator(0, 0, downscaleEdgeLength, downscaleEdgeLength, diffMaps[i - 1], downscaleEdgeLength * 2);
                downscaler.start();

                while (!downscaler.isDone)
                {
                    yield return null;
                }

                //Debug.Log("adding downscaled diff array: x: " + downscaler.m_result_MinMaxDiff.GetLength(0) + ", y: " + downscaler.m_result_MinMaxDiff.GetLength(1));

                diffMaps[i] = downscaler.m_result_MinMaxDiff;

                for (int j = 0; j < downscaleEdgeLength; j++)
                {
                    diffMaps[i][j, downscaleEdgeLength - 1].m_min = 0;
                    diffMaps[i][j, downscaleEdgeLength - 1].m_max = float.MaxValue;
                    diffMaps[i][j, downscaleEdgeLength - 1].m_diff = float.MaxValue;

                    diffMaps[i][downscaleEdgeLength - 1, j].m_min = 0;
                    diffMaps[i][downscaleEdgeLength - 1, j].m_max = float.MaxValue;
                    diffMaps[i][downscaleEdgeLength - 1, j].m_diff = float.MaxValue;
                }

                downscaler.dispose();
            }

            // find positions for POIs

            GUIManager.singleton.setGUILoadingProgressText("Points Of Interest: Finding places");
            yield return null;

            List<GameObject> placedMonuments = new List<GameObject>();
            List<PointOfInterest> placedMonumentsScript = new List<PointOfInterest>();

            for (int i = 0; i < inputPOIs.Count; i++)
            {
                int tempArrayLength = worldVertexCountEdge / (int)Mathf.Pow(2, inputPOIs[i].searchResolution);
                int toHighestResolution = (int)Mathf.Pow(2, inputPOIs[i].searchResolution);
                int totalRadius = inputPOIs[i].radius + inputPOIs[i].blurDistance + (int)inputPOIs[i].minDistanceToNextPOI;
                int totalPosX;
                int totalPosY;

                for (int j = 0; j < inputPOIs[i].maxOccurrence; j++)
                {
                    bool foundMonumentPlace = false;
                    bool tooClose = false;
                    int randomStartPosX = (int)RandomValuesSeed.perlinNoiseRanged(0, tempArrayLength, currentSeed + (i + j) * 11 * 1.23f, currentSeed + (i + j) * 21 * 1.23f);
                    int randomStartPosY = (int)RandomValuesSeed.perlinNoiseRanged(0, tempArrayLength, currentSeed + (i + j) * 12 * 1.23f, currentSeed + (i + j) * 22 * 1.23f);
                    //Debug.Log("searching in array " + inputPOIs[i].searchResolution + ", with array length " + tempArrayLength + ", at pos x: " + randomStartPosX + ", y: " + randomStartPosY);
                    bool startDirectionUp = RandomValuesSeed.getRandomBool(currentSeed + (i + j) * 12 * 1.23f);

                    for (int n = 0; n < 2; n++)
                    {
                        if (foundMonumentPlace)
                        {
                            break;
                        }

                        int direction = 0;

                        if (startDirectionUp)
                        {
                            if (n == 0)
                            {
                                direction = 0;
                            }
                            else
                            {
                                direction = 1;
                            }
                        }
                        else
                        {
                            if (n == 0)
                            {
                                direction = 1;
                            }
                            else
                            {
                                direction = 0;
                            }
                        }

                        switch (direction)
                        {
                            case (0):
                                {
                                    // go in one direction up
                                    for (int k = randomStartPosX; k < tempArrayLength; k++)
                                    {
                                        for (int l = randomStartPosY; l < tempArrayLength; l++)
                                        {
                                            if (diffMaps[inputPOIs[i].searchResolution][k, l].m_diff < inputPOIs[i].maxHeightDifference)
                                            {
                                                tooClose = false;
                                                totalPosX = (int)((0.5f + k) * toHighestResolution);
                                                totalPosY = (int)((0.5f + l) * toHighestResolution);

                                                for (int m = 0; m < placedMonuments.Count; m++)
                                                {
                                                    if (
                                                         Vector3.Distance(
                                                                          new Vector3(
                                                                                          totalPosX * worldSubMeshVertexDistance,
                                                                                          VertexHeightMapQuality1[totalPosX, totalPosY],
                                                                                          totalPosY * worldSubMeshVertexDistance
                                                                                      ),
                                                                          placedMonuments[m].transform.position
                                                                     )
                                                         < ((totalRadius + placedMonumentsScript[m].radius + placedMonumentsScript[m].blurDistance) * worldSubMeshVertexDistance)
                                                         )
                                                    {
                                                        tooClose = true;
                                                        break;
                                                    }
                                                }

                                                if (tooClose)
                                                {
                                                    continue;
                                                }

                                                changeTerrainForMonument(totalPosX, totalPosY, inputPOIs[i]);

                                                GameObject placedPOI = Instantiate(
                                                                                    m_POI_gameobjects[i],
                                                                                    new Vector3(
                                                                                                    totalPosX * worldSubMeshVertexDistance,
                                                                                                    VertexHeightMapQuality1[totalPosX, totalPosY],
                                                                                                    totalPosY * worldSubMeshVertexDistance
                                                                                                ),
                                                                                    Quaternion.identity
                                                                                    );
                                                placedMonuments.Add(placedPOI);
                                                placedMonumentsScript.Add(placedPOI.GetComponent<PointOfInterest>());
                                                roadConnectionPointsList.Add(new Vector2Int(totalPosX, totalPosY));

                                                foundMonumentPlace = true;
                                                break;
                                            }
                                        }

                                        if (foundMonumentPlace)
                                        {
                                            break;
                                        }
                                    }
                                    break;
                                }
                            case 1:
                                {

                                    // same but go downwards
                                    for (int k = randomStartPosX; k > -1; k--)
                                    {
                                        for (int l = randomStartPosY; l > -1; l--)
                                        {
                                            if (diffMaps[inputPOIs[i].searchResolution][k, l].m_diff < inputPOIs[i].maxHeightDifference)
                                            {
                                                tooClose = false;
                                                totalPosX = (int)((0.5f + k) * toHighestResolution);
                                                totalPosY = (int)((0.5f + l) * toHighestResolution);

                                                for (int m = 0; m < placedMonuments.Count; m++)
                                                {
                                                    if (
                                                         Vector3.Distance(
                                                                          new Vector3(
                                                                                          totalPosX * worldSubMeshVertexDistance,
                                                                                          VertexHeightMapQuality1[totalPosX, totalPosY],
                                                                                          totalPosY * worldSubMeshVertexDistance
                                                                                      ),
                                                                          placedMonuments[m].transform.position
                                                                     )
                                                         < ((totalRadius + placedMonumentsScript[m].radius + placedMonumentsScript[m].blurDistance) * worldSubMeshVertexDistance)
                                                         )
                                                    {
                                                        tooClose = true;
                                                        break;
                                                    }
                                                }

                                                if (tooClose)
                                                {
                                                    continue;
                                                }

                                                changeTerrainForMonument(totalPosX, totalPosY, inputPOIs[i]);

                                                GameObject placedPOI = Instantiate(
                                                                                    m_POI_gameobjects[i],
                                                                                    new Vector3(
                                                                                                    totalPosX * worldSubMeshVertexDistance,
                                                                                                    VertexHeightMapQuality1[totalPosX, totalPosY],
                                                                                                    totalPosY * worldSubMeshVertexDistance
                                                                                                ),
                                                                                    Quaternion.identity
                                                                                    );
                                                placedMonuments.Add(placedPOI);
                                                placedMonumentsScript.Add(placedPOI.GetComponent<PointOfInterest>());
                                                roadConnectionPointsList.Add(new Vector2Int(totalPosX, totalPosY));

                                                foundMonumentPlace = true;
                                                break;
                                            }
                                        }

                                        if (foundMonumentPlace)
                                        {
                                            break;
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                }

            }

            roadConnectionPoints = roadConnectionPointsList.ToArray();

            //Debug.Log("WorldManager: POI: Took " + (Time.realtimeSinceStartup - monumentStartTime) + " s");

            //----------------------------------------- roads -----------------------------------------------------

            if (!noRoads)
            {
                GUIManager.singleton.setGUILoadingProgressText("Finding roads");
                yield return null;
                findRoads();
            }
        }

        VertexHeightMapQuality1.compress();

        //----------------------------------------- Normals -----------------------------------------------------

        GUIManager.singleton.setGUILoadingProgressText("Calculating Terrain-Normals");
        yield return null;
        //createTerrainNormalsRocks();
        //createTerrainNormals();

        NormalCalculatorHeightmap normalCalc = new NormalCalculatorHeightmap(worldSubMeshVertexDistance, VertexHeightMapQuality1, 6);
        normalCalc.start();

        while (!normalCalc.isDone)
        {
            yield return null;
        }

        terrainNormals = new Vector3[worldVertexCountEdge * worldVertexCountEdge];
        terrainNormalsAngle = new float[worldVertexCountEdge * worldVertexCountEdge];

        for (int i = 0; i < worldVertexCountEdge; i++)
        {
            for (int j = 0; j < worldVertexCountEdge; j++)
            {
                terrainNormals[i + j * worldVertexCountEdge] = normalCalc.m_result_normals[i, j];
                terrainNormalsAngle[i + j * worldVertexCountEdge] = normalCalc.m_result_normalsAngle[i, j];
            }
        }

        normalCalc.dispose();

        //----------------------------------------- Textures -----------------------------------------------------

        GUIManager.singleton.setGUILoadingProgressText("Texture-Building");
        yield return null;
        buildVertexTextureMapEM(); // creates vertexTextureMapQuality1

        //----------------------------------------- world-raster -----------------------------------------------------

        if (m_startWorldBuildAsServer)
        {
            GUIManager.singleton.setGUILoadingProgressText("World Rasterization");
            yield return null;

            WorldRasterizer rasterizer = new WorldRasterizer(128, vertexTextureMapQuality1, 3);
            rasterizer.start();

            while (rasterizer.checkIfDone() == false)
            {
                // wait to finish
                yield return null;
            }

            rasterizer.getResult(out m_rasterMainStack, out m_rasterMainStackFields);

            rasterizer.dispose();
        }

        //----------------------------------------- terrain-objects -----------------------------------------------------

        GUIManager.singleton.setGUILoadingProgressText("Finding terrain-objects");
        yield return null;
        //findTerrainObj();

        //----------------------------------------- Rocks -----------------------------------------------------

        GUIManager.singleton.setGUILoadingProgressText("Finding rocks");
        yield return null;

        if (m_createRocks)
        {
            createRocks(terrainNormalsAngle, terrainNormals, VertexHeightMapQuality1, vertexTextureMapQuality1, placedMountains);
        }

        //findTerrainRocks();

        //----------------------------------------- cleanup normals -----------------------------------------------------

        terrainNormals = null;
        terrainNormalsAngle = null;

        //----------------------------------------- spawnpoints -----------------------------------------------------

        GUIManager.singleton.setGUILoadingProgressText("finding spawnpoints");
        yield return null;

        if (GameManager_Custom.singleton.isClient)
        {
            m_beachSpawnPoints = null;
        }
        else if (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient)
        {
            m_beachSpawnPoints = new List<Vector3>();
            findBeachSpawnpoints();
        }

        //----------------------------------------- last steps -----------------------------------------------------

        m_placeholderTextures = null;

        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            m_mainHeightmapRenderer = new HeightmapRenderer(VertexHeightMapQuality1, vertexTextureMapQuality1, worldSubMeshVertexDistance, m_TerrainMeshes_vertexEdgeLength, 20, (byte)vertexMapTextureNames.underwater);
        }

        if (m_serverCollidersActice && (GameManager_Custom.singleton.isServer || GameManager_Custom.singleton.isServerAndClient))
        {
            float[] scalingStage = new float[m_TerrainMeshes_stagesEdgeLength.Length];

            for (int i = 0; i < m_TerrainMeshes_stagesEdgeLength.Length; i++)
            {
                scalingStage[i] = m_TerrainMeshes_stagesEdgeLength[i] * 25 * worldSubMeshVertexDistance;
            }

            m_serverHeightmapColliderComputer = new HeightmapRendererCollider(VertexHeightMapQuality1, vertexTextureMapQuality1, worldSubMeshVertexDistance, m_TerrainMeshes_vertexEdgeLength, 20, (byte)vertexMapTextureNames.underwater, m_playerRadiusTerrainColliderMesh, scalingStage);
        }

        //----------------------------------------- CG Collect -----------------------------------------------------

        GUIManager.singleton.setGUILoadingProgressText("GC Collect");
        yield return null;
        System.GC.Collect();

        //----------------------------------------- last steps 2 -----------------------------------------------------
        GUIManager.singleton.setGUILoadingProgressText("worldbuilding done");
        yield return null;

        m_worldBuildDone = true;
        buildWorldRunning = false;
        SoundManager.singleton.onWorldBuildDone(worldVertexCountEdge * worldSubMeshVertexDistance);
        GameManager_Custom.singleton.onWorldBuilderLoadMapDone();
    }

    private void createRocks(float[] normalMapAngles, Vector3[] normalMap, ShortCompressedFloat heightmap, byte[,] texturemap, List<Vector2Int> mountains)
    {
        // 1: Rockmap from Normals + perlin noise

        float minRockValue = float.MaxValue;
        float maxRockValue = float.MinValue;

        float[,] rockmap = new float[worldVertexCountEdge, worldVertexCountEdge];

        for (int i = 0; i < worldVertexCountEdge; i++)
        {
            for (int j = 0; j < worldVertexCountEdge; j++)
            {
                rockmap[i, j] = Math.Max( normalMapAngles[i + j * worldVertexCountEdge], RandomValuesSeed.perlinNoiseRanged(0, m_rocksRandomAmplitude, i * m_rocksRandomFreqency, j * m_rocksRandomFreqency));
                minRockValue = Mathf.Min(minRockValue, rockmap[i, j]);
                maxRockValue = Mathf.Max(maxRockValue, rockmap[i, j]);
            }
        }

        Debug.Log("minRock: " + minRockValue + " ; maxRock: " + maxRockValue);

        // 2: smooth rockmap

        float average;

        int loopEnd = worldVertexCountEdge - 1;

        for (int m = 0; m < m_rocksSmoothCount; m++)
        {
            for (int i = 1; i < loopEnd; i++)
            {
                for (int j = 1; j < loopEnd; j++)
                {
                    average = 0;

                    average += rockmap[i, j];
                    average += rockmap[i - 1, j];
                    average += rockmap[i + 1, j];
                    average += rockmap[i, j - 1];
                    average += rockmap[i, j + 1];

                    rockmap[i, j] = average / 5;
                }
            }
        }

        // 3: block around mountains

        for (int i = 0; i < mountains.Count; i++)
        {
            int startPosX = Mathf.Max(mountains[i].x - m_rocksMountainBlockRadius, 0);
            int startPosY = Mathf.Max(mountains[i].y - m_rocksMountainBlockRadius, 0);

            int endPosX = Math.Min(mountains[i].x + m_rocksMountainBlockRadius, worldVertexCountEdge);
            int endPosY = Math.Min(mountains[i].y + m_rocksMountainBlockRadius, worldVertexCountEdge);

            for (int j = startPosX; j < endPosX; j++)
            {
                for (int k = startPosY; k < endPosY; k++)
                {
                    if (Vector2Int.Distance(mountains[i], new Vector2Int(j, k)) < m_rocksMountainBlockRadius)
                    {
                        rockmap[j, k] = 0;
                    }
                }
            }
        }

        // 4: place rocks

        List<int>[] rockStagePrefabsIndex = new List<int>[m_rocksStageMin.Length];

        for (int i = 0; i < rockStagePrefabsIndex.Length; i++)
        {
            rockStagePrefabsIndex[i] = new List<int>();
        }

        for (int i = 0; i < rocksPlaceStage.Length; i++)
        {
            rockStagePrefabsIndex[rocksPlaceStage[i]].Add(i);
        }

        for (int i = 0; i < m_rocksStageMin.Length; i++)
        {
            int tempPrefabIndex;
            Vector3 tempPosition;
            int tempEndPosX;
            int tempEndPosY;
            float tempRandomRot;
            GameObject tempGameobject;

            for (int j = 0; j < worldVertexCountEdge; j++)
            {
                for (int k = 0; k < worldVertexCountEdge; k++)
                {
                    if (rockmap[j, k] > m_rocksMinThreshold && rockmap[j, k] < m_rocksStageMin[i] && texturemap[j, k] != (byte)vertexMapTextureNames.underwater)
                    {
                        tempPrefabIndex = rockStagePrefabsIndex[i][RandomValuesSeed.getRandomValueSeed(j * 1.2f, k * 1.2f, 0, rockStagePrefabsIndex[i].Count - 1)];

                        tempPosition = new Vector3(j * worldSubMeshVertexDistance, heightmap[j, k], k * worldSubMeshVertexDistance) + Quaternion.LookRotation( normalMap[j + k * worldVertexCountEdge]) * m_rocksOffset[tempPrefabIndex];

                        tempRandomRot = RandomValuesSeed.getRandomValueSeed(j * 10.2f, k * 10.2f, 0, 360);

                        tempGameobject = Instantiate(rocksPrefabs[tempPrefabIndex], tempPosition, Quaternion.LookRotation(normalMap[j + k * worldVertexCountEdge]) * Quaternion.Euler(rocksOrientations[tempPrefabIndex]) * Quaternion.Euler(0, tempRandomRot, 0));

                        setMaterialGameobjectAndChildren(tempGameobject, rocksMaterial);

                        tempEndPosX = j + rocksBlockRadius[tempPrefabIndex] ;
                        tempEndPosY = k + rocksBlockRadius[tempPrefabIndex] ;

                        for (int l = j - rocksBlockRadius[tempPrefabIndex] ; l < tempEndPosX; l++)
                        {
                            for (int m = k - rocksBlockRadius[tempPrefabIndex] ; m < tempEndPosY; m++)
                            {
                                rockmap[l, m] = 0f;
                            }
                        }
                    }
                }
            }
        }

    }

    private void setMaterialGameobjectAndChildren(GameObject inputGameobject, Material material)
    {
        Renderer renderer = inputGameobject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material = material;
        }

        for (int i = 0; i < inputGameobject.transform.childCount; i++)
        {
            setMaterialGameobjectAndChildren(inputGameobject.transform.GetChild(i).gameObject, material);
        }
    }

    private float getMoistureValue(int posX, int posY)
    {
        return Mathf.PerlinNoise(currentSeed + posX * m_moistureFrequency, currentSeed * 1.1f + posY * m_moistureFrequency);
    }

    private float getTreeOctave(int posX, int posY)
    {
        return m_forestsOctave_curves.Evaluate(Mathf.PerlinNoise(currentSeed * 1.1f + posX * m_forestsOctave_frequency, currentSeed * 1.2f + posY * m_forestsOctave_frequency));
    }

    [SerializeField] private AnimationCurve turbulenceCurve;
    [SerializeField] private int turbulenceAmplitude = 10;

    private void changeTerrainForMonument(int posX, int posY, PointOfInterest POI)
    {
        Vector2Int middlePos = new Vector2Int(posX, posY);

        int innerRadius = POI.radius;
        int totalRadius = POI.radius + POI.blurDistance;
        int blurRadius = POI.blurDistance;

        int loopXEnd = posX + totalRadius;
        int loopYEnd = posY + totalRadius;

        float average = 0;
        int averageCounter = 0;

        // find average

        for (int i = posX - totalRadius; i < loopXEnd; i++)
        {
            for (int j = posY - totalRadius; j < loopYEnd; j++)
            {
                if (Vector2Int.Distance(middlePos, new Vector2Int(i, j)) < innerRadius)
                {
                    average += VertexHeightMapQuality1[i, j];
                    averageCounter++;
                }
            }
        }

        average /= averageCounter;

        // set to average and blur into terrain
        float distance;
        float blurDistance;

        for (int i = posX - totalRadius; i < loopXEnd; i++)
        {
            for (int j = posY - totalRadius; j < loopYEnd; j++)
            {
                if (Vector2Int.Distance(middlePos, new Vector2Int(i, j)) < innerRadius)
                {
                    VertexHeightMapQuality1[i, j] = average;
                    m_placeholderTextures[i, j] = PlaceholderTextureNames.Road;
                }
                else
                {
                    distance = Vector2Int.Distance(middlePos, new Vector2Int(i, j));

                    if (distance < totalRadius) // = blur radius
                    {
                        blurDistance = distance - innerRadius;
                        VertexHeightMapQuality1[i, j] = Mathf.Lerp(average, VertexHeightMapQuality1[i, j], m_POI_blurCurve.Evaluate(blurDistance / blurRadius));
                    }
                }
            }
        }

    }

    private float[] terrainNormalsAngle; // angle between terrain-normals and vector3.up
    private Vector3[] terrainNormals;
    /*
    private void createTerrainNormalsRocks()
    {
        /*
		 * 			C
		 * 	D		i		B
		 * 			A
		 */

    /*
    int indexA;
    int indexB;
    int indexC;
    int indexD;
    int maxIndex = VertexHeightMapQuality1.Length - 1;

    Vector3 i_A = new Vector3(0, 0, -1) * worldSubMeshVertexDistance;
    Vector3 i_B = new Vector3(1, 0, 0) * worldSubMeshVertexDistance;
    Vector3 i_C = new Vector3(0, 0, 1) * worldSubMeshVertexDistance;
    Vector3 i_D = new Vector3(-1, 0, 0) * worldSubMeshVertexDistance;

    Vector3 ABNormal;
    Vector3 BCNormal;
    Vector3 CDNormal;
    Vector3 DANormal;

    Vector3 resultingNormal;

    terrainNormals = new Vector3[VertexHeightMapQuality1.Length];
    terrainNormalsAngle = new float[VertexHeightMapQuality1.Length];
    //terrainRocksOpenList = new bool[VertexHeightMapQuality1.Length];

    byte rockIndex = (byte)vertexMapTextureNames.rock;

    for (int i = 0; i < VertexHeightMapQuality1.Length; i++)
    {
        if (vertexTextureMapQuality1[i] == rockIndex)
        {
            //terrainRocksOpenList[i] = true;

            indexA = Mathf.Max(0, i - worldVertexCountEdge);
            indexB = Mathf.Min(maxIndex, i + 1);
            indexD = Mathf.Max(0, i - 1);
            indexC = Mathf.Min(maxIndex, i + worldVertexCountEdge);

            i_A.y = VertexHeightMapQuality1[indexA] - VertexHeightMapQuality1[i];
            i_B.y = VertexHeightMapQuality1[indexB] - VertexHeightMapQuality1[i];
            i_C.y = VertexHeightMapQuality1[indexC] - VertexHeightMapQuality1[i];
            i_D.y = VertexHeightMapQuality1[indexD] - VertexHeightMapQuality1[i];

            ABNormal = Vector3.Cross(i_B, i_A).normalized;
            BCNormal = Vector3.Cross(i_C, i_B).normalized;
            CDNormal = Vector3.Cross(i_D, i_C).normalized;
            DANormal = Vector3.Cross(i_A, i_D).normalized;

            resultingNormal = (ABNormal + BCNormal + CDNormal + DANormal).normalized;

            terrainNormals[i] = resultingNormal;
            terrainNormalsAngle[i] = Mathf.Abs(Vector3.Angle(Vector3.up, resultingNormal));
        }
    }
}
*/

    /*
private void createTerrainNormals()
{
    /*
     * 			C
     * 	D		i		B
     * 			A
     */

    /*

    Vector3 i_B = new Vector3(1, 0, 0) * worldSubMeshVertexDistance;
    Vector3 i_C = new Vector3(0, 0, 1) * worldSubMeshVertexDistance;

    Vector3 BCNormal;

    terrainNormals = new Vector3[VertexHeightMapQuality1.Length];
    terrainNormalsAngle = new float[VertexHeightMapQuality1.Length];

    int loopEndIndex = VertexHeightMapQuality1.Length - worldVertexCountEdge - 2;
    for (int i = 0; i < loopEndIndex; i++)
    {
        i_B.y = VertexHeightMapQuality1[i + 1] - VertexHeightMapQuality1[i];
        i_C.y = VertexHeightMapQuality1[i + worldVertexCountEdge] - VertexHeightMapQuality1[i];

        BCNormal = Vector3.Cross(i_C, i_B).normalized;

        terrainNormals[i] = BCNormal;
        terrainNormalsAngle[i] = Vector3.Angle(Vector3.up, BCNormal);
    }
}

*/

    /*
    private void findTerrainObj()
    {
        bool[,] bushClosedList = new bool[worldVertexCountEdge, worldVertexCountEdge];
        float currentBushProbability;
        byte currentTextureIndex;
        List<Vector3>[] bushesPositions = new List<Vector3>[2];

        for (int i = 0; i < bushesPositions.Length; i++)
        {
            bushesPositions[i] = new List<Vector3>();
        }

        for (int i = 0; i < worldVertexCountEdge; i++)
        {
            for (int j = 0; j < worldVertexCountEdge; j++)
            {
                currentTextureIndex = vertexTextureMapQuality1[i + j * worldVertexCountEdge];

                if (!bushClosedList[i, j])
                {
                    if (currentTextureIndex == (byte)vertexMapTextureNames.grass)
                    {
                        currentBushProbability = bushProbabilityPercentBase / 8f;
                    }
                    else if (currentTextureIndex == (byte)vertexMapTextureNames.forest)
                    {
                        currentBushProbability = bushProbabilityPercentBase;
                    }
                    else if (currentTextureIndex == (byte)vertexMapTextureNames.deadGrass)
                    {
                        currentBushProbability = bushProbabilityPercentBase / 8f;
                    }
                    else
                    {
                        currentBushProbability = 0f;
                    }

                    if (RandomValuesSeed.getRandomBoolProbability(i, j, currentBushProbability))
                    {
                        for (int k = -bushBlockDistance / 2; k < bushBlockDistance / 2; k++)
                        {
                            for (int l = -bushBlockDistance / 2; l < bushBlockDistance / 2; l++)
                            {
                                bushClosedList[i + k, j + l] = true;
                            }
                        }

                        if (RandomValuesSeed.getRandomBoolProbability(j, i, 50f))
                        {
                            // bush typ 1
                            bushesPositions[0].Add(new Vector3(i * worldSubMeshVertexDistance, VertexHeightMapQuality1[i + j * worldVertexCountEdge] + terrainObjectsPrefabsYOffsets[0], j * worldSubMeshVertexDistance));
                        }
                        else
                        {
                            // bush typ 2
                            bushesPositions[1].Add(new Vector3(i * worldSubMeshVertexDistance, VertexHeightMapQuality1[i + j * worldVertexCountEdge] + terrainObjectsPrefabsYOffsets[1], j * worldSubMeshVertexDistance));
                        }
                    }
                }
            }
        }

        TerrainObjPositions[0] = bushesPositions[0].ToArray();
        TerrainObjPositions[1] = bushesPositions[1].ToArray();
    }

    */

        /*
    private void findTerrainRocks()
    {
        List<Vector3>[] rockPos = new List<Vector3>[rocksPrefabs.Length];
        List<Quaternion>[] rockRot = new List<Quaternion>[rocksPrefabs.Length];

        float xPos;
        float zPos;

        Vector2Int freeSpace = new Vector2Int(0, 0);

        Vector2Int currentObjSize = new Vector2Int(0, 0);

        int noFittingRockFoundCounter = 0;
        Vector3 resultingNormal = new Vector3(0, 0, 0);

        Vector3 rockSize = new Vector3(0, 0, 0);
        Vector2Int rockSize2Dhalf = new Vector2Int(0, 0);
        Vector2Int currentRockOriginPos = new Vector2Int(0, 0);
        Vector2Int freeSpaceCheckStartPos;
        Vector2Int freeSpaceTempPos;
        Vector2 currentRockDirRight;
        Vector2 currentRockDirForward;
        Vector2Int currentRockDirRightExtend = new Vector2Int(0, 0);
        Vector2Int currentRockDirForwardExtend = new Vector2Int(0, 0);
        Quaternion currentObjRot;
        int freeSpaceCheckIndex;
        int freeSpaceCounter;

        List<int> checkedOpenListEntries = new List<int>();

        int biggestRockSize = 0;

        bool[,] terrainRocksOpenList = new bool[VertexHeightMapQuality1.GetLength(0), VertexHeightMapQuality1.GetLength(1)];

        byte rockIndex = (byte)vertexMapTextureNames.rock;

        for (int i = 0; i < terrainRocksOpenList.GetLength(0); i++)
        {
            for (int j = 0; j < terrainRocksOpenList.GetLength(1); j++)
            {
                if (vertexTextureMapQuality1[i, j] == rockIndex)
                {
                    terrainRocksOpenList[i, j] = true;
                }
            }
        }

        for (int i = 0; i < rocksPrefabs.Length; i++)
        {
            biggestRockSize = Mathf.Max(biggestRockSize, rocksBlockRadius[i]);
            rockPos[i] = new List<Vector3>();
            rockRot[i] = new List<Quaternion>();
        }

        Vector3 rotEuler;

        int currentRockSize;
        int currentRockSizeHalf;
        int currentWorldIndex;
        int endIndex = worldVertexCountEdge - biggestRockSize;
        float yRot = 0;
        Vector3 currentRockPos;
        for (int k = 0; k < rocksPrefabs.Length; k++)
        {
            currentRockSize = rocksBlockRadius[k];
            currentRockSizeHalf = currentRockSize / 2;
            for (int i = biggestRockSize; i < endIndex; i += currentRockSize)
            {
                for (int j = biggestRockSize; j < endIndex; j += currentRockSize)
                {
                    if (terrainRocksOpenList[i, j])
                    {
                        if (terrainNormalsAngle[i + j * worldVertexCountEdge] > rocksAngleMin[k])
                        {
                            // set rock

                            for (int l = 0; l < currentRockSize; l++)
                            {
                                for (int m = 0; m < currentRockSize; m++)
                                {
                                    terrainRocksOpenList[i - currentRockSizeHalf + l, j - currentRockSizeHalf + m] = false;
                                }
                            }

                            currentRockPos = new Vector3(i * worldSubMeshVertexDistance, VertexHeightMapQuality1[i, j] + rocksYOffset[k], j * worldSubMeshVertexDistance);

                            switch (rocksRandomRotIndex[k])
                            {
                                case randomRockRotIndex.around_Y_axis:
                                    {
                                        yRot = RandomValuesSeed.getRandomValueSeed(currentRockPos.x, currentRockPos.y, 0f, 360f);
                                        break;
                                    }
                                case randomRockRotIndex.no_rotation:
                                    {
                                        yRot = 0;
                                        break;
                                    }
                            }

                            rotEuler = Quaternion.LookRotation(terrainNormals[i + j * worldVertexCountEdge]).eulerAngles;
                            rotEuler.x = 0;
                            rotEuler.z = 0;

                            currentObjRot = (Quaternion.Euler(rotEuler) * Quaternion.Euler(rocksOrientations[k])) * Quaternion.Euler(new Vector3(0, yRot, 0));

                            rockPos[k].Add(currentRockPos);
                            rockRot[k].Add(currentObjRot);

                        }
                    }
                }
            }
        }

        Debug.LogWarning("noFittingRockFoundCounter: " + noFittingRockFoundCounter);

        rocksPositions = new Vector3[rocksPrefabs.Length][];
        rocksRotations = new Quaternion[rocksPrefabs.Length][];
        for (int i = 0; i < rocksPositions.Length; i++)
        {
            if (noRocks)
            {
                rockPos[i].Clear();
                rockRot[i].Clear();
            }

            rocksPositions[i] = rockPos[i].ToArray();
            rocksRotations[i] = rockRot[i].ToArray();

            rockPos[i].Clear();
            rockRot[i].Clear();

            rockPos[i] = null;
            rockRot[i] = null;
        }
        rockPos = null;
        rockRot = null;

    }
    */
    private Vector2Int[] roadConnectionPoints;

    /*
    private bool getHeightmapCircleInfos(int originX, int originY, int radius, float minValue, out float average, out float min, out float max)
    {
        if (originX < 0 || originX >= worldVertexCountEdge || originY < 0 || originY >= worldVertexCountEdge)
        {
            average = 0;
            min = float.MinValue;
            max = float.MaxValue;
            return false; // out of bounds
        }
        else
        {
            min = float.MinValue;
            max = float.MaxValue;
            average = VertexHeightMapQuality1[originX + originY * worldVertexCountEdge];
        }

        Vector2Int origin = new Vector2Int(originX, originY);
        int loopEndX = originX + radius;
        int loopEndY = originY + radius;

        for (int i = originX - radius; i < loopEndX; i++)
        {
            for (int j = originX - radius; j < loopEndY; j++)
            {
                if (Vector2Int.Distance(origin, new Vector2Int(i, j)) < radius)
                {



                }
            }
        }

    }
    */

    /*
private void smoothHeightMapAt(ref List<Vector2Int> positions, ref int index)
{
    VertexHeightMapQuality1[positions[index].x + positions[index].y * worldVertexCountEdge] =
        (
            VertexHeightMapQuality1[positions[index].x + 1 + positions[index].y * worldVertexCountEdge]
            + VertexHeightMapQuality1[positions[index].x + (positions[index].y + 1) * worldVertexCountEdge]
            + VertexHeightMapQuality1[positions[index].x - 1 + positions[index].y * worldVertexCountEdge]
            + VertexHeightMapQuality1[positions[index].x + (positions[index].y - 1) * worldVertexCountEdge])
        / 4;
}

private void smoothHeightMapAt(ref Vector2Int position)
{
    VertexHeightMapQuality1[position.x + position.y * worldVertexCountEdge] =
        (
            VertexHeightMapQuality1[position.x + 1 + position.y * worldVertexCountEdge]
            + VertexHeightMapQuality1[position.x + (position.y + 1) * worldVertexCountEdge]
            + VertexHeightMapQuality1[position.x - 1 + position.y * worldVertexCountEdge]
            + VertexHeightMapQuality1[position.x + (position.y - 1) * worldVertexCountEdge])
        / 4;
}

    */

    private struct CountIndex
    {
        public byte count;
        public byte index;
    }

    private struct AStarNode
    {
        public AStarNode(int inParentClosedListIndex, int inWorldIndexX, int inWorldIndexY, float inCost)
        {
            parentClosedListIndex = inParentClosedListIndex;
            worldIndexX = inWorldIndexX;
            worldIndexY = inWorldIndexY;
            cost = inCost;
        }

        public int parentClosedListIndex;
        public int worldIndexX;
        public int worldIndexY;
        public float cost;
    }

    private struct FindForestNode
    {
        public FindForestNode(int posX, int posY, float cost)
        {
            m_posX = posX;
            m_posY = posY;
            m_cost = cost;
        }

        public int m_posX;
        public int m_posY;
        public float m_cost;
    }

    private class WorldViewPointsSnapshot
    {
        public bool[,] renderStates;
        public int[,] outerSize;

        public Vector2[] transformPositions;

        public WorldViewPointsSnapshot(List<GameObject> inWorldViewpointsList)
        {
            int viewPointCount = inWorldViewpointsList.Count;
            int maxEntries = 0;

            transformPositions = new Vector2[viewPointCount];

            foreach (GameObject viewObj in inWorldViewpointsList)
            {
                maxEntries = (int)Mathf.Max(maxEntries, viewObj.GetComponent<WorldViewPoint>().outerSizeLength);
                maxEntries = (int)Mathf.Max(maxEntries, viewObj.GetComponent<WorldViewPoint>().renderThisStageLength);
            }

            renderStates = new bool[viewPointCount, maxEntries];
            outerSize = new int[viewPointCount, maxEntries];

            for (int i = 0; i < inWorldViewpointsList.Count; i++)
            {
                transformPositions[i] = new Vector2(inWorldViewpointsList[i].transform.position.x, inWorldViewpointsList[i].transform.position.z);
                for (int j = 0; j < maxEntries; j++)
                {
                    outerSize[i, j] = inWorldViewpointsList[i].GetComponent<WorldViewPoint>().getOuterSize(j + 1);
                    renderStates[i, j] = inWorldViewpointsList[i].GetComponent<WorldViewPoint>().getRenderState(j + 1);
                }
            }

        }

        public void cleanUp()
        {
            renderStates = null;
            outerSize = null;
        }
    }

    /// <summary>
    /// Downscales the array cheap.
    /// </summary>
    /// <returns>The array cheap.</returns>
    /// <param name="inArray">In array.</param>
    /// <param name="downscalefactor">Downscalefactor: 2,4,8,16....</param>
    private float[] downscaleArrayCheap(ref float[] inArray, int downscalefactor)
    {
        int inEdgeLength = (int)Mathf.Sqrt(inArray.Length);
        int outEdgeLength = inEdgeLength / downscalefactor;

        float[] outputArray = new float[outEdgeLength * outEdgeLength];

        for (int i = 0; i < outEdgeLength; i++)
        {
            for (int j = 0; j < outEdgeLength; j++)
            {
                outputArray[i + j * outEdgeLength] = inArray[i * downscalefactor + j * downscalefactor * inEdgeLength];
            }
        }

        return outputArray;
    }

    private void findRoads()
    {
        /*

        Vector2Int connectionFromPoint;
        Vector2Int connectionToPoint;

        List<AStarNode> openList = new List<AStarNode>();
        List<AStarNode> closedList = new List<AStarNode>();
        AStarNode currentNode;
        AStarNode pathBackNode = new AStarNode(-1, -1, -1, -1);

        bool[] openListEntry;
        bool[] closedListEntry = new bool[VertexHeightMapQuality1.Length];
        bool[] closedListGlobal = new bool[VertexHeightMapQuality1.Length];
        bool[] roadPainted = new bool[VertexHeightMapQuality1.Length];
        Vector2Int addOpenListPos = new Vector2Int(0, 0);
        int addOpenListIndex;
        bool endReached;
        byte underwaterTexIndex = (byte)vertexMapTextureNames.underwater;
        byte beachTexIndex = (byte)vertexMapTextureNames.beachsand;
        byte roadTexIndex = (byte)vertexMapTextureNames.road_trail;
        float resultingCost;
        float shapFactor = -1; // hypothenuse is longer than horizontal or vertical
        bool noConnectionPossible;


        int edgeSizeMinusAStartDistance = worldVertexCountEdge - roadAStarDistance;
        for (int i = roadAStarDistance; i < edgeSizeMinusAStartDistance; i++)
        {
            for (int j = roadAStarDistance; j < edgeSizeMinusAStartDistance; j++)
            {
                if (vertexTextureMapQuality1[i + j * worldVertexCountEdge] == beachTexIndex || vertexTextureMapQuality1[i + j * worldVertexCountEdge] == underwaterTexIndex)
                {
                    closedListGlobal[i + j * worldVertexCountEdge] = true;
                }
            }
        }

        int tempIndex1;
        int tempIndex2;
        for (int i = 0; i < roadAStarDistance; i++)
        {
            for (int j = 0; j < worldVertexCountEdge; j++)
            {
                tempIndex1 = i * worldVertexCountEdge + j;
                closedListGlobal[tempIndex1] = true;
                closedListGlobal[closedListGlobal.Length - tempIndex1 - 1] = true;
                tempIndex2 = j * worldVertexCountEdge + i;
                closedListGlobal[tempIndex2] = true;
                closedListGlobal[closedListGlobal.Length - tempIndex2 - 1] = true;
            }
        }

        for (int i = 0; i < roadConnectionPoints.Length; i++)
        {
            roadConnectionPoints[i].x = (roadConnectionPoints[i].x / roadAStarDistance) * roadAStarDistance;
            roadConnectionPoints[i].y = (roadConnectionPoints[i].y / roadAStarDistance) * roadAStarDistance;
        }

        Debug.Log("roadConnectionPoints: " + roadConnectionPoints.Length);
        for (int i = 0; i < roadConnectionPoints.Length; i++)
        {
            openListEntry = new bool[VertexHeightMapQuality1.Length];
            closedListGlobal.CopyTo(closedListEntry, 0);

            connectionFromPoint = roadConnectionPoints[i];

            if (i == roadConnectionPoints.Length - 1)
            {
                connectionToPoint = roadConnectionPoints[0];
            }
            else
            {
                connectionToPoint = roadConnectionPoints[i + 1];
            }
            openList.Clear();
            closedList.Clear();

            openList.Add(new AStarNode(-1, connectionFromPoint.x, connectionFromPoint.y, 0));
            openListEntry[connectionFromPoint.x + connectionFromPoint.y * worldVertexCountEdge] = true;

            endReached = false;
            noConnectionPossible = false;

            while (true)
            {
                if (openList.Count > 0)
                {
                    currentNode = openList[0];
                }
                else
                {
                    noConnectionPossible = true;
                    break;
                }

                // find cheapest node and check if end is reached
                foreach (AStarNode checkNode in openList)
                {
                    if (checkNode.cost < currentNode.cost)
                    {
                        currentNode = checkNode;
                    }

                    if (checkNode.worldIndexX == connectionToPoint.x && checkNode.worldIndexY == connectionToPoint.y)
                    {
                        // end reached
                        pathBackNode = checkNode;
                        endReached = true;
                    }
                }
                if (endReached)
                {
                    break;
                }

                // add neighbors to openlist
                for (int j = 0; j < 8; j++)
                {
                    if (j == 0)
                    {
                        addOpenListPos.x = currentNode.worldIndexX + roadAStarDistance;
                        addOpenListPos.y = currentNode.worldIndexY;
                        shapFactor = roadAStarDistance;
                    }
                    else if (j == 1)
                    {
                        addOpenListPos.x = currentNode.worldIndexX;
                        addOpenListPos.y = currentNode.worldIndexY + roadAStarDistance;
                        shapFactor = roadAStarDistance;
                    }
                    else if (j == 2)
                    {
                        addOpenListPos.x = currentNode.worldIndexX - roadAStarDistance;
                        addOpenListPos.y = currentNode.worldIndexY;
                        shapFactor = roadAStarDistance;
                    }
                    else if (j == 3)
                    {
                        addOpenListPos.x = currentNode.worldIndexX;
                        addOpenListPos.y = currentNode.worldIndexY - roadAStarDistance;
                        shapFactor = roadAStarDistance;
                    }
                    else if (j == 4)
                    {
                        addOpenListPos.x = currentNode.worldIndexX + roadAStarDistance;
                        addOpenListPos.y = currentNode.worldIndexY + roadAStarDistance;
                        shapFactor = 1.41f * roadAStarDistance;
                    }
                    else if (j == 5)
                    {
                        addOpenListPos.x = currentNode.worldIndexX + roadAStarDistance;
                        addOpenListPos.y = currentNode.worldIndexY - roadAStarDistance;
                        shapFactor = 1.41f * roadAStarDistance;
                    }
                    else if (j == 6)
                    {
                        addOpenListPos.x = currentNode.worldIndexX - roadAStarDistance;
                        addOpenListPos.y = currentNode.worldIndexY - roadAStarDistance;
                        shapFactor = 1.41f * roadAStarDistance;
                    }
                    else if (j == 7)
                    {
                        addOpenListPos.x = currentNode.worldIndexX - roadAStarDistance;
                        addOpenListPos.y = currentNode.worldIndexY + roadAStarDistance;
                        shapFactor = 1.41f * roadAStarDistance;
                    }

                    addOpenListIndex = addOpenListPos.x + addOpenListPos.y * worldVertexCountEdge;

                    if (!(openListEntry[addOpenListIndex] || closedListEntry[addOpenListIndex]))
                    {
                        // not in openlist nor closedlist --> add to openlist

                        resultingCost = currentNode.cost + Vector2Int.Distance(addOpenListPos, connectionToPoint);

                        if (roadPainted[addOpenListIndex])
                        {
                            resultingCost += (roadTerrainAngleCost.Evaluate(terrainNormalsAngle[addOpenListIndex] / 45) * 2000 * shapFactor) / roadsOverlayCostReduction;
                        }
                        else
                        {
                            resultingCost += roadTerrainAngleCost.Evaluate(terrainNormalsAngle[addOpenListIndex] / 45) * 2000 * shapFactor;
                        }

                        openList.Add(new AStarNode(closedList.Count, addOpenListPos.x, addOpenListPos.y, resultingCost));
                        openListEntry[addOpenListIndex] = true;
                    }

                }

                // set current node to closedlist
                closedList.Add(currentNode);
                openListEntry[currentNode.worldIndexX + currentNode.worldIndexY * worldVertexCountEdge] = false;
                closedListEntry[currentNode.worldIndexX + currentNode.worldIndexY * worldVertexCountEdge] = true;
                openList.Remove(currentNode);
            }

            Debug.Log("closedList: " + closedList.Count);
            if (!noConnectionPossible)
            {
                int counter1 = 0;
                // paint the road
                while (true)
                {
                    if (pathBackNode.parentClosedListIndex == -1) // startpoint
                    {
                        break;
                    }

                    counter1++;

                    roadPainted[pathBackNode.worldIndexX + pathBackNode.worldIndexY * worldVertexCountEdge] = true;
                    for (int j = 0; j < 40; j++)
                    {
                        for (int k = 0; k < 40; k++)
                        {
                            vertexTextureMapQuality1[pathBackNode.worldIndexX + j + (pathBackNode.worldIndexY + k) * worldVertexCountEdge] = roadTexIndex;
                        }
                    }

                    /*
                    vertexTextureMapQuality1[pathBackNode.worldIndexX + pathBackNode.worldIndexY * worldVertexCountEdge] = roadTexIndex;
                    vertexTextureMapQuality1[pathBackNode.worldIndexX+1 + pathBackNode.worldIndexY * worldVertexCountEdge] = roadTexIndex;
                    vertexTextureMapQuality1[pathBackNode.worldIndexX-1 + pathBackNode.worldIndexY * worldVertexCountEdge] = roadTexIndex;
                    vertexTextureMapQuality1[pathBackNode.worldIndexX + (pathBackNode.worldIndexY+1) * worldVertexCountEdge] = roadTexIndex;
                    vertexTextureMapQuality1[pathBackNode.worldIndexX + (pathBackNode.worldIndexY-1) * worldVertexCountEdge] = roadTexIndex;
                    */

        /*

                    pathBackNode = closedList[pathBackNode.parentClosedListIndex];
                }
                Debug.Log("painted points: " + counter1);
            }
            else
            {
                Debug.LogWarning("no road connection possible");
            }

        }


        openList.Clear();
        openList = null;

        closedList.Clear();
        closedList = null;

        openListEntry = null;
        closedListEntry = null;
        closedListGlobal = null;
        roadPainted = null;

    */
    }

    private void findBeachSpawnpoints()
    {
        int spawnPointDistance = 300;
        int startPos;
        int currentPos;

        for (int k = 0; k < 4; k++) // approach to beach on all 4 sides
        {
            switch (k)
            {
                case 0: // from left to right
                    {
                        for (int i = 0; i < worldVertexCountEdge; i += spawnPointDistance)
                        {
                            for (int j = 0; j < worldVertexCountEdge; j += spawnPointDistance)
                            {
                                if ((vertexMapTextureNames)vertexTextureMapQuality1[j, i] == vertexMapTextureNames.beachsand)
                                {
                                    m_beachSpawnPoints.Add(new Vector3(j * worldSubMeshVertexDistance, VertexHeightMapQuality1[j, i], i * worldSubMeshVertexDistance));
                                    break;
                                }
                            }
                        }
                        break;
                    }
                case 1: // from bottom to top
                    {
                        for (int i = 0; i < worldVertexCountEdge; i += spawnPointDistance)
                        {
                            for (int j = 0; j < worldVertexCountEdge; j += spawnPointDistance)
                            {
                                if ((vertexMapTextureNames)vertexTextureMapQuality1[i, j] == vertexMapTextureNames.beachsand)
                                {
                                    m_beachSpawnPoints.Add(new Vector3(i * worldSubMeshVertexDistance, VertexHeightMapQuality1[i, j], j * worldSubMeshVertexDistance));
                                    break;
                                }
                            }
                        }
                        break;
                    }
                case 2: // from right to left
                    {
                        for (int i = 0; i < worldVertexCountEdge; i += spawnPointDistance)
                        {
                            for (int j = worldVertexCountEdge - 1; j > -1; j -= spawnPointDistance)
                            {
                                if ((vertexMapTextureNames)vertexTextureMapQuality1[j, i] == vertexMapTextureNames.beachsand)
                                {
                                    m_beachSpawnPoints.Add(new Vector3(j * worldSubMeshVertexDistance, VertexHeightMapQuality1[j, i], i * worldSubMeshVertexDistance));
                                    break;
                                }
                            }
                        }
                        break;
                    }
                case 3: // from top to bottom
                    {
                        for (int i = 0; i < worldVertexCountEdge; i += spawnPointDistance)
                        {
                            for (int j = worldVertexCountEdge - 1; j > -1; j -= spawnPointDistance)
                            {
                                if ((vertexMapTextureNames)vertexTextureMapQuality1[i, j] == vertexMapTextureNames.beachsand)
                                {
                                    m_beachSpawnPoints.Add(new Vector3(i * worldSubMeshVertexDistance, VertexHeightMapQuality1[i, j], j * worldSubMeshVertexDistance));
                                    break;
                                }
                            }
                        }
                        break;
                    }
                default:
                    {
                        Debug.LogError("unkwon index: " + k);
                        break;
                    }
            }
        }
    }

    private void drawMeshesInstancedTrees()
    {
        // distant trees
        if (matricesDistantTrees != null && matricesDistantTrees.Length > 0)
        {
            for (int i = 0; i < matricesDistantTrees.Length; i++) // for every tree type
            {
                if (matricesDistantTrees[i] == null || matricesDistantTrees[i].Length == 0)
                {
                    continue;
                }

                for (int j = 0; j < matricesDistantTrees[i].Length; j++) // for every <=1023 member array of one three type
                {
                    if (matricesDistantTrees[i][j] != null && matricesDistantTrees[i][j].Length > 0)
                    {
                        Graphics.DrawMeshInstanced(distantTreeMesh, 0, distantTreeMaterials[i], matricesDistantTrees[i][j], matricesDistantTrees[i][j].Length, null, UnityEngine.Rendering.ShadowCastingMode.On, true);
                    }
                }
            }
        }

    }

    /// <summary>
    /// Gets the players distance to water. checking only 4 directions straight. not updated every frame
    /// </summary>
    /// <returns>The last player distance to water.</returns>
    public Vector2 getLastPlayerDistanceToWater()
    {
        return LastPlayerDistanceToWater;
    }
    private const int playerDistanceToWaterMaxIterationsLoop = 1;
    private Vector2 LastPlayerDistanceToWater = Vector2.positiveInfinity;
    private int calculatePlayerDistanceToWaterIterations = 0;
    private IEnumerator calculatePlayerDistanceToWater()
    {
        while (true)
        {
            //float startTime2 = Time.realtimeSinceStartup; 
            if (EntityManager.singleton.getLocalPlayer() != null)
            {
                Vector2Int currentPlayerPosArray = new Vector2Int((int)(Mathf.Max(EntityManager.singleton.getLocalPlayer().transform.position.x, 0) / worldSubMeshVertexDistance), (int)(Mathf.Max(EntityManager.singleton.getLocalPlayer().transform.position.z, 0) / worldSubMeshVertexDistance));
                int extend = 0;
                int worldVertexCountEdgeMinusOne = worldVertexCountEdge - 1;

                Vector2Int currentIterationPosArray = new Vector2Int(0, 0);
                vertexMapTextureNames currentTexture;

                while (extend < playerDistanceToWaterMaxIterationsExtend)
                {
                    if (calculatePlayerDistanceToWaterIterations > playerDistanceToWaterMaxIterationsLoop)
                    {
                        calculatePlayerDistanceToWaterIterations = 0;
                        yield return null;
                    }

                    currentIterationPosArray.x = Mathf.Min(currentPlayerPosArray.x + extend, worldVertexCountEdgeMinusOne);
                    currentIterationPosArray.y = currentPlayerPosArray.y;

                    if (currentIterationPosArray.x < 0 || currentIterationPosArray.x >= vertexTextureMapQuality1.GetLength(0) || currentIterationPosArray.y < 0 || currentIterationPosArray.y >= vertexTextureMapQuality1.GetLength(1))
                    {
                        currentTexture = vertexMapTextureNames.underwater;
                    }
                    else
                    {
                        currentTexture = (vertexMapTextureNames)vertexTextureMapQuality1[currentIterationPosArray.x, currentIterationPosArray.y];
                    }

                    if (currentTexture == vertexMapTextureNames.underwater)
                    {
                        break;
                    }

                    //////////////////////////////////////
                    currentIterationPosArray.x = Mathf.Max(currentPlayerPosArray.x - extend, 0);

                    currentTexture = (vertexMapTextureNames)vertexTextureMapQuality1[currentIterationPosArray.x, currentIterationPosArray.y];

                    if (currentTexture == vertexMapTextureNames.underwater)
                    {
                        break;
                    }

                    //////////////////////////////////////
                    currentIterationPosArray.x = currentPlayerPosArray.x;
                    currentIterationPosArray.y = Mathf.Min(currentPlayerPosArray.y + extend, worldVertexCountEdgeMinusOne);

                    currentTexture = (vertexMapTextureNames)vertexTextureMapQuality1[currentIterationPosArray.x, currentIterationPosArray.y];

                    if (currentTexture == vertexMapTextureNames.underwater)
                    {
                        break;
                    }

                    //////////////////////////////////////
                    currentIterationPosArray.y = Mathf.Max(currentPlayerPosArray.y - extend, 0);

                    currentTexture = (vertexMapTextureNames)vertexTextureMapQuality1[currentIterationPosArray.x, currentIterationPosArray.y];

                    if (currentTexture == vertexMapTextureNames.underwater)
                    {
                        break;
                    }

                    extend++;
                    calculatePlayerDistanceToWaterIterations++;
                }

                //Debug.LogWarning("playerDistanceToWaterTime" +(Time.realtimeSinceStartup - startTime2) );

                if (extend == playerDistanceToWaterMaxIterationsExtend)
                {
                    LastPlayerDistanceToWater = Vector2.positiveInfinity;
                }
                else
                {
                    LastPlayerDistanceToWater = currentIterationPosArray - currentPlayerPosArray;
                }

            }
            else
            {
                LastPlayerDistanceToWater = Vector2.positiveInfinity;
            }
            yield return null;
        }
    }

    public void set3DTreeRenderDistance(int newIndex)
    {
        switch (newIndex)
        {
            case 0: // very low
                {
                    treeRenderDistanceMax = 100;
                    break;
                }
            case 1: // low
                {
                    treeRenderDistanceMax = 200;
                    break;
                }
            case 2: // medium
                {
                    treeRenderDistanceMax = 350;
                    break;
                }
            case 3: // high
                {
                    treeRenderDistanceMax = 720;
                    break;
                }
            case 4: // very high
                {
                    treeRenderDistanceMax = 1500;
                    break;
                }
            default:
                {
                    Debug.LogError("unknown index: " + newIndex);
                    break;
                }
        }
    }

    private int findTreePrefabByName(string inName)
    {
        for (int i = 0; i < treePrefabs.Length; i++)
        {
            if (treePrefabs[i].name == inName)
            {
                return i;
            }
        }
        return -1;
    }

    private void treesCloseManagement() // obsolete
    {
        if (treesInitialSetupPending)
        {
            if (m_spawnForests)
            {
                m_rasterMainStack.addAllResources(FieldResources.ResourceType.Tree);
            }

            Debug.Log("TODO Mike: Move initial lootbarrel spawn and berry plant");

            m_rasterMainStack.addAllResources(FieldResources.ResourceType.RandomLootBarrel);
            m_rasterMainStack.addAllResources(FieldResources.ResourceType.BerryPlant);

            /*

            float tempPosX;
            float tempPosY;
            float temp_height;
            float tempFertility;
            Vector3 normal;
            byte textureIndex;

            for (int i = 0; i < worldVertexCountEdge; i += 24)
            {
                for (int j = 0; j < worldVertexCountEdge; j += 24)
                {
                    if (vertexTextureMapQuality1[i + j * worldVertexCountEdge] == (byte)vertexMapTextureNames.forest)
                    {
                        tempPosX = (RandomValuesSeed.strictNoise(i * 0.9f,j * 0.9f) * 30  + i) * worldSubMeshVertexDistance;
                        tempPosY = ( RandomValuesSeed.strictNoise(i * 0.8f, j * 0.8f) * 30 +j) * worldSubMeshVertexDistance;

                        getHeightmapPointInfo(ref tempPosX, ref tempPosY, out temp_height, out tempFertility, out normal, out textureIndex);

                        Vector3 position = new Vector3(tempPosX, temp_height, tempPosY);
                        //Vector3 position = new Vector3(i * worldSubMeshVertexDistance, VertexHeightMapQuality1[i + j * worldVertexCountEdge], j * worldSubMeshVertexDistance);
                        int randomNumber = RandomValuesSeed.getRandomValueSeed(i, j, 3);
                        if (randomNumber == 0)
                        {
                            TreePositionsLists[0].Add(position);
                        }
                        if (randomNumber == 1)
                        {
                            TreePositionsLists[1].Add(position);
                        }
                        if (randomNumber == 2)
                        {
                            TreePositionsLists[2].Add(position);
                        }
                    }
                }
            }

            */

            treesInitialSetupPending = false;
        }

        bool isWithinRange = false;
        List<GameObject> objToRemove = new List<GameObject>();
        int treePrefabIndex = -1;

        // remove 3D trees out of range
        if (destroyTrees)
        {
            foreach (GameObject activeTree in ActiveTreesCloseList)
            {
                foreach (GameObject viewPoint in WorldViewpointsList)
                {
                    isWithinRange = false;
                    if (Vector3.Distance(activeTree.transform.position, viewPoint.transform.position) < treeRenderDistanceMax)
                    {
                        isWithinRange = true;
                        break;
                    }
                }

                if (!isWithinRange)
                {
                    objToRemove.Add(activeTree);
                }
            }

            foreach (GameObject removeObj in objToRemove)
            {
                ActiveTreesCloseList.Remove(removeObj);
                treePrefabIndex = findTreePrefabByName(removeObj.name);
                free3DTreeObjs[treePrefabIndex].Add(removeObj);
                removeObj.SetActive(false);
            }
        }

        // create new 3D trees and provide distant tree list 

        if (m_createTreeObjects)
        {
            foreach (List<Vector3> distantTreeList in TreesDistantLists)
            {
                distantTreeList.Clear();
            }

            bool treeAlreadyActive = false;
            for (int i = 0; i < TreePositionsLists.Length; i++)
            {
                foreach (Vector3 treePos in TreePositionsLists[i])
                {
                    isWithinRange = false;
                    foreach (GameObject viewPoint in WorldViewpointsList)
                    {
                        if (Vector3.Distance(treePos, viewPoint.transform.position) < treeRenderDistanceMax)
                        {
                            isWithinRange = true;
                            break;
                        }
                    }

                    if (isWithinRange)
                    {
                        treeAlreadyActive = false;
                        foreach (GameObject activeTree in ActiveTreesCloseList)
                        {
                            if (activeTree.transform.position == treePos)
                            {
                                treeAlreadyActive = true;
                                break;
                                // tree already active --> skip
                            }
                        }
                        if (!treeAlreadyActive)
                        {
                            Quaternion rot = Quaternion.Euler(new Vector3(0, Mathf.PerlinNoise(treePos.x * 1.2f, treePos.z * 1.2f) * 360, 0));
                            GameObject tempObj;
                            if (free3DTreeObjs[i].Count > 0)
                            {
                                tempObj = free3DTreeObjs[i][0];
                                free3DTreeObjs[i].RemoveAt(0);
                                tempObj.transform.position = treePos;
                                tempObj.transform.rotation = rot;
                                tempObj.SetActive(true);
                            }
                            else
                            {
                                tempObj = Instantiate(treePrefabs[i], treePos, rot);
                                tempObj.name = treePrefabs[i].name;
                            }

                            if (hide3DTreesHierarchy)
                            {
                                tempObj.hideFlags = HideFlags.HideInHierarchy;
                            }
                            ActiveTreesCloseList.Add(tempObj);
                        }

                    }
                    else
                    {
                        TreesDistantLists[i].Add(treePos);
                    }
                }
            }
        }

    }

    private List<GameObject>[] activeTerrainObjs = null;
    private bool terrainObjRenderingRunnig = false;
    private const int cost_FindRemove_TerrainRendering = 10;
    private const int cost_FindCreate_TerrainRendering = 10;
    private IEnumerator terrainObjRenderingCoroutine()
    {
        int currentCost = 0;
        terrainObjRenderingRunnig = true;
        bool isObjActiveTemp;
        bool isWithinRangeTemp;
        List<Vector3>[] positionsToCreate = new List<Vector3>[terrainObjectsPrefabs.Length];
        List<GameObject>[] ObjToRemove = new List<GameObject>[terrainObjectsPrefabs.Length];
        List<Quaternion>[] rotationsToCreate = new List<Quaternion>[terrainObjectsPrefabs.Length];

        for (int i = 0; i < terrainObjectsPrefabs.Length; i++)
        {
            positionsToCreate[i] = new List<Vector3>();
            ObjToRemove[i] = new List<GameObject>();
            rotationsToCreate[i] = new List<Quaternion>();
        }

        // find objs to remove
        for (int i = 0; i < activeTerrainObjs.Length; i++)
        {
            for (int j = 0; j < activeTerrainObjs[i].Count; j++)
            {
                isWithinRangeTemp = false;
                foreach (GameObject viewPoint in WorldViewpointsList)
                {
                    if (Vector3.Distance(activeTerrainObjs[i][j].transform.position, viewPoint.transform.position) < terrainObjectsMaxRenderDistance)
                    {
                        isWithinRangeTemp = true;
                        break;
                    }
                }
                if (!isWithinRangeTemp)
                {
                    ObjToRemove[i].Add(activeTerrainObjs[i][j]);
                }

                currentCost += cost_FindRemove_TerrainRendering;
                if (currentCost > maxCostTerrainRendering)
                {
                    currentCost = 0;
                    yield return null;
                }
            }
        }

        Vector3 tempPos;
        // find objs to create
        for (int i = 0; i < TerrainObjPositions.Length; i++)
        {
            // for every obj typ
            for (int j = 0; j < TerrainObjPositions[i].Length; j++)
            {
                // for every obj of one typ
                isObjActiveTemp = false;

                isWithinRangeTemp = false;
                foreach (GameObject viewPoint in WorldViewpointsList)
                {
                    if (Vector3.Distance(TerrainObjPositions[i][j], viewPoint.transform.position) < terrainObjectsMaxRenderDistance)
                    {
                        isWithinRangeTemp = true;
                        break;
                    }
                }

                for (int k = 0; k < activeTerrainObjs[i].Count; k++)
                {
                    // for every active obj of one typ
                    if (TerrainObjPositions[i][j] == activeTerrainObjs[i][k].transform.position)
                    {
                        isObjActiveTemp = true;
                        break;
                    }
                }

                if (!isObjActiveTemp && isWithinRangeTemp)
                {
                    tempPos = TerrainObjPositions[i][j];
                    positionsToCreate[i].Add(tempPos);
                    rotationsToCreate[i].Add(RandomValuesSeed.getRandomRotationYAxis(tempPos.x + tempPos.y + tempPos.z));
                }

                currentCost += cost_FindCreate_TerrainRendering;
                if (currentCost > maxCostTerrainRendering)
                {
                    currentCost = 0;
                    yield return null;
                }
            }
        }

        yield return null;

        // remove objs
        for (int i = 0; i < ObjToRemove.Length; i++)
        {
            // for every obj typ
            for (int j = 0; j < ObjToRemove[i].Count; j++)
            {
                recyleTerrainObj(ObjToRemove[i][j], i);
            }
        }

        // create objs
        for (int i = 0; i < positionsToCreate.Length; i++)
        {
            // for every obj typ
            for (int j = 0; j < positionsToCreate[i].Count; j++)
            {
                createTerrainObj(positionsToCreate[i][j], rotationsToCreate[i][j], i);
            }
        }

        terrainObjRenderingRunnig = false;
        updateIEnumeratorPosition = UpdateIEnumeratorPosition.done;
        yield return null;
    }

    private List<GameObject>[] freeTerrainObjects = null;
    private void recyleTerrainObj(GameObject objToRecyle, int terrainObjIndex)
    {
        activeTerrainObjs[terrainObjIndex].Remove(objToRecyle);
        objToRecyle.SetActive(false);
        freeTerrainObjects[terrainObjIndex].Add(objToRecyle);
    }
    private void createTerrainObj(Vector3 position, Quaternion rotation, int terrainObjIndex)
    {
        GameObject currentObj;
        if (freeTerrainObjects[terrainObjIndex].Count > 0)
        {
            currentObj = freeTerrainObjects[terrainObjIndex][0];
            freeTerrainObjects[terrainObjIndex].RemoveAt(0);
            currentObj.SetActive(true);
        }
        else
        {
            currentObj = Instantiate(terrainObjectsPrefabs[terrainObjIndex]) as GameObject;
#if DEBUG
            if (hideTerrainObjsInspector)
            {
                currentObj.hideFlags = HideFlags.HideInHierarchy;
            }
#endif
        }

        currentObj.transform.position = position;
        currentObj.transform.rotation = rotation;
        activeTerrainObjs[terrainObjIndex].Add(currentObj);
    }

    private IEnumerator treesDistantManagement()
    {
        //Debug.Log("distant tree start time: " + Time.realtimeSinceStartup);
        int IEnumCounter = 0;
        for (int i = 0; i < TreesDistantLists.Length; i++)
        {
            List<Matrix4x4> matricesDistanceTreesList = new List<Matrix4x4>();
            Quaternion rot;
            Vector2 distanceVec2;

            foreach (Vector3 distanceTreePos in TreesDistantLists[i])
            {
                distanceVec2 = LastWorldViewpointsSnapshot.transformPositions[0] - new Vector2(distanceTreePos.x, distanceTreePos.z);
                rot = Quaternion.LookRotation(new Vector3(distanceVec2.x, 0.0f, distanceVec2.y));
                rot = Quaternion.Euler(new Vector3(0, rot.eulerAngles.y, 0));
                matricesDistanceTreesList.Add(Matrix4x4.TRS(distanceTreePos + new Vector3(0f, distantTreeOffsetY[i], 0f), rot, Vector3.one * distantTreeSize[i]));

                IEnumCounter += 2;
                if (IEnumCounter > maxDistantTreeManagementActionsPerFrame)
                {
                    IEnumCounter = 0;
                    //Debug.Log("dropping out of distant tree management 1");
                    yield return null;
                }
            }

            int arraySize = (int)(matricesDistanceTreesList.Count / 1023) + 1;
            tempMatricesDistantTrees[i] = new Matrix4x4[arraySize][];

            List<Matrix4x4> matricesDistanceTreesListOneArray = new List<Matrix4x4>();
            int arrayCounter = 0;
            for (int j = 0; j < matricesDistanceTreesList.Count; j++)
            {
                if (matricesDistanceTreesListOneArray.Count > 1022)
                {
                    tempMatricesDistantTrees[i][arrayCounter] = matricesDistanceTreesListOneArray.ToArray();
                    matricesDistanceTreesListOneArray.Clear();
                    arrayCounter++;
                }
                matricesDistanceTreesListOneArray.Add(matricesDistanceTreesList[j]);
                IEnumCounter += 1;
                if (IEnumCounter > maxDistantTreeManagementActionsPerFrame)
                {
                    IEnumCounter = 0;
                    //Debug.Log("dropping out of distant tree management 2");
                    yield return null;
                }
            }

            if (matricesDistanceTreesListOneArray.Count != 0)
            {
                tempMatricesDistantTrees[i][arrayCounter] = matricesDistanceTreesListOneArray.ToArray();
            }
        }
        for (int i = 0; i < matricesDistantTrees.Length; i++)
        {
            matricesDistantTrees[i] = tempMatricesDistantTrees[i];
        }

        updateIEnumeratorPosition = UpdateIEnumeratorPosition.rockManagement;
        treesDistanceManagementRunning = false;

        //Debug.Log("distant tree end time: " + Time.realtimeSinceStartup);
    }

    private List<GameObject>[] rocksToRemove;
    private List<int>[] rocksToAddIndex;
    private IEnumerator rockManagement() // old
    {
        int i;

        // Debug.Log("started rockManagement: " + Time.realtimeSinceStartup);
        //Debug.Log("vector pos count: " +rocksPositions.Length);
        int iterationCounter = 0;
        Vector2 rockposXZ = new Vector2(0, 0);

        rocksToAddIndex = new List<int>[rocksPrefabs.Length];
        rocksToRemove = new List<GameObject>[rocksPrefabs.Length];
        for (i = 0; i < rocksToRemove.Length; i++)
        {
            rocksToRemove[i] = new List<GameObject>();
            rocksToAddIndex[i] = new List<int>();
        }


        //  rocks to remove
        for (i = 0; i < rocksPositions.Length; i++)
        {
            foreach (GameObject rockObj in activeRocksObj[i])
            {
                rockposXZ.x = rockObj.transform.position.x;
                rockposXZ.y = rockObj.transform.position.z;

                if (Vector2.Distance(LastWorldViewpointsSnapshot.transformPositions[0], rockposXZ) > rocksDistance)
                {
                    rocksToRemove[i].Add(rockObj);
                }

                if (iterationCounter > rocksCalcIterations)
                {
                    iterationCounter = 0;
                    yield return null;
                }
                iterationCounter += 2;
            }
        }

        //Debug.Log("found to remove: "+rocksToRemove.Count);

        // rocks to add
        bool rocksAlreadyActive;
        for (int j = 0; j < rocksPositions.Length; j++)
        {
            for (i = 0; i < rocksPositions[j].Length; i++)
            {
                rockposXZ.x = rocksPositions[j][i].x;
                rockposXZ.y = rocksPositions[j][i].z;
                if (Vector2.Distance(LastWorldViewpointsSnapshot.transformPositions[0], rockposXZ) < rocksDistance)
                {
                    rocksAlreadyActive = false;
                    foreach (GameObject rockObj in activeRocksObj[j])
                    {
                        if (rockObj.transform.position == rocksPositions[j][i])
                        {
                            // rock is already active --> doesnt need to get created
                            rocksAlreadyActive = true;
                            break;
                        }

                        if (iterationCounter > rocksCalcIterations)
                        {
                            iterationCounter = 0;
                            yield return null;
                        }
                        iterationCounter++;
                    }
                    if (!rocksAlreadyActive)
                    {
                        rocksToAddIndex[j].Add(i);
                    }
                }

                if (iterationCounter > rocksCalcIterations)
                {
                    iterationCounter = 0;
                    yield return null;
                }
                iterationCounter += 2;
            }
        }

        //Debug.Log("found to add: "+rocksToAddIndex.Count);

        GameObject ObjToSpawn;
        for (i = 0; i < rocksToAddIndex.Length; ++i)
        {
            foreach (int localRockToAddIndex in rocksToAddIndex[i])
            {
                if (freeRocksObj[i].Count > 0)
                {
                    ObjToSpawn = freeRocksObj[i][0];
                    freeRocksObj[i].RemoveAt(0);
                    ObjToSpawn.transform.position = rocksPositions[i][localRockToAddIndex];
                    ObjToSpawn.transform.rotation = rocksRotations[i][localRockToAddIndex];
                    ObjToSpawn.SetActive(true);
                }
                else
                {
                    ObjToSpawn = Instantiate(rocksPrefabs[i], rocksPositions[i][localRockToAddIndex], rocksRotations[i][localRockToAddIndex]);
                    ObjToSpawn.GetComponent<Renderer>().material = rocksMaterial;
                }
                activeRocksObj[i].Add(ObjToSpawn);

                if (hideRocksInHierarchy)
                {
                    ObjToSpawn.hideFlags = HideFlags.HideInHierarchy;
                }

                if (iterationCounter > rockSpawningRemovingIterations)
                {
                    iterationCounter = 0;
                    yield return null;
                }
                iterationCounter++;
            }
            rocksToAddIndex[i].Clear();

            foreach (GameObject rockToRemove in rocksToRemove[i])
            {
                activeRocksObj[i].Remove(rockToRemove);
                rockToRemove.SetActive(false);
                freeRocksObj[i].Add(rockToRemove);

                if (iterationCounter > rockSpawningRemovingIterations)
                {
                    iterationCounter = 0;
                    yield return null;
                }
                iterationCounter++;
            }
            rocksToRemove[i].Clear();
        }


        updateIEnumeratorPosition = UpdateIEnumeratorPosition.terrainObjManagement;
        rockManagementRunning = false;

        //Debug.Log("ended rockManagement: " + Time.realtimeSinceStartup);
    }

    private WorldViewPointsSnapshot LastWorldViewpointsSnapshot = null;

    private void takeCurrentWorldViewpointsSnapshot()
    {
        if (LastWorldViewpointsSnapshot != null)
        {
            LastWorldViewpointsSnapshot.cleanUp();
        }
        LastWorldViewpointsSnapshot = null;
        LastWorldViewpointsSnapshot = new WorldViewPointsSnapshot(WorldViewpointsList);
    }

    public void removeWoldViewpoint(GameObject WorldViewpointToRemove)
    {
        if (!WorldViewpointsList.Remove(WorldViewpointToRemove))
        {
            Debug.LogError("can't remove worldviewpoint from list: " + WorldViewpointToRemove.name + ";" + WorldViewpointToRemove.GetHashCode());
        }
    }

    private void buildVertexTextureMapEM()
    {
        vertexTextureMapQuality1 = new byte[worldVertexCountEdge, worldVertexCountEdge];

        float maxSnowHeightDelta = m_snowHeightMax - m_snowHeightMin;
        float currentVertHeight;
        int currentVertIndex;
        for (int i = 0; i < worldVertexCountEdge; i++)
        {
            for (int j = 0; j < worldVertexCountEdge; j++)
            {
                currentVertIndex = i + j * worldVertexCountEdge;
                currentVertHeight = VertexHeightMapQuality1[i, j];

                if (m_placeholderTextures[i, j] != PlaceholderTextureNames.Not_Set)
                {
                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.road_trail;
                    continue;
                }

                if (terrainNormalsAngle[currentVertIndex] > m_texAngleMinSteepMountain)
                {
                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.mountain_steep;
                }
                else if (terrainNormalsAngle[currentVertIndex] > m_texAngleMinMountain)
                {
                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.rock;
                }
                else if (currentVertHeight > m_snowHeightMax) // snow
                {
                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.snow;
                }
                else if (currentVertHeight > m_snowHeightMin && m_snowDeltaCurve.Evaluate((currentVertHeight - m_snowHeightMin) / maxSnowHeightDelta) * m_snowDeltaHeightWeight > 2) // chance for snow
                {
                    // TODO Mike: maybe add perlin noise multiplicator to m_snowDeltaCurve
                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.snow;
                }
                else
                {
                    if (currentVertHeight < m_texUnderwaterHeight)
                    {
                        vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.underwater;
                    }
                    else if (currentVertHeight < m_texBeachHeight)
                    {
                        if (terrainNormalsAngle[currentVertIndex] > m_texAngleMinBeachTransition)
                        {
                            vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.beachGrassTransition;
                        }
                        else
                        {
                            vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.beachsand;
                        }
                    }
                    else if (currentVertHeight < m_texBeachTrasitionHeight) // between beach and grass
                    {
                        vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.beachGrassTransition;
                    }
                    else
                    {
                        float moisture = getMoistureValue(i, j);
                        float treeNoise = getTreeOctave(i, j);

                        if (currentVertHeight < m_lowAltitudeHeight)
                        {
                            if (moisture > 0.2f)
                            {
                                vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.grass;

                                if (treeNoise > m_minForestsAmplitude && moisture > 0.7f)
                                {
                                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.forest;
                                }
                            }
                            else if (moisture > 0.1f)
                            {
                                vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.deadGrass;
                            }
                            else
                            {
                                vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.dirt;
                            }
                        }
                        else if (currentVertHeight < m_mediumAltitudeHeight)
                        {
                            if (moisture > 0.7f)
                            {
                                vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.grass;

                                if (treeNoise > m_minForestsAmplitude)
                                {
                                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.forest;
                                }
                            }
                            else if (moisture > 0.18f)
                            {
                                vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.deadGrass;

                                if (treeNoise > m_minForestsAmplitude && moisture > 0.5f)
                                {
                                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.forest;
                                }
                            }
                            else
                            {
                                vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.dirt;
                            }
                        }
                        else
                        {
                            if (moisture > 0.7f)
                            {
                                vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.deadGrass;

                                if (treeNoise > m_minForestsAmplitude)
                                {
                                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.forest;
                                }
                            }
                            else if (moisture > 0.35f)
                            {
                                vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.dirt;
                            }
                            else
                            {
                                vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.snow;
                            }
                        }
                    }
                }

            }
        }

        //saveTextureMapToDisk(@"C:\TEMP\Unity\TextureMap.png");
    }

    /*
    private void buildVertexTextureMapFertility(int vertexCountEdge)
    {
        WorldLandmassBuilderOctave treeOctave = new WorldLandmassBuilderOctave(new WorldLandmassBuilderOctaveProperties(m_forestsOctave_frequency, 1, currentSeed + 27, currentSeed + 27, m_forestsOctave_smoothCount, m_forestsOctave_curves.keys, m_forestsOctave_scaleCount), vertexCountEdge);
        treeOctave.start();

        vertexTextureMapQuality1 = new byte[worldVertexCountEdge, worldVertexCountEdge];

        float maxSnowHeightDelta = m_snowHeightMax - m_snowHeightMin;
        float currentVertHeight;
        int currentVertIndex;
        for (int i = 0; i < worldVertexCountEdge; i++)
        {
            for (int j = 0; j < worldVertexCountEdge; j++)
            {
                currentVertIndex = i + j * worldVertexCountEdge;
                currentVertHeight = VertexHeightMapQuality1[i, j];

                vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.forest;

                if (m_placeholderTextures[i, j] != PlaceholderTextureNames.Not_Set)
                {
                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.road_trail;
                    continue;
                }

                if (terrainNormalsAngle[currentVertIndex] > texAngleMinSteepMountain)
                {
                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.mountain_steep;
                }
                else if (terrainNormalsAngle[currentVertIndex] > texAngleMinMountain)
                {
                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.rock;
                }
                else if (currentVertHeight > m_snowHeightMax) // snow
                {
                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.snow;
                }
                else if (currentVertHeight > m_snowHeightMin && WorldFertility[i, j] + m_snowDeltaCurve.Evaluate((currentVertHeight - m_snowHeightMin) / maxSnowHeightDelta) * m_snowDeltaHeightWeight > 2) // chance for snow
                {
                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.snow;
                }
                else
                {
                    if (currentVertHeight < texUnderwaterHeightMax)
                    {
                        vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.underwater;
                    }
                    else if (currentVertHeight < texBeachHeightMax)
                    {
                        if (terrainNormalsAngle[currentVertIndex] > texAngleMinBeachTransition)
                        {
                            vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.beachGrassTransition;
                        }
                        else
                        {
                            vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.beachsand;
                        }
                    }
                    else if (currentVertHeight < 27) // between beach and grass
                    {
                        vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.beachGrassTransition;
                    }
                    else
                    {
                        if (WorldFertility[i, j] > greenGrassFertility)
                        {
                            vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.grass;
                        }
                        else if (WorldFertility[i, j] > deadGrassFertility)
                        {
                            vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.deadGrass;
                        }
                        else
                        {
                            vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.dirt;
                        }
                    }
                }

            }
        }
        

        // trees

        while (!treeOctave.isDone)
        {
            Thread.Sleep(10);
        }

        //float debug_start_time = Time.realtimeSinceStartup;

        for (int i = 0; i < vertexCountEdge; i++)
        {
            for (int j = 0; j < vertexCountEdge; j++)
            {
                if (m_placeholderTextures[i, j] != PlaceholderTextureNames.Not_Set)
                {
                    continue;
                }

                if (VertexHeightMapQuality1[i, j] > texBeachHeightMax && treeOctave.m_result[i, j] * WorldFertility[i, j] * m_forestFertilityFactor > m_minForestsFertility)
                {
                    vertexTextureMapQuality1[i, j] = (byte)vertexMapTextureNames.forest;
                }
            }
        }

        treeOctave.dispose();

        //Debug.Log("FORESTS Time: " + (Time.realtimeSinceStartup - debug_start_time));
        //Debug.Log("FORESTS: " +debugCounter);
    }

    */

    public float getHeightmapY(Vector2 position)
    {
        if (position.x / worldSubMeshVertexDistance >= worldVertexCountEdge || position.y / worldSubMeshVertexDistance >= worldVertexCountEdge || position.x < 0 || position.y < 0)
        {
            return 0;
        }

        float unused3;
        float height;
        Vector3 unused4;
        byte unused5;

        getHeightmapPointInfo(ref position.x, ref position.y, out height, out unused3, out unused4, out unused5);

        return height;
    }

    public void getHeightmapPointInfo(ref float posXWorld, ref float posYWorld, out float height, out float fertility, out Vector3 normal, out byte textureIndex)
    {
        /*
            D   C

            A   B
         */

        //worldSubMeshVertexDistance
        //VertexHeightMapQuality1

        int IndexAX;
        int IndexAY;

        IndexAX = (int)(posXWorld / worldSubMeshVertexDistance);
        IndexAY = (int)(posYWorld / worldSubMeshVertexDistance);

        if (VertexHeightMapQuality1 != null && VertexHeightMapQuality1.isCompressed && (IndexAX + 1) < VertexHeightMapQuality1.GetLength(0) && (IndexAY + 1) < VertexHeightMapQuality1.GetLength(1) && IndexAX > -1 && IndexAY > -1)
        {
            textureIndex = vertexTextureMapQuality1[IndexAX, IndexAY];

            Vector3 vecAHeight = new Vector3(IndexAX * worldSubMeshVertexDistance, VertexHeightMapQuality1[IndexAX, IndexAY], IndexAY * worldSubMeshVertexDistance); // index 0
            Vector3 vecBHeight = new Vector3((IndexAX + 1) * worldSubMeshVertexDistance, VertexHeightMapQuality1[IndexAX + 1, IndexAY], IndexAY * worldSubMeshVertexDistance); // index 1
            Vector3 vecCHeight = new Vector3((IndexAX + 1) * worldSubMeshVertexDistance, VertexHeightMapQuality1[IndexAX + 1, IndexAY + 1], (IndexAY + 1) * worldSubMeshVertexDistance); // index 2
            Vector3 vecDHeight = new Vector3(IndexAX * worldSubMeshVertexDistance, VertexHeightMapQuality1[IndexAX, IndexAY + 1], (IndexAY + 1) * worldSubMeshVertexDistance); // index 3

            Vector3 vecAFertility = new Vector3(IndexAX * worldSubMeshVertexDistance, 0, IndexAY * worldSubMeshVertexDistance); // index 0
            Vector3 vecBFertility = new Vector3((IndexAX + 1) * worldSubMeshVertexDistance, 0, IndexAY * worldSubMeshVertexDistance); // index 1
            Vector3 vecCFertility = new Vector3((IndexAX + 1) * worldSubMeshVertexDistance, 0, (IndexAY + 1) * worldSubMeshVertexDistance); // index 2
            Vector3 vecDFertility = new Vector3(IndexAX * worldSubMeshVertexDistance, 0, (IndexAY + 1) * worldSubMeshVertexDistance); // index 3

            Vector2 vecAxy = new Vector2(IndexAX * worldSubMeshVertexDistance, IndexAY * worldSubMeshVertexDistance); // index 0
            Vector2 vecBxy = new Vector2((IndexAX + 1) * worldSubMeshVertexDistance, IndexAY * worldSubMeshVertexDistance); // index 1
            Vector2 vecCxy = new Vector2((IndexAX + 1) * worldSubMeshVertexDistance, (IndexAY + 1) * worldSubMeshVertexDistance); // index 2
            Vector2 vecDxy = new Vector2(IndexAX * worldSubMeshVertexDistance, (IndexAY + 1) * worldSubMeshVertexDistance); // index 3

            Vector2 inPoint = new Vector2(posXWorld, posYWorld);

            int indexFurthestPoint = 0;
            float futherstDistance = float.MinValue;
            float currentDistance;

            currentDistance = Vector2.Distance(inPoint, vecAxy);
            if (currentDistance > futherstDistance)
            {
                futherstDistance = currentDistance;
                indexFurthestPoint = 0;
            }

            currentDistance = Vector2.Distance(inPoint, vecBxy);
            if (currentDistance > futherstDistance)
            {
                futherstDistance = currentDistance;
                indexFurthestPoint = 1;
            }

            currentDistance = Vector2.Distance(inPoint, vecCxy);
            if (currentDistance > futherstDistance)
            {
                futherstDistance = currentDistance;
                indexFurthestPoint = 2;
            }

            currentDistance = Vector2.Distance(inPoint, vecDxy);
            if (currentDistance > futherstDistance)
            {
                futherstDistance = currentDistance;
                indexFurthestPoint = 3;
            }

            Vector3 triangleHeightNormal = Vector3.zero;
            Vector3 triangleFertilityNormal = Vector3.zero;
            Vector3 trianglePointHeight = Vector3.zero;
            Vector3 trianglePointFertility = Vector3.zero;

            if (indexFurthestPoint == 0)
            {
                // A missing
                triangleHeightNormal = Vector3.Cross(vecBHeight - vecCHeight, vecDHeight - vecCHeight);
                trianglePointHeight = vecBHeight;
                triangleFertilityNormal = Vector3.Cross(vecBFertility - vecCFertility, vecDFertility - vecCFertility);
                trianglePointFertility = vecBFertility;
            }
            else if (indexFurthestPoint == 1)
            {
                // B missing
                triangleHeightNormal = Vector3.Cross(vecCHeight - vecDHeight, vecAHeight - vecDHeight);
                trianglePointHeight = vecAHeight;
                triangleFertilityNormal = Vector3.Cross(vecCFertility - vecDFertility, vecAFertility - vecDFertility);
                trianglePointFertility = vecAFertility;
            }
            else if (indexFurthestPoint == 2)
            {
                // C missing
                triangleHeightNormal = Vector3.Cross(vecDHeight - vecAHeight, vecBHeight - vecAHeight);
                trianglePointHeight = vecAHeight;
                triangleFertilityNormal = Vector3.Cross(vecDFertility - vecAFertility, vecBFertility - vecAFertility);
                trianglePointFertility = vecAFertility;
            }
            else if (indexFurthestPoint == 3)
            {
                // D missing
                triangleHeightNormal = Vector3.Cross(vecAHeight - vecBHeight, vecCHeight - vecBHeight);
                trianglePointHeight = vecAHeight;
                triangleFertilityNormal = Vector3.Cross(vecAFertility - vecBFertility, vecCFertility - vecBFertility);
                trianglePointFertility = vecAFertility;
            }
            triangleHeightNormal = Vector3.Normalize(triangleHeightNormal);
            triangleFertilityNormal = Vector3.Normalize(triangleFertilityNormal);


            height = trianglePointHeight.y + (triangleHeightNormal.z * (trianglePointHeight.z - inPoint.y) + triangleHeightNormal.x * (trianglePointHeight.x - inPoint.x)) / triangleHeightNormal.y;
            fertility = trianglePointFertility.y + (triangleFertilityNormal.z * (trianglePointFertility.z - inPoint.y) + triangleFertilityNormal.x * (trianglePointFertility.x - inPoint.x)) / triangleFertilityNormal.y;
            normal = triangleHeightNormal;
        }
        else
        {
            height = 0;
            fertility = 0;
            normal = Vector3.up;
            textureIndex = 0;
        }
    }

    private void saveTextureMapToDisk(string _fullPath)
    {
        Color[] pixels = new Color[vertexTextureMapQuality1.GetLength(0) * vertexTextureMapQuality1.GetLength(1)];

        for (int i = 0; i < vertexTextureMapQuality1.GetLength(0); i++)
        {
            for (int j = 0; j < vertexTextureMapQuality1.GetLength(1); j++)
            {
                Color tempColor;

                switch ((vertexMapTextureNames)vertexTextureMapQuality1[i, j])
                {
                    case vertexMapTextureNames.beachGrassTransition:
                        {
                            tempColor = new Color(0.75f, 1f, 0.25f);
                            break;
                        }
                    case vertexMapTextureNames.beachsand:
                        {
                            tempColor = Color.yellow;
                            break;
                        }
                    case vertexMapTextureNames.deadGrass:
                        {
                            tempColor = new Color(0.75f, 1f, 0.75f);
                            break;
                        }
                    case vertexMapTextureNames.dirt:
                        {
                            tempColor = new Color(0.5f, 0.25f, 0);
                            break;
                        }
                    case vertexMapTextureNames.forest:
                        {
                            tempColor = new Color(0.75f, 0.25f, 0);
                            break;
                        }
                    case vertexMapTextureNames.grass:
                        {
                            tempColor = Color.green;
                            break;
                        }
                    case vertexMapTextureNames.mountain_steep:
                        {
                            tempColor = new Color(0.25f, 0.25f, 0.25f);
                            break;
                        }
                    case vertexMapTextureNames.rock:
                        {
                            tempColor = new Color(0.5f, 0.5f, 0.5f);
                            break;
                        }
                    case vertexMapTextureNames.road_trail:
                        {
                            tempColor = new Color(0.75f, 0.5f, 0.5f);
                            break;
                        }
                    case vertexMapTextureNames.snow:
                        {
                            tempColor = Color.white;
                            break;
                        }
                    case vertexMapTextureNames.underwater:
                        {
                            tempColor = Color.blue;
                            break;
                        }
                    default:
                        {
                            tempColor = Color.black;
                            break;
                        }
                }

                pixels[i + j * vertexTextureMapQuality1.GetLength(0)] = tempColor;
            }
        }

        Texture2D tex = new Texture2D(vertexTextureMapQuality1.GetLength(0), vertexTextureMapQuality1.GetLength(1));
        tex.SetPixels(pixels);
        tex.Apply();

        SaveTextureAsPNG(tex, _fullPath);
    }

    private void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
    {
        byte[] _bytes = _texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
        Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
    }

    /*
    private float[] fixedPointsSmoothingGPU(float[] fixedPoints, int edgeSizeX, int edgeSizeY, int smoothCount)
    {
        const int smoothShaderDivisor = 8;

        int extendedEdgeSizeX = edgeSizeX + 2;
        int extendedEdgeSizeY = edgeSizeY + 2;

        if (edgeSizeX % smoothShaderDivisor != 0)
        {
            Debug.LogError("edgeSizeX is not dividable by " + smoothShaderDivisor);
        }
        if (edgeSizeY % smoothShaderDivisor != 0)
        {
            Debug.LogError("edgeSizeY is not dividable by " + smoothShaderDivisor);
        }

        float[] extendedArray = new float[extendedEdgeSizeX * extendedEdgeSizeY];

        for (int i = 0; i < edgeSizeX; i++)
        {
            for (int j = 0; j < edgeSizeY; j++)
            {
                extendedArray[i + j * extendedEdgeSizeX] = fixedPoints[i + j * edgeSizeX];
            }
        }

        outputTestTexture2 = new Texture2D(edgeSizeX, edgeSizeY);
        outputTestTexture2.SetPixels(floatToColor(fixedPoints));
        outputTestTexture2.Apply();

        int smoothKernel = computeShaderWorldGeneration.FindKernel("smoothArray");
        int smoothFixedKernel = computeShaderWorldGeneration.FindKernel("moveAndFixedSmoothArrays");
        computeShaderWorldGeneration.SetInt("ArraySizeX", extendedEdgeSizeX);

        ComputeBuffer bufferInputSmooth = new ComputeBuffer(extendedArray.Length, sizeof(float));
        ComputeBuffer bufferOutputSmooth = new ComputeBuffer(extendedArray.Length, sizeof(float));
        ComputeBuffer bufferFixedSmooth = new ComputeBuffer(extendedArray.Length, sizeof(float));
        //ComputeBuffer bufferInputWait = new ComputeBuffer(1, sizeof(float));
        //ComputeBuffer bufferFixedWait = new ComputeBuffer(1, sizeof(float));

        bufferInputSmooth.SetData(extendedArray);
        bufferOutputSmooth.SetData(extendedArray);
        bufferFixedSmooth.SetData(extendedArray);
        //bufferInputWait.SetData(new float[1]{0});
        //bufferFixedWait.SetData(new float[1]{0});

        computeShaderWorldGeneration.SetBuffer(smoothKernel, "smoothMapInput", bufferInputSmooth);
        computeShaderWorldGeneration.SetBuffer(smoothKernel, "smoothMapOutput", bufferOutputSmooth);
        //computeShaderWorldGeneration.SetBuffer(smoothKernel,"smoothWaitInput",bufferInputWait);

        computeShaderWorldGeneration.SetBuffer(smoothFixedKernel, "smoothMapInput", bufferInputSmooth);
        computeShaderWorldGeneration.SetBuffer(smoothFixedKernel, "smoothMapFixed", bufferFixedSmooth);
        computeShaderWorldGeneration.SetBuffer(smoothFixedKernel, "smoothMapOutput", bufferOutputSmooth);
        //computeShaderWorldGeneration.SetBuffer(smoothFixedKernel,"smoothWaitFixed",bufferFixedWait);

        float[] tempReturn = new float[1];
        for (int i = 0; i < smoothCount; i++)
        {
            computeShaderWorldGeneration.Dispatch(smoothKernel, edgeSizeX / smoothShaderDivisor, edgeSizeY / smoothShaderDivisor, 1);
            //bufferInputWait.GetData(tempReturn);
            computeShaderWorldGeneration.Dispatch(smoothFixedKernel, edgeSizeX / smoothShaderDivisor, edgeSizeY / smoothShaderDivisor, 1);
            //bufferFixedWait.GetData(tempReturn);
        }

        float[] tempArray = new float[extendedArray.Length];
        float[] outputArray = new float[edgeSizeX * edgeSizeY];

        bufferOutputSmooth.GetData(tempArray);

        for (int i = 0; i < edgeSizeX; i++)
        {
            for (int j = 0; j < edgeSizeY; j++)
            {
                outputArray[i + j * edgeSizeX] = tempArray[i + j * extendedEdgeSizeX];
            }
        }

        outputTestTexture3 = new Texture2D(edgeSizeX, edgeSizeY);
        outputTestTexture3.SetPixels(floatToColor(outputArray));
        outputTestTexture3.Apply();

        SaveTextureAsPNG(outputTestTexture3, Application.dataPath + "/IngameExport/GPUTest.png");

        bufferInputSmooth.Release();
        bufferOutputSmooth.Release();
        bufferFixedSmooth.Release();

        bufferInputSmooth.Dispose();
        bufferOutputSmooth.Dispose();
        bufferFixedSmooth.Dispose();

        return outputArray;
    }
    */

    private float sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        bool b1, b2, b3;

        b1 = sign(pt, v1, v2) < 0.0f;
        b2 = sign(pt, v2, v3) < 0.0f;
        b3 = sign(pt, v3, v1) < 0.0f;

        return ((b1 == b2) && (b2 == b3));
    }

    private float DistanceToLine(Vector3 lineOrigin, Vector3 lineDir, Vector3 point)
    {
        return Vector3.Cross(lineDir, point - lineOrigin).magnitude;
    }

    private Vector2 rotateVector2(Vector2 input, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        float tx = input.x;
        float ty = input.y;
        input.x = (cos * tx) - (sin * ty);
        input.y = (sin * tx) + (cos * ty);
        return input;
    }

    private class smoothNoisePixelsCalculator
    {
        private int x_start;
        private int z_start;
        private int x_end;
        private int z_end;
        private int x_size_vertex;
        private int xSizeVertexLocal;

        private Thread myThread;

        private float[] vertexHeightMapLocal;

        private bool NOTIgnoreFixedPoints;

        private fixedWorldPoints[] fixedPoints;

        private bool currentPointIsFixedPoint = false;
        private float smoothedValue;

        public long LOCKED_Is_Thread_Done = 1;
        public bool justFinished = false;

        private ThreadStart threadDelegate;

        public void cleanUp()
        {
            vertexHeightMapLocal = null;
            fixedPoints = null;
            myThread.Abort();
            myThread = null;
        }

        public void setup(int x_start_new, int z_start_new, int x_end_new, int z_end_new, bool ignoreFixedPoint_new, fixedWorldPoints[] fixedPoint_new, float[] VertexHeightMapRef, int x_size_vertex_new)
        {
            x_start = x_start_new;
            z_start = z_start_new;

            x_end = x_end_new;
            z_end = z_end_new;

            x_size_vertex = x_size_vertex_new;

            NOTIgnoreFixedPoints = !ignoreFixedPoint_new;

            threadDelegate = new ThreadStart(calcInThread);

            if (ignoreFixedPoint_new)
            {
                fixedPoints = null;
            }
            else
            {
                fixedPoints = new fixedWorldPoints[fixedPoint_new.Length];
                for (int i = 0; i < fixedPoint_new.Length; i++)
                {
                    fixedPoints[i] = new fixedWorldPoints();
                    //fixedPoints[i].value = fixedPoint_new[i].value;
                    fixedPoints[i].x = fixedPoint_new[i].x;
                    fixedPoints[i].y = fixedPoint_new[i].y;
                }
            }

            xSizeVertexLocal = ((x_end + 1) - (x_start - 1));
            vertexHeightMapLocal = new float[((z_end + 1) - (z_start - 1)) * xSizeVertexLocal];

            for (int i = z_start - 1; i < z_end + 1; i++)
            {
                for (int j = x_start - 1; j < x_end + 1; j++)
                {
                    vertexHeightMapLocal[j - (x_start - 1) + (i - (z_start - 1)) * xSizeVertexLocal] = VertexHeightMapRef[(j) + (i) * x_size_vertex];
                }
            }
        }

        public void updateHeightMap(float[] VertexHeightMapRef)
        {
            for (int i = z_start - 1; i < z_end + 1; i++)
            {
                for (int j = x_start - 1; j < x_end + 1; j++)
                {
                    vertexHeightMapLocal[j - x_start + 1 + (i - z_start + 1) * xSizeVertexLocal] = VertexHeightMapRef[j + i * x_size_vertex];
                }
            }
        }

        private void calcInThread()
        {
            // smooth terrain

            int loopEndZ = z_end - z_start;
            int loopEndX = x_end - x_start;

            for (int i = 0; i < loopEndZ; i++)
            {
                for (int j = 0; j < loopEndX; j++)
                {

                    if (NOTIgnoreFixedPoints)
                    {
                        currentPointIsFixedPoint = false;
                        for (int m = 0; m < fixedPoints.Length; m++)
                        {
                            if (fixedPoints[m].x == i + z_start)
                            {
                                if (fixedPoints[m].y == j + x_start)
                                {
                                    currentPointIsFixedPoint = true;
                                    break;
                                }
                            }
                        }
                        if (currentPointIsFixedPoint)
                        {
                            continue;
                        }
                    }

                    smoothedValue = (vertexHeightMapLocal[j + xSizeVertexLocal * i]
                    + vertexHeightMapLocal[j + xSizeVertexLocal * (i + 1)]
                    + vertexHeightMapLocal[j + xSizeVertexLocal * (i + 2)]

                    + vertexHeightMapLocal[j + 1 + xSizeVertexLocal * i]
                    + vertexHeightMapLocal[j + 1 + xSizeVertexLocal * (i + 1)]
                    + vertexHeightMapLocal[j + 1 + xSizeVertexLocal * (i + 2)]

                    + vertexHeightMapLocal[j + 2 + xSizeVertexLocal * i]
                    + vertexHeightMapLocal[j + 2 + xSizeVertexLocal * (i + 1)]
                    + vertexHeightMapLocal[j + 2 + xSizeVertexLocal * (i + 2)])
                    / 9;

                    vertexHeightMapLocal[j + xSizeVertexLocal * i] = smoothedValue;
                }

            }
            justFinished = true;
            Interlocked.Increment(ref LOCKED_Is_Thread_Done);
        }

        public void startCalulation()
        {
            if (Interlocked.Read(ref LOCKED_Is_Thread_Done) == 0)
            {
                Debug.LogError("Trying to start a thread, while an old thread is still running");
            }
            else // == 1
            {
                Interlocked.Decrement(ref LOCKED_Is_Thread_Done);

                if (myThread != null)
                {
                    myThread.Abort();
                }

                myThread = new Thread(threadDelegate);
                myThread.Start();
            }
        }

        public void addResults(float[] addToThisRef)
        {
            for (int i = z_start; i < z_end; i++)
            {
                for (int j = x_start; j < x_end; j++)
                {
                    addToThisRef[i * x_size_vertex + j] = vertexHeightMapLocal[(i - z_start) * x_size_vertex + j - x_start];
                }
            }
        }
    }

    public event EventHandler<EventArgs> worldMeshesUpdatedEvent;

    virtual protected void onWorldMeshUpdated()
    {
        EventHandler<EventArgs> handler = worldMeshesUpdatedEvent;

        if (handler != null)
        {
            EventArgs args = new EventArgs();

            worldMeshesUpdatedEvent(this, args);
        }
    }

    public void terrainMeshesForceRenderOnce()
    {
        m_TerrainMeshes_ForceRenderOnce = true;
    }

    private void smoothNoisePixels(int count, bool IgnoreFixedPoints, int numberOfThreads, float[] VertexHeightMapQuadInput)
    {
        // smooth texture
        int currentSmoothStage = 0;

        List<smoothNoisePixelsCalculator> calcThreadObjList = new List<smoothNoisePixelsCalculator>();

        int edgeLength = (int)Mathf.Sqrt(VertexHeightMapQuadInput.Length);
        int z_size_needed = edgeLength - 2; // cant smooth border points
        int threadIntervall_z = z_size_needed / numberOfThreads;

        bool allThreadsFinished = false;

        for (int i = 0; i < numberOfThreads; i++)
        {
            smoothNoisePixelsCalculator myCalc = new smoothNoisePixelsCalculator();
            calcThreadObjList.Add(myCalc);

            if (i == numberOfThreads - 1) // last thread might need to calc more because of rounding error
            {
                myCalc.setup(1, (i * threadIntervall_z) + 1, edgeLength - 1, edgeLength - 1, IgnoreFixedPoints, fixedPoints, VertexHeightMapQuadInput, edgeLength);
            }
            else
            {
                myCalc.setup(1, (i * threadIntervall_z) + 1, edgeLength - 1, threadIntervall_z * (i + 1) + 1, IgnoreFixedPoints, fixedPoints, VertexHeightMapQuadInput, edgeLength);
            }
            myCalc.startCalulation();
        }

        while (currentSmoothStage < count)
        {
            foreach (smoothNoisePixelsCalculator obj in calcThreadObjList)
            {
                if (currentSmoothStage > 0)
                {
                    obj.updateHeightMap(VertexHeightMapQuadInput); // dont need to update first run because setup already received the right values
                    obj.startCalulation();
                }
            }

            allThreadsFinished = false;
            while (!allThreadsFinished)
            {
                allThreadsFinished = true;
                foreach (smoothNoisePixelsCalculator obj in calcThreadObjList)
                {
                    if (Interlocked.Read(ref obj.LOCKED_Is_Thread_Done) == 0)
                    {
                        allThreadsFinished = false;
                    }
                    else
                    {
                        if (obj.justFinished)
                        {
                            obj.justFinished = false;
                            obj.addResults(VertexHeightMapQuadInput);
                        }
                    }
                }
            }

            currentSmoothStage++;
        }

        // clean up thread-objects
        for (int i = 0; i < calcThreadObjList.Count; i++)
        {
            calcThreadObjList[i].cleanUp();
            calcThreadObjList[i] = null;
        }
        calcThreadObjList.Clear();
        calcThreadObjList = null;
    }

}
