using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataEntity_BuildingPart : DataEntity_Damageable
{
    public int m_builtByPlayerGameID;
    public int m_associatedBuildingUID;
    public float m_stability;
    public List<int>[] m_connectedBuildPartsEntityID;

    public override List<byte> fillSaveData(List<byte> saveData)
    {
        base.fillSaveData(saveData);

        saveData.AddRange(BitConverter.GetBytes(m_builtByPlayerGameID));
        saveData.AddRange(BitConverter.GetBytes(m_associatedBuildingUID));
        saveData.AddRange(BitConverter.GetBytes(m_stability));
        saveData.AddRange(BitConverter.GetBytes(m_connectedBuildPartsEntityID.Length));

        for (int i = 0; i < m_connectedBuildPartsEntityID.Length; i++)
        {
            saveData.AddRange(BitConverter.GetBytes(m_connectedBuildPartsEntityID[i].Count));
            for (int j = 0; j < m_connectedBuildPartsEntityID[i].Count; j++)
            {
                saveData.AddRange(BitConverter.GetBytes(m_connectedBuildPartsEntityID[i][j]));
            }
        }

        return saveData;
    }

    public override int setFromSaveData(byte[] data, int startIndex)
    {
        startIndex = base.setFromSaveData(data, startIndex);

        m_builtByPlayerGameID = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;
        m_associatedBuildingUID = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;
        m_stability = BitConverter.ToSingle(data, startIndex);
        startIndex += 4;
        int endI = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;

        m_connectedBuildPartsEntityID = new List<int>[endI];

        for (int i = 0; i < endI; i++)
        {
            int endJ = BitConverter.ToInt32(data, startIndex);
            startIndex += 4;

            m_connectedBuildPartsEntityID[i] = new List<int>();

            for (int j = 0; j < endJ; j++)
            {
                m_connectedBuildPartsEntityID[i].Add(BitConverter.ToInt32(data, startIndex));
                startIndex += 4;
            }
        }

        return startIndex;
    }
}
