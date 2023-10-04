#define THREAD_TRY_CATCH
#undef  THREAD_TRY_CATCH
#define DEBUG
#undef DEBUG

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Threading;

public class TerrainDetailManager : MonoBehaviour
{
    private class DetailSquare
    {
        public DetailSquare(int detailPrefabsCount, int UID)
        {
            m_UID = UID;

            m_instancingObjects = new Matrix4x4[detailPrefabsCount][];

            for (int i = 0; i < m_instancingObjects.Length; i++)
            {
                m_instancingObjects[i] = new Matrix4x4[1023]; // Graphics.DrawMeshInstanced limitation
            }

            m_instancingObjectsCount = new int[detailPrefabsCount];

            m_cornerPositions = new Vector2[4];
        }

        public Vector2Int m_RasterPosition;
        public int m_detailResolution;
        public Matrix4x4[][] m_instancingObjects;
        public int[] m_instancingObjectsCount;
        public Vector2[] m_cornerPositions;
        public Vector2 m_origin;

        private int m_UID;

        public override int GetHashCode()
        {
            return m_UID;
        }
    }

    public static TerrainDetailManager singleton = null;

    [Header("Settings")]
    [SerializeField] private float m_angleCullingCorrectionFactor = 1;
    [SerializeField] private int m_detailFieldSize = 10; // how many vertices per field
    [SerializeField] private AnimationCurve m_rangeProbabilityReduction; //reduction of probability per range instance
    [SerializeField] private int[] m_spawnRanges; // 10 how many fields
    [SerializeField] private int m_spawnChecksPerSquareEdge = 4;
    [SerializeField] private float m_maxSizeFactor = 100f; // with growing distance the objects get less but bigger. what is the biggerst size it can get ?
    [SerializeField, Range(0.00001f, 1f)] private float m_qualityFactor = 1f;
    [Header("Detail Objects Prefabs")]
    [SerializeField] private Mesh[] m_detailPrefabMesh;
    [SerializeField] private Material[] m_detailPrefabMaterial;
    [SerializeField] private ShadowCastingMode[] m_detailPrefabShadowCasting;
    [SerializeField] private bool[] m_detailPrefabReceiveShadows;
    [SerializeField] private float[] m_detailPrefabLocalYOffset;
    [SerializeField] private Vector3[] m_detailPrefabLocalMinSize;
    [SerializeField] private Vector3[] m_detailPrefabLocalMaxSize;
    [SerializeField] private WorldManager.vertexMapTextureNames[] m_detailPrefabTexture; // on which texture will can this detail object occur
    [SerializeField] private float[] m_detailPrefabProbabilityFactor;
    [Header("Debug")]
    [SerializeField] private bool m_drawInstances = true;
    [SerializeField] private bool m_recomputeAll = false;
    [SerializeField, ReadOnly] private int m_activeDetailFieldCount = 0;

    private Thread m_createDetailThread = null;
    private ManualResetEvent m_detailComputeWaitMRE = new ManualResetEvent(true);
    private long LOCKED_stopDetailCompute = 0;
    private long LOCKED_detailComputeDone = 0;
    private Vector2 LOCKED_playerPosition = Vector2.zero;
    private List<DetailSquare> LOCKED_exchangeFields = new List<DetailSquare>();
    private List<DetailSquare> m_activeDetailSquares = new List<DetailSquare>();
    private List<DetailSquare> m_freeDetailSquares = new List<DetailSquare>();
    private int m_detailSquareUIDCOunter = 0;
    private bool LOCKED_transferDetailList = false;

    protected void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else
        {
            Debug.LogError("TerrainDetailManager: Awake: tried to set singleton multiple times !");
        }
    }

    protected void Start()
    {
        if (m_detailPrefabMaterial.Length != m_detailPrefabMaterial.Length ||
            m_detailPrefabMaterial.Length != m_detailPrefabMaterial.Length ||
            m_detailPrefabMaterial.Length != m_detailPrefabShadowCasting.Length ||
            m_detailPrefabMaterial.Length != m_detailPrefabReceiveShadows.Length ||
            m_detailPrefabMaterial.Length != m_detailPrefabProbabilityFactor.Length ||
            m_detailPrefabMaterial.Length != m_detailPrefabLocalYOffset.Length ||
            m_detailPrefabMaterial.Length != m_detailPrefabLocalMinSize.Length ||
            m_detailPrefabMaterial.Length != m_detailPrefabLocalMaxSize.Length)
        {
            Debug.LogError("Prefab-Arrays are different sizes !");
        }

        m_createDetailThread = new Thread(new ThreadStart(updateDetailCacheThread));
        m_createDetailThread.Start();
    }

    protected void OnDestroy()
    {
        //Debug.Log("terrainManagerDestroyed !");
        Interlocked.Exchange(ref LOCKED_stopDetailCompute, 1);
        if (m_createDetailThread != null)
        {
            m_createDetailThread.Abort();
            m_createDetailThread = null;
        }
    }

    protected void Update()
    {
        if ((GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient) && GameManager_Custom.singleton.isGameInitialized)
        {
            if (Interlocked.Read(ref LOCKED_detailComputeDone) == 1)
            {
                Player_local player = EntityManager.singleton.getLocalPlayer();

                if (player != null)
                {
                    LOCKED_playerPosition = new Vector2(player.transform.position.x, player.transform.position.z);
                }

                if (LOCKED_transferDetailList)
                {
                    LOCKED_transferDetailList = false;

                    // switch thread and active list
                    List<DetailSquare> tempList = m_activeDetailSquares;
                    m_activeDetailSquares = LOCKED_exchangeFields;
                    LOCKED_exchangeFields = tempList;

                    m_activeDetailFieldCount = m_activeDetailSquares.Count;
                }

                Interlocked.Exchange(ref LOCKED_detailComputeDone, 0);
                m_detailComputeWaitMRE.Set(); // continue thread
            }

            if (m_drawInstances)
            {
                Player_local player = EntityManager.singleton.getLocalPlayer();

                if (player != null)
                {
                    Vector2 playerPosition = new Vector2(player.transform.position.x, player.transform.position.z);
                    Vector2 playerViewDirection = new Vector2(player.transform.forward.x, player.transform.forward.z);
                    float fieldOfView = CameraStack.fieldOfView * m_angleCullingCorrectionFactor;
                    float minDistance = m_detailFieldSize * WorldManager.singleton.getDefaultSubmeshVertDistance();

                    for (int i = 0; i < m_activeDetailSquares.Count; i++)
                    {
                        if (Vector2.Distance(playerPosition, m_activeDetailSquares[i].m_origin) < minDistance || checkDetailSquareVisibly(m_activeDetailSquares[i], playerPosition, playerViewDirection, fieldOfView))
                        {
                            /*
                            for (int j = 0; j < m_activeDetailSquares[i].m_cornerPositions.Length; j++)
                            {
                                Debug.DrawRay(new Vector3(m_activeDetailSquares[i].m_cornerPositions[j].x, 0.0f, m_activeDetailSquares[i].m_cornerPositions[j].y), Vector3.up * 1000f, Color.red);
                            }
                            */

                            for (int j = 0; j < m_detailPrefabMesh.Length; j++)
                            {
                                if (m_activeDetailSquares[i].m_instancingObjectsCount[j] > 0)
                                {
                                    Graphics.DrawMeshInstanced(m_detailPrefabMesh[j], 0, m_detailPrefabMaterial[j], m_activeDetailSquares[i].m_instancingObjects[j], m_activeDetailSquares[i].m_instancingObjectsCount[j], null, m_detailPrefabShadowCasting[j], m_detailPrefabReceiveShadows[j]);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void updateDetailCacheThread()
    {
        Dictionary<Vector2Int, int> neededPositions_resolution = new Dictionary<Vector2Int, int>();

        float vertexDistance = WorldManager.singleton.getDefaultSubmeshVertDistance();

        Vector3[] sizeRanges = new Vector3[m_detailPrefabLocalMinSize.Length];

        for (int i = 0; i < sizeRanges.Length; i++)
        {
            sizeRanges[i] = m_detailPrefabLocalMaxSize[i] - m_detailPrefabLocalMinSize[i];
        }

        Vector2 lastUpdatePositionWorld = Vector2.zero;

        while (Interlocked.Read(ref LOCKED_stopDetailCompute) == 0)
        {
#if THREAD_TRY_CATCH
            try
            {
#endif
            m_detailComputeWaitMRE.WaitOne();

            LOCKED_transferDetailList = false;

            Vector2Int playerPositionRaster = new Vector2Int((Mathf.RoundToInt((LOCKED_playerPosition.x / vertexDistance) / m_detailFieldSize)) * m_detailFieldSize, (Mathf.RoundToInt((LOCKED_playerPosition.y / vertexDistance) / m_detailFieldSize)) * m_detailFieldSize);

            if (Vector2.Distance(lastUpdatePositionWorld, new Vector2(playerPositionRaster.x * vertexDistance, playerPositionRaster.y * vertexDistance)) > m_detailFieldSize * vertexDistance / 2 && GameManager_Custom.singleton.isGameInitialized)
            {
                lastUpdatePositionWorld = new Vector2(playerPositionRaster.x * vertexDistance, playerPositionRaster.y * vertexDistance);
                LOCKED_transferDetailList = true;

                // recyle

                for (int i = 0; i < LOCKED_exchangeFields.Count; i++)
                {
                    if (!m_activeDetailSquares.Contains(LOCKED_exchangeFields[i]))
                    {
                        recyleDetailSquare(LOCKED_exchangeFields[i]);
                    }
                }


                LOCKED_exchangeFields.Clear();

                // find raster positions

                neededPositions_resolution.Clear();

                for (int i = 0; i < m_spawnRanges.Length; i++)
                {
                    Vector2Int rangeHalf = new Vector2Int(m_spawnRanges[i] / 2, m_spawnRanges[i] / 2) * m_detailFieldSize;

                    for (int j = 0; j < m_spawnRanges[i]; j++)
                    {
                        for (int k = 0; k < m_spawnRanges[i]; k++)
                        {
                            Vector2Int fieldRasterPosition = playerPositionRaster + new Vector2Int(j, k) * m_detailFieldSize - rangeHalf;

                            if (Vector2Int.Distance(fieldRasterPosition, playerPositionRaster) <= (m_spawnRanges[i] * m_detailFieldSize / 2) && !neededPositions_resolution.ContainsKey(fieldRasterPosition))
                            {
                                neededPositions_resolution.Add(fieldRasterPosition, i);
                            }
                        }
                    }
                }

                // process raster positions

                // fields to release and create

                if (!m_recomputeAll)
                {
                    for (int i = 0; i < m_activeDetailSquares.Count; i++)
                    {
                        if (neededPositions_resolution.ContainsKey(m_activeDetailSquares[i].m_RasterPosition) && neededPositions_resolution[m_activeDetailSquares[i].m_RasterPosition] == m_activeDetailSquares[i].m_detailResolution)
                        {
                            // no need to recompute because already active
                            neededPositions_resolution.Remove(m_activeDetailSquares[i].m_RasterPosition);
                            LOCKED_exchangeFields.Add(m_activeDetailSquares[i]);
                        }
                    }
                }

                // compute detail objects

                foreach (KeyValuePair<Vector2Int, int> pair in neededPositions_resolution)
                {
                    DetailSquare currentSquare = getNewDetailSquare();
                    currentSquare.m_detailResolution = pair.Value;
                    currentSquare.m_RasterPosition = pair.Key;

                    currentSquare.m_origin = new Vector2(pair.Key.x * vertexDistance + m_detailFieldSize * vertexDistance / 2, pair.Key.y * vertexDistance + m_detailFieldSize * vertexDistance / 2);
                    currentSquare.m_cornerPositions[0] = currentSquare.m_origin + new Vector2(-m_detailFieldSize * vertexDistance / 2, -m_detailFieldSize * vertexDistance / 2);
                    currentSquare.m_cornerPositions[1] = currentSquare.m_origin + new Vector2(-m_detailFieldSize * vertexDistance / 2, m_detailFieldSize * vertexDistance / 2);
                    currentSquare.m_cornerPositions[2] = currentSquare.m_origin + new Vector2(m_detailFieldSize * vertexDistance / 2, -m_detailFieldSize * vertexDistance / 2);
                    currentSquare.m_cornerPositions[3] = currentSquare.m_origin + new Vector2(m_detailFieldSize * vertexDistance / 2, m_detailFieldSize * vertexDistance / 2);

                    for (int i = 0; i < m_detailFieldSize; i++)
                    {
                        for (int j = 0; j < m_detailFieldSize; j++)
                        {
                            for (int k = 0; k < m_spawnChecksPerSquareEdge; k++)
                            {
                                for (int l = 0; l < m_spawnChecksPerSquareEdge; l++)
                                {
                                    for (int m = 0; m < m_detailPrefabMesh.Length; m++)
                                    {

                                        float currentPosX = (pair.Key.x + i) * vertexDistance + ((Mathf.PerlinNoise((pair.Key.x + i) * 2.1f + k * 1.1f, (pair.Key.y + j) * 2.2f + l * 1.2f + m * 1.3f) - 0.5f) * 2) * vertexDistance;
                                        float currentPosY = (pair.Key.y + j) * vertexDistance + ((Mathf.PerlinNoise(((pair.Key.x + i) * 2.1f + k * 1.1f) * 0.5f, ((pair.Key.y + j) * 2.2f + l * 1.2f + m * 1.3f) * 0.6f) - 0.5f) * 2) * vertexDistance;

                                        float worldHeight;
                                        Vector3 worldNormal;
                                        byte worldTexture;
                                        float unused;

                                        WorldManager.singleton.getHeightmapPointInfo(ref currentPosX, ref currentPosY, out worldHeight, out unused, out worldNormal, out worldTexture);

                                        if (worldTexture == (byte)m_detailPrefabTexture[m])
                                        {
                                            float ProbabilityFalloff = m_rangeProbabilityReduction.Evaluate(1f - ((float)pair.Value / (m_spawnRanges.Length - 1))) * m_qualityFactor;

                                            if (RandomValuesSeed.getRandomBoolProbability(currentPosX, currentPosY, m_detailPrefabProbabilityFactor[m] * ProbabilityFalloff))
                                            {
                                                if (currentSquare.m_instancingObjectsCount[m] > 1022)
                                                {
                                                    Debug.LogWarning("TerrainDetailManager: updateDetailCacheThread: too many detail objects for Graphics.DrawMeshInstanced !");
                                                    break;
                                                }

                                                Vector3 size = m_detailPrefabLocalMinSize[m] + sizeRanges[m] * RandomValuesSeed.getRandomValueSeed(currentPosX, currentPosY);
                                                size.x = size.x * Mathf.Min(1 / Mathf.Max(ProbabilityFalloff, 0.0001f), m_maxSizeFactor);
                                                size.z = size.z * Mathf.Min(1 / Mathf.Max(ProbabilityFalloff, 0.0001f), m_maxSizeFactor);

                                                currentSquare.m_instancingObjects[m][currentSquare.m_instancingObjectsCount[m]] = Matrix4x4.TRS(new Vector3(currentPosX, worldHeight, currentPosY) + worldNormal * m_detailPrefabLocalYOffset[m], Quaternion.LookRotation(worldNormal) * Quaternion.Euler(90, 0, 0) * RandomValuesSeed.getRandomRotationYAxis(currentPosX + currentPosY), size);
                                                currentSquare.m_instancingObjectsCount[m]++;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //Debug.Log("adding square: count: " + currentSquare.m_instancingObjectsCount[0]);
                    LOCKED_exchangeFields.Add(currentSquare);
                }

                //Debug.Log("recomputing terrain detail objects done !");
            }

            m_detailComputeWaitMRE.Reset(); // block
            Interlocked.Exchange(ref LOCKED_detailComputeDone, 1);
#if THREAD_TRY_CATCH
            }
            catch (System.Exception ex)
            {
                Debug.LogError("TerrainDetailManager: updateDetailCacheThread: " + ex);
            }
#endif
        }
    }

    private DetailSquare getNewDetailSquare()
    {
        if (m_freeDetailSquares.Count > 0)
        {
            DetailSquare returnValue = m_freeDetailSquares[0];
            m_freeDetailSquares.RemoveAt(0);

            for (int i = 0; i < returnValue.m_instancingObjectsCount.Length; i++)
            {
                returnValue.m_instancingObjectsCount[i] = 0;
            }

            return returnValue;
        }
        else
        {
            m_detailSquareUIDCOunter++;
            return new DetailSquare(m_detailPrefabMesh.Length, m_detailSquareUIDCOunter);
        }
    }

    private void recyleDetailSquare(DetailSquare detailSquare)
    {
#if DEBUG
        if (m_freeDetailSquares.Contains(detailSquare))
        {
            Debug.LogWarning("TerrainDetailManager: recyleDetailSquare: object recyled multiple times !");
        }
        else
        {
#endif
        m_freeDetailSquares.Add(detailSquare);
#if DEBUG
        }
#endif
    }

    private static bool checkDetailSquareVisibly(DetailSquare detailSquare, Vector2 viewerPosition, Vector2 viewDirection, float viewAngle)
    {
        float minAngle = float.MaxValue;

        for (int i = 0; i < detailSquare.m_cornerPositions.Length; i++)
        {
            Vector2 connectionVec = detailSquare.m_cornerPositions[i] - viewerPosition;
            minAngle = Mathf.Min(Vector2.Angle(connectionVec, viewDirection));

            if (minAngle < viewAngle)
            {
                return true;
            }
        }

        if (minAngle > viewAngle)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void setActivity(bool newState)
    {
        m_drawInstances = newState;
    }

    /*
    private void createDetailSquare(int posX, int posY)
    {
#if TDM_DEBUG
		Debug.DrawRay(new Vector3( posX* m_squareEdgeSize,0.0f,posY * m_squareEdgeSize), Vector3.up * 100f, Color.green, 1f);
#endif

        float currentFertility;
        float currentPosX;
        float currentPosY;
        float currentPosHeight;
        float currentSizeRange;
        byte currentTextureIndex;
        Vector3 currentNormal;

        DetailSquare result = new DetailSquare(new Vector2Int(posX, posY), new List<SquareData>());

        for (int i = 0; i < thread_m_spawnChecksPerSquareEdge; i++)
        {
            for (int j = 0; j < thread_m_spawnChecksPerSquareEdge; j++)
            {
                currentPosX = posX * m_squareEdgeSize + m_squareDetailObjRandomRange + i * m_squareDetailObjDistance;
                currentPosY = posY * m_squareEdgeSize + m_squareDetailObjRandomRange + j * m_squareDetailObjDistance;

                currentPosX += ((Mathf.PerlinNoise(currentPosX * 4.1f, currentPosY * 4.1f) * 2) - 1f) * m_squareDetailObjRandomRange;
                currentPosY += ((Mathf.PerlinNoise(currentPosX * 2f, currentPosY * 2f) * 2) - 1f) * m_squareDetailObjRandomRange;

                WorldManager.singleton.getHeightmapPointInfo(ref currentPosX, ref currentPosY, out currentPosHeight, out currentFertility, out currentNormal, out currentTextureIndex);

                //Debug.DrawRay(new Vector3(currentPosX, currentPosHeight, currentPosY), currentNormal, Color.green, 100f);

                for (int k = 0; k < m_detailPrefabProbabilityFactor.Length; k++)
                {
                    if (currentTextureIndex == (byte)m_detailPrefabTexture[k])
                    {
                        currentSizeRange = m_detailPrefabLocalMinSize[k] - m_detailPrefabLocalMaxSize[k];

                        if (RandomValuesSeed.getRandomBoolProbability(currentPosX, currentPosY, m_detailPrefabProbabilityFactor[k]))
                        {
                            result.data.Add(new SquareData(k, Matrix4x4.TRS(new Vector3(currentPosX, currentPosHeight, currentPosY) + currentNormal * m_detailPrefabLocalYOffset[k], Quaternion.LookRotation(currentNormal) * Quaternion.Euler(90, 0, 0) * RandomValuesSeed.getRandomRotationYAxis(currentPosX + currentPosY), Vector3.one * (m_detailPrefabLocalMinSize[k] + currentSizeRange * RandomValuesSeed.getRandomValueSeed(currentPosX, currentPosY)))));
                            break;
                        }
                    }
                }

            }
        }

        m_ActiveDetailSquares.Add(result);
    }
    */
}
