using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainDetailManagerV2 : MonoBehaviour
{
    [Header("Details Prefabs")]
    [SerializeField] private GameObject[] m_detailPrefabs;
    [SerializeField] private WorldManager.vertexMapTextureNames[] m_detailGround;
    [SerializeField] private float[] m_detailProbability;
    [Header("Settings")]
    [SerializeField] private float m_detailDistance = 20f;
    [SerializeField] private int m_actionsPerFrame = 10;
    [SerializeField] private AnimationCurve m_probabilityCurve;
    [Header("Debug")]
    [SerializeField] private bool m_reloadAll = false;
    [SerializeField] private bool m_hideInHierarchy = true;

    private Vector3Int m_lastPositionOnFinished = Vector3Int.zero;
    private Dictionary<int, int> m_prefabRemap = new Dictionary<int, int>(); // if the same prefab is present multiple times in m_detailPrefabs, then remap to get a more efficient cache

    private List<GameObject>[] m_activeObjects = null;
    private Queue<GameObject>[] m_objectsCache = null;

    protected void Awake()
    {
        m_activeObjects = new List<GameObject>[m_detailPrefabs.Length];
        m_objectsCache = new Queue<GameObject>[m_detailPrefabs.Length];

        for (int i = 0; i < m_activeObjects.Length; i++)
        {
            m_activeObjects[i] = new List<GameObject>();
            m_objectsCache[i] = new Queue<GameObject>();
        }

        for(int i = 0; i < m_detailPrefabs.Length; i++)
        {
            for (int j = 0; j < m_detailPrefabs.Length; j++)
            {
                if(m_detailPrefabs[i] == m_detailPrefabs[j])
                {
                    m_prefabRemap.Add(i, j);
                    //Debug.Log("TerrainDetailManagerV2: mapping prefab " + i + " to " + j);
                    break;
                }
            }
        }
    }

    protected void Start()
    {
        StartCoroutine("mainWork");
    }

    private IEnumerator mainWork()
    {
        int actionCounter;

        int iPlusOne;

        while (true)
        {
            if ((GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient) && GameManager_Custom.singleton.isGameInitialized)
            {
                if (m_reloadAll)
                {
                    m_reloadAll = false;
                    m_lastPositionOnFinished = Vector3Int.zero;

                    for (int i = 0; i < m_activeObjects.Length; i++)
                    {
                        while (m_activeObjects[i].Count > 0)
                        {
                            recyleTerrainObject(m_activeObjects[i][0], i);
                            m_activeObjects[i].RemoveAt(0);
                        }
                    }
                }

                Vector3Int playerPosition = new Vector3Int();
                playerPosition.x = (int)PlayerManager.singleton.getWorldViewPoint(-1).transform.position.x;
                playerPosition.y = (int)PlayerManager.singleton.getWorldViewPoint(-1).transform.position.y;
                playerPosition.z = (int)PlayerManager.singleton.getWorldViewPoint(-1).transform.position.z;

                if (playerPosition == m_lastPositionOnFinished)
                {
                    yield return null;
                    continue; // if player hasn't moved -> restart
                }

                int detailDistanceInt = (int)m_detailDistance;
                actionCounter = 0;

                // remove old object

                for (int i = 0; i < m_activeObjects.Length; i++)
                {
                    for (int j = 0; j < m_activeObjects[i].Count; j++)
                    {
                        if (Vector3.Distance(m_activeObjects[i][j].transform.position, playerPosition) > (detailDistanceInt + 1))
                        {
                            recyleTerrainObject(m_activeObjects[i][j], i);
                            m_activeObjects[i].RemoveAt(j);

                            j--;
                        }

                        actionCounter++;

                        if (actionCounter > m_actionsPerFrame)
                        {
                            actionCounter = 0;
                            yield return null;
                        }
                    }
                }

                yield return null;

                // create new objects

                float tempPosY;
                float tempFertility;
                Vector3 tempNormal;
                byte tempTextureIndex;
                float tempJ;
                float tempK;

                for (int i = 0; i < m_detailPrefabs.Length; i++)
                {
                    iPlusOne = i + 1;

                    for (int j = playerPosition.x - detailDistanceInt; j < playerPosition.x + detailDistanceInt; j++)
                    {
                        for (int k = playerPosition.z - detailDistanceInt; k < playerPosition.z + detailDistanceInt; k++)
                        {
                            Vector3Int iteratorPosition = new Vector3Int(j, playerPosition.y, k);

                            tempJ = j;
                            tempK = k;

                            WorldManager.singleton.getHeightmapPointInfo(ref tempJ, ref tempK, out tempPosY, out tempFertility, out tempNormal, out tempTextureIndex);

                            if (tempTextureIndex == (byte)m_detailGround[i] && Vector3Int.Distance(iteratorPosition, playerPosition) < detailDistanceInt && Vector3Int.Distance(iteratorPosition, m_lastPositionOnFinished) >= detailDistanceInt)
                            {
                                if (RandomValuesSeed.getRandomBoolProbability(j * iPlusOne * 0.3141f, k * iPlusOne * 0.3141f, m_probabilityCurve.Evaluate(m_detailProbability[i] / 100f) * 100f))
                                {
                                    GameObject terrainObject = getTerrainObject(i);

                                    Vector2 randomOffset = new Vector2();
                                    randomOffset.x = RandomValuesSeed.getRandomValueSeed(j, k * iPlusOne, -0.5f, 0.5f) + Mathf.Sin(j * 0.0005f);
                                    randomOffset.y = RandomValuesSeed.getRandomValueSeed(k, j * iPlusOne, -0.5f, 0.5f) + Mathf.Sin(k * 0.0005f);

                                    terrainObject.transform.position = new Vector3(j + randomOffset.x, WorldManager.singleton.getHeightmapY(new Vector2(j, k) + randomOffset), k + randomOffset.y);
                                    terrainObject.transform.rotation = RandomValuesSeed.getRandomRotationYAxis(j, k * iPlusOne);

                                    m_activeObjects[i].Add(terrainObject);
                                }
                            }

                            actionCounter++;

                            if (actionCounter > m_actionsPerFrame)
                            {
                                actionCounter = 0;
                                yield return null;
                            }
                        }
                    }
                }

                m_lastPositionOnFinished = playerPosition;

                yield return null;
            }
            else
            {
                yield return null;
            }
        }
    }

    private void recyleTerrainObject(GameObject terrainObject, int prefabIndex)
    {
        terrainObject.SetActive(false);
        m_objectsCache[m_prefabRemap[prefabIndex]].Enqueue(terrainObject);
    }

    private GameObject getTerrainObject(int prefabIndex)
    {
        GameObject returnValue;

        prefabIndex = m_prefabRemap[prefabIndex];

        if (m_objectsCache[prefabIndex].Count > 0)
        {
            returnValue = m_objectsCache[prefabIndex].Dequeue();
            returnValue.SetActive(true);
        }
        else
        {
            returnValue = Instantiate(m_detailPrefabs[prefabIndex]);
            if (m_hideInHierarchy)
            {
                returnValue.hideFlags = HideFlags.HideInHierarchy;
            }
        }

        return returnValue;
    }
}
