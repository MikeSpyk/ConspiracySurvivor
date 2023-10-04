using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataEntity_Damageable : DataEntity_Base
{
    public float m_health;

    public override List<byte> fillSaveData(List<byte> saveData)
    {
        base.fillSaveData(saveData);
        saveData.AddRange(BitConverter.GetBytes(m_health));

        return saveData;
    }

    public override int setFromSaveData(byte[] data, int startIndex)
    {
        startIndex = base.setFromSaveData(data, startIndex);
        m_health = BitConverter.ToSingle(data, startIndex);
        startIndex += 4;

        return startIndex;
    }
}
