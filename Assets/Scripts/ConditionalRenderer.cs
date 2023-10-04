using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionalRenderer : MonoBehaviour {

    [SerializeField] private bool m_renderRunning;
    [SerializeField] private bool m_destroy;
    
    private Renderer m_renderer = null;
    private Collider m_collider = null;
    private MeshFilter m_meshFilter = null;

    // Use this for initialization
    void Start()
    {
        if (m_renderRunning)
        {

        }
        else
        {
            m_renderer = GetComponent<Renderer>();
            m_collider = GetComponent<Collider>();
            m_meshFilter = GetComponent<MeshFilter>();

            if (m_renderer != null)
            {
                m_renderer.enabled = false;
                if (m_destroy)
                {
                    Destroy(m_renderer);
                }
            }
            if (m_collider != null)
            {
                m_collider.enabled = false;
                if (m_destroy)
                {
                    Destroy(m_collider);
                }
            }
            if (m_meshFilter != null && m_destroy)
            {
                Destroy(m_meshFilter);
            }

            if (m_destroy)
            {
                Destroy(GetComponent<ConditionalRenderer>());
            }
        }
    }
	
}
