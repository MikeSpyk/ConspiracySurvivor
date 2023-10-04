using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCBattleField : MonoBehaviour
{
    [SerializeField] private GameObject m_navMeshSurfacePrefab;
    [SerializeField] private float m_battleFieldSegmentSize = 10;
    [SerializeField] private int m_createNavMeshsPerUpdate = 2;

    private Vector3 m_batteFieldOrigin = Vector3.zero;
    private bool m_createBattlefieldActive = false;
    private NPCBattleManager m_NPCBattleManager = null;
    private List<GameObject> m_navMeshSurfaceObjs = new List<GameObject>();
    private int m_battleFieldSize = 100;

    public void setBattleFieldSize(int size)
    {
        m_battleFieldSize = size;
    }

    public void setNPCBattleManager(NPCBattleManager manager)
    {
        m_NPCBattleManager = manager;
    }

    public void restart(Vector3 position)
    {
        m_batteFieldOrigin = position;

        StartCoroutine(createBattlefieldCoroutine());
    }

    private IEnumerator createBattlefieldCoroutine()
    {
        if (m_createBattlefieldActive)
        {
            throw new System.NotSupportedException("NPCBattleField: createBattlefieldCoroutine: started twice");
        }

        m_createBattlefieldActive = true;

        int battleFieldSegmentsPerEdge = (int)(m_battleFieldSize / m_battleFieldSegmentSize);

        for (int i = 0; i < m_navMeshSurfaceObjs.Count; i++)
        {
            Destroy(m_navMeshSurfaceObjs[i]);
        }
        m_navMeshSurfaceObjs.Clear();

        Debug.Log("TODO Mike: recycle nav mesh investigation");

        yield return null;

        int iterationCounter = 0;

        Vector3 lowerLeftStart = m_batteFieldOrigin - new Vector3(m_battleFieldSize / 2, 0, m_battleFieldSize / 2);

        for (int i = 0; i < battleFieldSegmentsPerEdge; i++)
        {
            for (int j = 0; j < battleFieldSegmentsPerEdge; j++)
            {
                if (iterationCounter > m_createNavMeshsPerUpdate)
                {
                    iterationCounter = 0;
                    yield return null;
                }

                iterationCounter++;

                GameObject spawnedObj = Instantiate(m_navMeshSurfacePrefab, lowerLeftStart + new Vector3(i * m_battleFieldSegmentSize, 0, j * m_battleFieldSegmentSize), Quaternion.identity);
                spawnedObj.transform.parent = gameObject.transform;

                NavMeshSurface navMeshSurface = spawnedObj.GetComponent<NavMeshSurface>();

                navMeshSurface.collectObjects = CollectObjects.Volume;
                navMeshSurface.size = new Vector3(m_battleFieldSegmentSize, navMeshSurface.size.y, m_battleFieldSegmentSize);
                navMeshSurface.BuildNavMesh();

                m_navMeshSurfaceObjs.Add(spawnedObj);
            }
        }

        yield return null;

        m_createBattlefieldActive = false;

        m_NPCBattleManager.onCreateBattlefieldDone();
    }

}
