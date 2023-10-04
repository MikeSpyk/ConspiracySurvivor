using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomItemDispenser
{
    public RandomItemDispenser(int[] itemIndices, int[] itemRarities)
    {
        if (itemIndices.Length != itemRarities.Length)
        {
            throw new System.ArgumentOutOfRangeException("RandomItemDispenser: RandomItemDispenser: itemIndices.length differs from itemRarities.length !");
        }

        m_rarity_index_itemID = new SortedDictionary<int, Dictionary<int, int>>();

        for (int i = 0; i < itemRarities.Length; i++)
        {
            if (!m_rarity_index_itemID.ContainsKey(itemRarities[i]))
            {
                m_rarity_index_itemID.Add(itemRarities[i], new Dictionary<int, int>());
            }

            m_rarity_index_itemID[itemRarities[i]].Add(m_rarity_index_itemID[itemRarities[i]].Count, itemIndices[i]);
        }

        int maxRarity = int.MinValue;

        foreach (KeyValuePair<int, Dictionary<int, int>> pair in m_rarity_index_itemID)
        {
            maxRarity = System.Math.Max(maxRarity, pair.Key);
        }

        int[] counts = new int[maxRarity + 1];

        foreach (KeyValuePair<int, Dictionary<int, int>> pair in m_rarity_index_itemID)
        {
            counts[pair.Key] = pair.Value.Count;
        }

        m_manager = new RandomItemManager(counts);
    }

    private SortedDictionary<int, Dictionary<int, int>> m_rarity_index_itemID;
    private RandomItemManager m_manager = null;

    public int getRandomItem()
    {
        int rarityLevel;
        int rarityElement;

        do
        {
            m_manager.getItem(out rarityLevel, out rarityElement);
        }
        while (rarityElement == -1);

        return m_rarity_index_itemID[rarityLevel][rarityElement];
    }
}
