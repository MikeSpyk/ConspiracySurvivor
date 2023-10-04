using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionDistance
{
    public PositionDistance(Vector3 position, float distance)
    {
        m_position = position;
        m_distance = distance;
    }

    public Vector3 m_position;
    public float m_distance;
}
