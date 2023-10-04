using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataEntity_DroppedItem : DataEntity_Base
{
    public StorableItem m_item;

    public override List<byte> fillSaveData(List<byte> saveData)
    {
        base.fillSaveData(saveData);

        saveData.AddRange(BitConverter.GetBytes(m_item.itemTemplateIndex));
        saveData.AddRange(BitConverter.GetBytes(m_item.m_stackSize));

        return saveData;
    }

    public override int setFromSaveData(byte[] data, int startIndex)
    {
        startIndex = base.setFromSaveData(data, startIndex);

        m_item = ItemManager.singleton.createNewStorableItem(BitConverter.ToInt32(data, startIndex), BitConverter.ToInt32(data, startIndex + 4));
        startIndex += 8;

        return startIndex;
    }
}
