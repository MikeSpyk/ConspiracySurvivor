using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanWater : MonoBehaviour
{
    public static OceanWater singleton;

    [SerializeField] private Material[] m_materials;

    private Renderer m_renderer;

    private void Awake()
    {
        singleton = this;

        m_renderer = GetComponent<Renderer>();
    }

    public void setWaterMaterial(int index)
    {
        m_renderer.material = m_materials[index];
    }
}
