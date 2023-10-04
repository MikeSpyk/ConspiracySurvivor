using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataEntity_PlayerSpawnPoint : DataEntity_Damageable
{
    public int m_playerGameID;
    public float m_lastTimeSpawned;

    public override List<byte> fillSaveData(List<byte> saveData)
    {
        base.fillSaveData(saveData);

        saveData.AddRange(BitConverter.GetBytes(m_playerGameID));

        return saveData;
    }

    public override int setFromSaveData(byte[] data, int startIndex)
    {
        startIndex = base.setFromSaveData(data, startIndex);

        m_playerGameID = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;

        return startIndex;
    }
}
