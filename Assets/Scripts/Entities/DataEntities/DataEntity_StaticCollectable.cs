using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataEntity_StaticCollectable : DataEntity_Base
{
    public int m_rasterFieldID;

    public override List<byte> fillSaveData(List<byte> saveData)
    {
        base.fillSaveData(saveData);

        saveData.AddRange(BitConverter.GetBytes(m_rasterFieldID));

        return saveData;
    }

    public override int setFromSaveData(byte[] data, int startIndex)
    {
        startIndex = base.setFromSaveData(data, startIndex);

        m_rasterFieldID = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;

        WorldManager.singleton.registerResource(FieldResources.ResourceType.BerryPlant, m_rasterFieldID, new Vector2(m_position.x, m_position.z)); // is this right ? always adding more on top after every cull ?

        return startIndex;
    }
}
