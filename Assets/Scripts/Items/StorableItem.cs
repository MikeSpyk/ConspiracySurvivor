using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StorableItem
{
    /// <summary>
    /// don't use this ! Use ItemManager.singleton.createNewStorableItem istead.
    /// </summary>
    /// <param name="itemTemplateIndex"></param>
    /// <param name="stackSize"></param>
    public StorableItem(int itemTemplateIndex, int stackSize)
    {
        m_itemTemplateIndex = itemTemplateIndex;
        m_stackSize = stackSize;
    }
    /// <summary>
    /// don't use this ! Use ItemManager.singleton.createNewStorableItem istead.
    /// </summary>
    /// <param name="itemTemplateIndex"></param>
    public StorableItem(int itemTemplateIndex)
    {
        m_itemTemplateIndex = itemTemplateIndex;
        m_stackSize = 1;
    }

    private int m_itemTemplateIndex;
    public int m_stackSize;

    /// <summary>
    /// container this item is stored within
    /// </summary>
    public LootContainer m_owningContainer = null;

    public int itemTemplateIndex
    {
        get
        {
            return m_itemTemplateIndex;
        }
    }

    public int GUIIconIndex
    {
        get
        {
            return ItemManager.singleton.getGUIIconIndex(m_itemTemplateIndex);
        }
    }

    public int WorldPrefabIndex
    {
        get
        {
            return ItemManager.singleton.getWorldPrefabIndex(m_itemTemplateIndex);
        }
    }

    public bool isStackable
    {
        get
        {
            return ItemManager.singleton.getItemStackable(m_itemTemplateIndex);
        }
    }

    public int maxStackSize
    {
        get
        {
            return ItemManager.singleton.getMaxStackSize(m_itemTemplateIndex);
        }
    }

    public string displayName
    {
        get
        {
            return ItemManager.singleton.getDisplayName(m_itemTemplateIndex);
        }
    }

    public StorableItemTemplate.ItemType itemType
    {
        get
        {
            return ItemManager.singleton.getItemType(m_itemTemplateIndex);
        }
    }

    public string description
    {
        get
        {
            return ItemManager.singleton.getDescription(m_itemTemplateIndex);
        }
    }
}
