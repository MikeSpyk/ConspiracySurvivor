using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointOfInterest : MonoBehaviour
{
    [SerializeField] private int m_radius = 1;
    [SerializeField] private int m_blurDistance = 1; // length on top of radius to blur with the terrain
    [SerializeField] private float m_minDistanceToNextPOI = 1; // length on top of radius and blur distance to next point of interrest
    [SerializeField] private float m_maxHeightDiff = 1; // how flat should the terrain be 
    [SerializeField] private int m_searchResolution = 7; // stepwidth in heightmap (2^x). this is the space that gets checked for height differences (1 = 2, 2 = 4, 3 = 8, ... , 7 = 128, ...)
    [SerializeField] private int m_maxOccurrence = 1;
    [SerializeField] private int m_SpawnPointGroupID = -1;
    [Header("Debug Inputs")]
    [SerializeField] private bool m_setAsGlobalSpawnPoints;
    [Header("Outputs")]
    [SerializeField] private int m_spawnpointsCount = 0;

    private GameManager_Custom m_gameManager = null;
    private List<GameObject> m_spawnPoints = new List<GameObject>();

    private void Start()
    {
        m_gameManager = GameManager_Custom.singleton;
        initialize();
    }

    private void Update()
    {
        if(m_setAsGlobalSpawnPoints)
        {
            m_setAsGlobalSpawnPoints = false;

            if (m_gameManager.isServer || m_gameManager.isServerAndClient)
            {
                List<Vector3> spawnPointsPos = new List<Vector3>();

                for(int i = 0; i < m_spawnPoints.Count; i++)
                {
                    spawnPointsPos.Add(m_spawnPoints[i].transform.position);
                }

                m_gameManager.setGlobalPlayerSpawnPoints(spawnPointsPos);
            }
        }
    }

    public int radius
    {
        get
        {
            return m_radius;
        }
    }

    public int blurDistance
    {
        get
        {
            return m_blurDistance;
        }
    }

    public float minDistanceToNextPOI
    {
        get
        {
            return m_minDistanceToNextPOI;
        }
    }

    public float maxHeightDifference
    {
        get
        {
            return m_maxHeightDiff;
        }
    }

    public int searchResolution
    {
        get
        {
            return m_searchResolution;
        }
    }

    public int maxOccurrence
    {
        get
        {
            return m_maxOccurrence;
        }
    }

    public void initialize()
    {
        if (m_gameManager.isServer || m_gameManager.isServerAndClient)
        {
            m_SpawnPointGroupID = GameManager_Custom.singleton.getUniqueSpawnPointID();
            setChildrenSpawnPointsGroupID(gameObject);
        }
    }

    private void setChildrenSpawnPointsGroupID(GameObject parent)
    {
        SpawnPoint spawnPoint;
        Transform child;

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            child = parent.transform.GetChild(i);

            spawnPoint = child.GetComponent<SpawnPoint>();

            if (spawnPoint != null)
            {
                m_spawnpointsCount++;
                spawnPoint.setSpawnGroupID(m_SpawnPointGroupID);
                m_spawnPoints.Add(child.gameObject);
            }

            if(child.childCount > 0)
            {
                setChildrenSpawnPointsGroupID(child.gameObject);
            }
        }

    }

}
