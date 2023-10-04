using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataEntity_LootContainer : DataEntity_Damageable
{
    public LootContainer m_lootContainer;

    public override List<byte> fillSaveData(List<byte> saveData)
    {
        base.fillSaveData(saveData);

        StorableItem[] items = m_lootContainer.getAllItems();

        saveData.AddRange(BitConverter.GetBytes(items.Length));
        saveData.AddRange(BitConverter.GetBytes(m_lootContainer.elementsPerRow));

        for (int i = 0;i < items.Length; i++)
        {
            if(items[i] == null)
            {
                saveData.AddRange(BitConverter.GetBytes(-1));
                saveData.AddRange(BitConverter.GetBytes(0));
            }
            else
            {
                saveData.AddRange(BitConverter.GetBytes(items[i].itemTemplateIndex));
                saveData.AddRange(BitConverter.GetBytes(items[i].m_stackSize));
            }
        }

        return saveData;
    }

    public override int setFromSaveData(byte[] data, int startIndex)
    {
        startIndex = base.setFromSaveData(data, startIndex);

        int count = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;

        int elementsPerRow = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;

        m_lootContainer = new LootContainer(count, elementsPerRow);

        for (int i = 0; i < count; i++)
        {
            int itemID = BitConverter.ToInt32(data, startIndex);
            startIndex += 4;

            StorableItem item = null;

            if (itemID != -1)
            {
                item = new StorableItem(itemID, BitConverter.ToInt32(data, startIndex));
                m_lootContainer.setItem(item, i);
            }
            startIndex += 4;
        }

        return startIndex;
    }
}
