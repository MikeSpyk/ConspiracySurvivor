using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataEntity_HarvestableResource : DataEntity_Base
{
    public float m_resourceAmount = 0;
    public int m_rasterFieldID = -1;
    public FieldResources.ResourceType m_resourceType = FieldResources.ResourceType.Undefined;

    public override List<byte> fillSaveData(List<byte> saveData)
    {
        base.fillSaveData(saveData);

        saveData.AddRange(BitConverter.GetBytes(m_resourceAmount));
        saveData.AddRange(BitConverter.GetBytes(m_rasterFieldID));
        saveData.AddRange(BitConverter.GetBytes((int)m_resourceType));

        return saveData;
    }

    public override int setFromSaveData(byte[] data, int startIndex)
    {
        startIndex = base.setFromSaveData(data, startIndex);

        m_resourceAmount = BitConverter.ToSingle(data, startIndex);
        startIndex += 4;
        m_rasterFieldID = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;
        m_resourceType = (FieldResources.ResourceType)BitConverter.ToInt32(data, startIndex);
        startIndex += 4;

        WorldManager.singleton.registerResource(m_resourceType, m_rasterFieldID, new Vector2(m_position.x, m_position.z));

        return startIndex;
    }

}
