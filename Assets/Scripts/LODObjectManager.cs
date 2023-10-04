using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class LODObjectManager : MonoBehaviour
{
    [SerializeField] private GameObject[] m_LODPrefabs;

    private LODInfos[][] m_PrefabsLODData;
    private GameObject[][] m_PrefabsGameobjects;
    private BillboardData[][] m_PrefabsBillboardData;

    private List<LODObject> m_LODObjects = new List<LODObject>();

    private long LOCKED_UpdateThreadIsDone = 1;
    private Thread m_updateThread = null;

    // Use this for initialization
    void Start ()
    {
        m_PrefabsLODData = new LODInfos[m_LODPrefabs.Length][];
        m_PrefabsGameobjects = new GameObject[m_LODPrefabs.Length][];
        m_PrefabsBillboardData = new BillboardData[m_LODPrefabs.Length][];

        for (int i = 0; i< m_LODPrefabs.Length; i++)
        {
            m_PrefabsLODData[i] = new LODInfos[m_LODPrefabs[i].transform.childCount];
            m_PrefabsGameobjects[i] = new GameObject[m_LODPrefabs[i].transform.childCount];
            m_PrefabsBillboardData[i] = new BillboardData[m_LODPrefabs[i].transform.childCount];

            for (int j = 0; j < m_LODPrefabs[i].transform.childCount; j++)
            {
                m_PrefabsLODData[i][j] = m_LODPrefabs[i].transform.GetChild(j).GetComponent<LODInfos>();
                if(m_PrefabsLODData[i][j] == null)
                {
                    Debug.LogError("LODObjectManager: Initializing: Missing LODInfos component on LODPrefab child !");
                }
                else
                {
                    if(m_PrefabsLODData[i][j].LODType == LODInfos.LODObjectTyp.GameObject)
                    {
                        m_PrefabsGameobjects[i][j] = m_LODPrefabs[i].transform.GetChild(j).gameObject;
                    }
                    else if (m_PrefabsLODData[i][j].LODType == LODInfos.LODObjectTyp.Billboard)
                    {
                        m_PrefabsBillboardData[i][j] = m_LODPrefabs[i].transform.GetChild(j).GetComponent<BillboardData>();
                    }
                    else
                    {
                        Debug.LogError("LODObjectManager: Initializing: Unknown LODType !");
                    }
                }
            }
        }
	}

    // Update is called once per frame
    void Update()
    {

        if(Interlocked.Read(ref LOCKED_UpdateThreadIsDone) == 1)
        {
            LOCKED_UpdateThreadIsDone = 0;

            if(m_updateThread != null)
            {
                m_updateThread.Abort();
            }

            m_updateThread = new Thread(new ThreadStart(updateActiveLODObjects));
            m_updateThread.Start();
        }
    }

    public void addLODObject(LODObject newObject)
    {
        m_LODObjects.Add(newObject);
    }
    public void addLODObjects(List<LODObject> newObjects)
    {
        m_LODObjects.AddRange(newObjects);
    }

    private void updateActiveLODObjects()
    {
        for(int i = 0; i < m_LODObjects.Count; i++)
        {

        }

        Interlocked.Exchange(ref LOCKED_UpdateThreadIsDone, 1);
    }
}
