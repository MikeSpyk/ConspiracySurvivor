using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootContainer
{
    /// <summary>
    /// creates a new loot container with the size of "size" elements with a maximum of "elementsPerRow" elements per row
    /// </summary>
    /// <param name="size"> how many items can be stored within this container</param>
    /// <param name="elementsPerRow">layout: how many items per row.</param>
    /// <param name="parentGameObject"></param>
    public LootContainer(int size, int elementsPerRow, GameObject parentGameObject = null)
    {
        m_storedItems = new StorableItem[size];
        if (parentGameObject == null)
        {
            //Debug.LogWarning("LootContainer: Constructer: parentGameObject is null");
        }
        m_parentGameObject = parentGameObject;
        m_elementsPerRow = elementsPerRow;
    }

    private int m_elementsPerRow;
    private StorableItem[] m_storedItems = null;
    private GameObject m_parentGameObject = null;

    public int elementsPerRow { get { return m_elementsPerRow; } }

    public event EventHandler<IntegerEventArgs> ItemChangedEvent;

    virtual protected void onItemChanged(int index)
    {
        EventHandler<IntegerEventArgs> handler = ItemChangedEvent;

        if (handler != null)
        {
            IntegerEventArgs args = new IntegerEventArgs();
            args.integer = index;

            ItemChangedEvent(this, args);
        }
    }

    public bool checkIfFreeSpace()
    {
        if (m_storedItems == null)
        {
            return false;
        }
        else
        {
            for (int i = 0; i < m_storedItems.Length; i++)
            {
                if (m_storedItems[i] == null)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// returns the underlying StorableItem array (might contain null values)
    /// </summary>
    /// <returns></returns>
    public StorableItem[] getAllItems()
    {
        return m_storedItems;
    }

    public StorableItem getItem(int index)
    {
        if (index < 0 || index > m_storedItems.Length - 1)
        {
            Debug.LogWarning("LootContainer: getItem: index is out of Range: " + index);
            return null;
        }

        return m_storedItems[index];
    }

    public void setAllItems(StorableItem[] items)
    {
        if (items == null || items.Length == 0)
        {
            Debug.LogError("LootContainer: setAllItems: new items-array null or empty");
        }
        else
        {
            m_storedItems = items;
        }
    }

    public void removeAllItems()
    {
        if (m_storedItems != null)
        {
            for (int i = 0; i < m_storedItems.Length; i++)
            {
                m_storedItems[i] = null;
            }
        }
    }

    public void destroyAllItems()
    {
        removeAllItems();

        m_storedItems = null;
    }

    public void getContainerLayout(out int size, out int elementsPerRow)
    {
        if (m_storedItems == null)
        {
            size = 0;
            elementsPerRow = 0;
        }
        else
        {
            size = m_storedItems.Length;
            elementsPerRow = m_elementsPerRow;
        }
    }

    /// <summary>
    /// returns the count of all stored items
    /// </summary>
    /// <returns></returns>
    public int getSize()
    {
        if (m_storedItems == null)
        {
            return 0;
        }

        return m_storedItems.Length;
    }

    /// <summary>
    /// attempts to add a new item to the loot container und returns true if successful
    /// </summary>
    /// <param name="index"></param>
    /// <param name="itemToAdd"></param>
    /// <returns></returns>
    public bool tryAddItem(int index, StorableItem itemToAdd)
    {
        if (index < 0 || index > m_storedItems.Length - 1)
        {
            Debug.LogWarning("LootContainer: tryAddItem: index out of Range: " + index);
            return false;
        }

        if (m_storedItems[index] != null)
        {
            Debug.LogWarning("LootContainer: tryAddItem: space is already occupied by another item");
            return false;
        }

        if (itemToAdd == null)
        {
            return true;
        }

        if (itemToAdd.m_owningContainer != null)
        {
            Debug.LogWarning("LootContainer: tryAddItem: item to add is still part of another loot container");
            return false;
        }

        if (TryStackItem(index, itemToAdd))
        {
            return true;
        }

        m_storedItems[index] = itemToAdd;
        itemToAdd.m_owningContainer = this;

        onItemChanged(index);

        return true;
    }
    /// <summary>
    ///  attempts to add a new item to the loot container at the first free spot und returns true if successful
    /// </summary>
    /// <param name="itemToAdd"></param>
    /// <returns></returns>
    public bool tryAddItem(StorableItem itemToAdd)
    {

        //Debug.Log("added item: " + itemToAdd.displayName +" ; "+ itemToAdd.description);

        if (itemToAdd == null)
        {
            return true;
        }

        if (itemToAdd.m_owningContainer != null)
        {
            Debug.LogWarning("LootContainer: tryAddItem: item to add is still part of another loot container");
            return false;
        }

        for (int i = 0; i < m_storedItems.GetLength(0); i++)
        {
            if (m_storedItems[i] != null)
            {
                if (TryStackItem(i, itemToAdd))
                {
                    return true;
                }
            }

            if (m_storedItems[i] == null)
            {
                m_storedItems[i] = itemToAdd;
                itemToAdd.m_owningContainer = this;

                onItemChanged(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// removes an item from this container and returns the item
    /// </summary>
    /// <param name="indexX"></param>
    /// <param name="indexY"></param>
    /// <returns></returns>
    public StorableItem removeItem(int index)
    {
        if (index < 0 || index > m_storedItems.Length - 1)
        {
            Debug.LogWarning("LootContainer: removeItem: index is out of Range: " + index);
            return null;
        }

        StorableItem returnValue = m_storedItems[index];

        m_storedItems[index] = null;

        if (returnValue != null)
        {
            returnValue.m_owningContainer = null;
            onItemChanged(index);
        }

        return returnValue;
    }

    public bool setItem(StorableItem item, int index)
    {
        if (m_storedItems == null)
        {
            Debug.LogWarning("LootContainer: setItem: m_storedItems is null");
            return false;
        }

        if (index < 0 || index > m_storedItems.Length - 1)
        {
            Debug.LogWarning("LootContainer: setItem: index is out of Range: " + index);
            return false;
        }

        m_storedItems[index] = item;
        onItemChanged(index);
        return true;
    }

    /// <summary>
    /// switch 2 items within this loot container
    /// </summary>
    /// <param name="indexSource"></param>
    /// <param name="indexTarget"></param>
    public void switchItem(int indexSource, int indexTarget)
    {
        if (indexSource < 0 || indexSource > m_storedItems.Length - 1)
        {
            Debug.LogWarning("LootContainer: switchItem: indexSource is out of Range: " + indexSource);
            return;
        }

        if (indexTarget < 0 || indexTarget > m_storedItems.GetLength(0) - 1)
        {
            Debug.LogWarning("LootContainer: switchItem: indexTarget is out of Range: " + indexTarget);
            return;
        }

        StorableItem tempItem = m_storedItems[indexSource];


        if (TryStackItem(indexTarget, tempItem))
        {
            removeItem(indexSource);
            onItemChanged(indexSource);
            return;
        }


        m_storedItems[indexSource] = m_storedItems[indexTarget];
        m_storedItems[indexTarget] = tempItem;

        onItemChanged(indexSource);
        onItemChanged(indexTarget);
    }

    /// <summary>
    /// switch an item from this container with an item from another container
    /// </summary>
    /// <param name="indexSource"></param>
    /// <param name="indexTarget"></param>
    /// <param name="targetContainer"></param>
    public void switchItemContainer(int indexSource, int indexTarget, LootContainer targetContainer)
    {
        if (targetContainer == null)
        {
            Debug.LogWarning("LootContainer: switchItemContainer: targetContainer is null");
            return;
        }

        int targetContainterSize = targetContainer.getSize();

        if (indexSource < 0 || indexSource > m_storedItems.Length - 1)
        {
            Debug.LogWarning("LootContainer: switchItemContainer: indexSource is out of Range: " + indexSource);
            return;
        }

        if (indexTarget < 0 || indexTarget > targetContainterSize - 1)
        {
            Debug.LogWarning("LootContainer: switchItemContainer: indexTarget is out of Range: " + indexTarget);
            return;
        }

        StorableItem tempItemTarget = targetContainer.removeItem(indexTarget);
        StorableItem tempItemSource = removeItem(indexSource);

        targetContainer.tryAddItem(indexTarget, tempItemSource);
        tryAddItem(indexSource, tempItemTarget);
    }


    /// <summary>
    /// split an item in 2 stacks
    /// </summary>
    /// <param name="index"></param>
    public void SplitItem(int index)
    {
        if (m_storedItems[index] != null)
        {
            int teststacksize;
            StorableItem tempItem = ItemManager.singleton.createNewStorableItem(m_storedItems[index].itemTemplateIndex, 0);

            if (index < 0 || index > m_storedItems.Length - 1)
            {
                Debug.LogWarning("LootContainer: SplitItem: index is out of Range: " + index);
                return;
            }
            if (m_storedItems[m_storedItems.Length - 1] == null)
            {
                if (m_storedItems[index].isStackable)
                {
                    teststacksize = m_storedItems[index].m_stackSize / 2;
                    if (teststacksize > 0)
                    {
                        m_storedItems[index].m_stackSize -= teststacksize;
                        tempItem.m_stackSize = teststacksize;

                        for (int i = 0; i < m_storedItems.GetLength(0); i++)
                        {
                            if (m_storedItems[i] == null)
                            {
                                m_storedItems[i] = tempItem;
                                tempItem.m_owningContainer = this;

                                onItemChanged(i);
                                onItemChanged(index);
                                return;
                            }
                        }
                    }
                }
            }
        }
        return;


    }

    /// <summary>
    /// Try to Make 1 Stack
    /// </summary>
    /// <param name="index"></param>
    /// <param name="itemToAdd"></param>

    private bool TryStackItem(int index, StorableItem itemToAdd)
    {
        int teststacksize;
        if (index < 0 || index > m_storedItems.Length - 1)
        {
            Debug.LogWarning("LootContainer: TryStackItem: index is out of Range: " + index);
            return false;
        }

        if (itemToAdd == null)
        {
            Debug.LogWarning("LootContainer: TryStackItem: itemToAdd ist null : " + itemToAdd);
            return false;
        }

        if (m_storedItems[index] != null)
        {
            if (m_storedItems[index].itemTemplateIndex == itemToAdd.itemTemplateIndex)
            {
                if (itemToAdd.isStackable && m_storedItems[index].isStackable)
                {
                    if (m_storedItems[index].maxStackSize > m_storedItems[index].m_stackSize)
                    {
                        if (itemToAdd.maxStackSize > itemToAdd.m_stackSize)
                        {
                            teststacksize = m_storedItems[index].m_stackSize + itemToAdd.m_stackSize;

                            if (teststacksize <= m_storedItems[index].maxStackSize)
                            {
                                m_storedItems[index].m_stackSize += itemToAdd.m_stackSize;
                                onItemChanged(index);
                                return true;
                            }
                            else
                            {
                                if (m_storedItems[m_storedItems.Length - 1] == null)
                                {
                                    m_storedItems[index].m_stackSize = m_storedItems[index].maxStackSize;
                                    teststacksize -= m_storedItems[index].maxStackSize;
                                    itemToAdd.m_stackSize = teststacksize;
                                    for (int i = 0; i < m_storedItems.GetLength(0); i++)
                                    {
                                        if (m_storedItems[i] == null)
                                        {
                                            m_storedItems[i] = itemToAdd;
                                            itemToAdd.m_owningContainer = this;

                                            onItemChanged(i);
                                            onItemChanged(index);
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    public void tryDeleteItem(int amount, int index)
    {
        int tempstacksize;
        if (m_storedItems[index] == null)
        {
            Debug.LogWarning("LootContainer: TryDeleteItem: itemToDelet ist null : " + m_storedItems[index]);
            return;
        }

        tempstacksize = m_storedItems[index].m_stackSize - amount;

        if (tempstacksize <= 0)
        {
            removeItem(index);
            onItemChanged(index);
            return;
        }

        m_storedItems[index].m_stackSize -= amount;
        onItemChanged(index);
        return;
    }
}
