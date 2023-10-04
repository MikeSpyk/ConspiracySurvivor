using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LODSpritesEntity : MonoBehaviour
{
    [SerializeField] public Material[] m_spritesMaterials;
    [SerializeField] public UnityEngine.Rendering.ShadowCastingMode m_shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    [SerializeField] public bool m_receiveShadows = false;
    [SerializeField] public Mesh m_mesh = null;
    [SerializeField] public Vector3 m_size = Vector3.one;
    [SerializeField] public float m_offsetY = 0f;
}
