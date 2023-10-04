using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleCachedManager : MonoBehaviour
{
    public static ParticleCachedManager singleton;

    [SerializeField] private GameObject[] m_prefabs;
    [SerializeField] private bool DEBUG_hideInHierarchy;
    [SerializeField] private bool DEBUG_noCache;
    [SerializeField] private float m_client_maxViewDistance = 200f;
    private HideFlags m_currentHideFlagsSettings;
    private bool m_debug_hideInHierarchy_last;
    private List<GameobjectParticleSystem>[] m_cachedObjects;
    private List<GameobjectParticleSystem>[] m_activeObjects;

    [Header("Outputs")]
    [SerializeField] private int m_activeEffectsCount = 0;
    [SerializeField] private int m_cachedEffectsCount = 0;

    public float client_maxViewDistance { get { return m_client_maxViewDistance; } }

    private void Awake()
    {
        singleton = this;
    }

    private void Start()
    {
        m_cachedObjects = new List<GameobjectParticleSystem>[m_prefabs.Length];
        m_activeObjects = new List<GameobjectParticleSystem>[m_prefabs.Length];

        for (int i = 0; i < m_prefabs.Length; i++)
        {
            m_cachedObjects[i] = new List<GameobjectParticleSystem>();
            m_activeObjects[i] = new List<GameobjectParticleSystem>();
        }

        if (DEBUG_hideInHierarchy)
        {
            m_currentHideFlagsSettings = HideFlags.HideInHierarchy;
        }
        else
        {
            m_currentHideFlagsSettings = HideFlags.None;
        }
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < m_activeObjects.Length; i++)
        {
            for (int j = 0; j < m_activeObjects[i].Count; j++)
            {
                if (!m_activeObjects[i][j].m_particleSystem.isPlaying) // is no longer playing = is done --> recyle
                {
                    recyleObject(m_activeObjects[i][j]);
                    j--;
                }
            }
        }
    }

    private void OnValidate()
    {
        // hide in hierarchy or show in hierarchy
        if (m_cachedObjects == null)
        {
            return;
        }

        if (DEBUG_hideInHierarchy != m_debug_hideInHierarchy_last)
        {
            m_debug_hideInHierarchy_last = DEBUG_hideInHierarchy;

            HideFlags newHideFlags;
            if (DEBUG_hideInHierarchy)
            {
                newHideFlags = HideFlags.HideInHierarchy;
            }
            else
            {
                newHideFlags = HideFlags.None;
            }

            for (int i = 0; i < m_activeObjects.Length; i++)
            {
                for (int j = 0; j < m_activeObjects[i].Count; j++)
                {
                    m_activeObjects[i][j].m_gameObject.hideFlags = newHideFlags;
                }
            }

            for (int i = 0; i < m_cachedObjects.Length; i++)
            {
                for (int j = 0; j < m_cachedObjects[i].Count; j++)
                {
                    m_cachedObjects[i][j].m_gameObject.hideFlags = newHideFlags;
                }
            }
        }
    }

    private GameobjectParticleSystem getNewObject(int prefabIndex)
    {
        GameobjectParticleSystem returnValue;

        if (m_cachedObjects[prefabIndex].Count > 0 && !DEBUG_noCache)
        {
            returnValue = m_cachedObjects[prefabIndex][0];
            m_cachedObjects[prefabIndex].RemoveAt(0);
            returnValue.m_gameObject.SetActive(true);
            m_cachedEffectsCount--;
        }
        else
        {
            returnValue = new GameobjectParticleSystem();
            returnValue.m_gameObject = Instantiate(m_prefabs[prefabIndex]);
            returnValue.m_gameObject.hideFlags = m_currentHideFlagsSettings;
            returnValue.m_particleSystem = returnValue.m_gameObject.GetComponent<ParticleSystem>();
            returnValue.m_prefabIndex = prefabIndex;
        }

        m_activeObjects[prefabIndex].Add(returnValue);
        m_activeEffectsCount++;

        return returnValue;
    }

    private void recyleObject(GameobjectParticleSystem inObject)
    {
        inObject.m_gameObject.SetActive(false);
        m_cachedObjects[inObject.m_prefabIndex].Add(inObject);
        m_activeObjects[inObject.m_prefabIndex].Remove(inObject);
        m_activeEffectsCount--;
        m_cachedEffectsCount++;
    }

    public void playParticleEffect(int prefabIndex, Vector3 position, Quaternion rotation)
    {
        if (prefabIndex >= m_prefabs.Length || prefabIndex < 0)
        {
            Debug.LogWarning("ParticleCachedManager: prefabIndex out of range: " + prefabIndex);
            return;
        }

        GameobjectParticleSystem currentObj = getNewObject(prefabIndex);
        currentObj.m_gameObject.transform.position = position;
        currentObj.m_gameObject.transform.rotation = rotation;
        currentObj.m_particleSystem.Play();
    }
}
