using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavUtility : MonoBehaviour
{
    [SerializeField] private NavMeshSurface m_navMeshSurface;
    [SerializeField] private bool m_rebuildNavMesh = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(m_rebuildNavMesh)
        {
            m_rebuildNavMesh = false;
            m_navMeshSurface = GetComponent<NavMeshSurface>();

            if (m_navMeshSurface != null)
            {
                m_navMeshSurface.BuildNavMesh();
            }
        }
    }

}
