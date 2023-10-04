using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataEntity_Player : DataEntity_Damageable
{
    public int m_playerGameID;
    public LootContainer m_playerInventory;
    public LootContainer m_playerHotbar;

    public override List<byte> fillSaveData(List<byte> saveData)
    {
        base.fillSaveData(saveData);

        saveData.AddRange(BitConverter.GetBytes(m_playerGameID));

        StorableItem[] hotbarItems = m_playerHotbar.getAllItems();

        saveData.AddRange(BitConverter.GetBytes(hotbarItems.Length));
        for (int i = 0; i < hotbarItems.Length; i++)
        {
            if (hotbarItems[i] == null)
            {
                saveData.AddRange(BitConverter.GetBytes(-1));
                saveData.AddRange(BitConverter.GetBytes(-1));
            }
            else
            {
                saveData.AddRange(BitConverter.GetBytes(hotbarItems[i].itemTemplateIndex));
                saveData.AddRange(BitConverter.GetBytes(hotbarItems[i].m_stackSize));
            }
        }

        StorableItem[] inventoryItems = m_playerInventory.getAllItems();

        saveData.AddRange(BitConverter.GetBytes(inventoryItems.Length));
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i] == null)
            {
                saveData.AddRange(BitConverter.GetBytes(-1));
                saveData.AddRange(BitConverter.GetBytes(-1));
            }
            else
            {
                saveData.AddRange(BitConverter.GetBytes(inventoryItems[i].itemTemplateIndex));
                saveData.AddRange(BitConverter.GetBytes(inventoryItems[i].m_stackSize));
            }
        }

        return saveData;
    }

    public override int setFromSaveData(byte[] data, int startIndex)
    {
        startIndex = base.setFromSaveData(data, startIndex);

        m_playerGameID = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;
        int hotbarSize = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;

        Player_base playerPrefab = EntityManager.singleton.getEntityPrefab(5) as Player_base;

        m_playerHotbar = new LootContainer(playerPrefab.hotbarSize, 6);

        for (int i = 0; i < hotbarSize; i++)
        {
            StorableItem item = null;

            int itemIndex = BitConverter.ToInt32(data, startIndex);
            startIndex += 4;
            int itemStackCount = BitConverter.ToInt32(data, startIndex);
            startIndex += 4;

            if (itemIndex > -1)
            {
                item = ItemManager.singleton.createNewStorableItem(itemIndex, itemStackCount);
                m_playerHotbar.setItem(item, i);
            }
        }

        int InventorySize = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;

        m_playerInventory = new LootContainer(playerPrefab.inventorySize, 6);

        for (int i = 0; i < InventorySize; i++)
        {
            StorableItem item = null;

            int itemIndex = BitConverter.ToInt32(data, startIndex);
            startIndex += 4;
            int itemStackCount = BitConverter.ToInt32(data, startIndex);
            startIndex += 4;

            if (itemIndex > -1)
            {
                item = ItemManager.singleton.createNewStorableItem(itemIndex, itemStackCount);
                m_playerInventory.setItem(item, i);
            }
        }

        return startIndex;
    }
}
