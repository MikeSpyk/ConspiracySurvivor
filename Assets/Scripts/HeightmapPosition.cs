using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightmapPosition
{
    public HeightmapPosition(float height, Vector2Int position)
    {
        m_heightValue = height;
        m_position = position;
    }

    public float m_heightValue;
    public Vector2Int m_position;
}
