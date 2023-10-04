using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LODInfos : MonoBehaviour
{
    public enum LODObjectTyp { GameObject, MeshOnly, Billboard }

    [SerializeField] private int m_LODLevel = 0;
    [SerializeField] private LODObjectTyp m_LODType;
    [SerializeField] private float m_maxDistance = 100f;

    public int LODLevel
    {
        get
        {
            return m_LODLevel;
        }
    }

    public LODObjectTyp LODType
    {
        get
        {
            return m_LODType;
        }
    }

    public float maxDistance
    {
        get
        {
            return m_maxDistance;
        }
    }
}
