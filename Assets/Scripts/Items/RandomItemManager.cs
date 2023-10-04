using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomItemManager
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="itemsCountByRarity">An array containing the items counts for each rarity level</param>
    public RandomItemManager(int[] itemsCountByRarity)
    {
        if(itemsCountByRarity.Length < 2)
        {
            Debug.LogError("RandomItemManager: RandomItemManager: there must be at least 2 rarity levels in order for this class to work correctly !");
            // item intervalls will be distributes wrong otherwise. maybe fix ?
        }

        m_rarityLevels = itemsCountByRarity.Length;
        m_itemsByRarity = itemsCountByRarity;

        m_rarityLevelRange = new float[itemsCountByRarity.Length];
        m_rarityLevelItemsRange = new float[itemsCountByRarity.Length][];

        float lastLevelRarity = 1f;
        float levelRarity = 0.5f;

        float levelOffset = 0;

        for (int i = 0; i < itemsCountByRarity.Length; i++)
        {
            if (i == itemsCountByRarity.Length - 2) // special procedure for last intervall to make it smaller than the intervall before. becaue otherwise it would have the same size as the intervall before because dividing by 2 get 2 equaly sized intervalls
            {
                levelRarity += levelRarity / 2;
            }

            if (i == itemsCountByRarity.Length - 1) // special procedure for last intervall to make it smaller than the intervall before. becaue otherwise it would have the same size as the intervall before because dividing by 2 get 2 equaly sized intervalls
            {
                levelRarity = lastLevelRarity / 3;
            }

            m_rarityLevelRange[i] = levelOffset + levelRarity;

            m_rarityLevelItemsRange[i] = new float[itemsCountByRarity[i]];
            float itemRarityStep = levelRarity / itemsCountByRarity[i];

            float itemRarity = levelOffset + itemRarityStep;

            for (int j = 0; j < itemsCountByRarity[i]; j++)
            {
                m_rarityLevelItemsRange[i][j] = itemRarity;
                itemRarity += itemRarityStep;
            }

            levelOffset += levelRarity;
            lastLevelRarity = levelRarity;
            levelRarity /= 2;
        }
    }

    private int m_rarityLevels = -1; // how many levels of rarity
    private int[] m_itemsByRarity = null; // how many items for each rarity level
    private float[] m_rarityLevelRange = null; // maps a (max)precentage value to a certain rarity level (how are the rarity levels distributed in percent)
    private float[][] m_rarityLevelItemsRange = null; // maps a precentage value to an item (of a specified rarity level). for rarity[0]: 50 - 51% = itemA, 51 - 52% = itemB,...
    private System.Random m_ramdom = new System.Random();
#if RANDOMITEMMANAGER_DEBUG
        public int m_DEBUG_maxReRolls = 0;
#endif


    /// <summary>
    /// gets a random item defined by the index of the rarity level and the index of the item within the rarity level
    /// </summary>
    /// <param name="rarityLevel">the rarity level index</param>
    /// <param name="itemIndex">the items index within the determined rarity level</param>
    public void getItem(out int rarityLevel, out int itemIndex)
    {
        int[] rarityLevels = new int[m_rarityLevels];

        for (int i = 0; i < m_rarityLevels; i++)
        {
            rarityLevels[i] = i;
        }

        getItem(out rarityLevel, out itemIndex, rarityLevels);
    }

    /// <summary>
    /// gets a random item defined by the index of the rarity and the index of the item within the rarity level. the item will get choosen from the given rarity levels only
    /// </summary>
    /// <param name="rarityLevel">the rarity level index</param>
    /// <param name="itemIndex">the items index within the determined rarity level</param>
    /// <param name="rarityLevelsFilter">the rarity levels to choose from</param>
    public void getItem(out int rarityLevel, out int itemIndex, params int[] rarityLevelsFilter)
    {
        float randomValue;

#if RANDOMITEMMANAGER_DEBUG
            int reRolls = 0;
#endif

        while (true)
        {
            randomValue = (float)m_ramdom.NextDouble();

            // choose rarity level
            rarityLevel = m_rarityLevels - 1;

            for (int i = 0; i < m_rarityLevels; i++)
            {
                if (randomValue < m_rarityLevelRange[i]) // 0 ist most commen rarity
                {
                    rarityLevel = i;
                    break;
                }
            }

            bool withinRange = false;

            for (int i = 0; i < rarityLevelsFilter.Length; i++)
            {
                if (rarityLevelsFilter[i] == rarityLevel)
                {
                    withinRange = true;
                    break;
                }
            }

            if (withinRange)
            {
                break;
            }
#if RANDOMITEMMANAGER_DEBUG
                else
                {
                    reRolls++;
                }
#endif
        }

#if RANDOMITEMMANAGER_DEBUG
            m_DEBUG_maxReRolls = Math.Max(reRolls, m_DEBUG_maxReRolls);
#endif

        // choose item from rarity level

        itemIndex = m_itemsByRarity[rarityLevel] - 1;

        for (int i = 0; i < m_itemsByRarity[rarityLevel]; i++)
        {
            if (randomValue < m_rarityLevelItemsRange[rarityLevel][i])
            {
                itemIndex = i;
                break;
            }
        }
    }
}
