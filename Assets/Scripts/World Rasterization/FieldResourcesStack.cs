using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct FieldResourcesStack
{
    public FieldResourcesStack(int need)
    {
        m_need = need;
        m_lastTimeAdded = 0;
    }

    public int m_need;
    public float m_lastTimeAdded;
}
