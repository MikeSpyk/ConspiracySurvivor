using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataEntity_BuildingPartDoor : DataEntity_Damageable
{
    public int m_associatedBuildingID;
    public int m_associatedWallEntityID;

    public override List<byte> fillSaveData(List<byte> saveData)
    {
        base.fillSaveData(saveData);
        saveData.AddRange(BitConverter.GetBytes(m_associatedBuildingID));
        saveData.AddRange(BitConverter.GetBytes(m_associatedWallEntityID));

        return saveData;
    }

    public override int setFromSaveData(byte[] data, int startIndex)
    {
        startIndex = base.setFromSaveData(data, startIndex);
        m_associatedBuildingID = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;
        m_associatedWallEntityID = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;

        return startIndex;
    }
}
