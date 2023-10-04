using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUIRaycastIdentifier : MonoBehaviour
{
    public enum Type { Default = 0, PlayerInventory = 1, PlayerHotbar = 2, PlayerLootContainer = 3}
    [SerializeField] private Type m_type;
    [SerializeField] private int m_index;

    public Type getType()
    {
        return m_type;
    }

    public int getIndex()
    {
        return m_index;
    }
}
