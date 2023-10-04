using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataEntity_Base
{
    public int m_entityPrefabID = -1;
    public int m_entity_UID = -1;
    public int m_gridFieldUID = -2;
    public Vector3 m_position = Vector3.zero;
    public Quaternion m_rotation = Quaternion.identity;
    public List<int> m_observingDistantPlayers;

    /// <summary>
    /// appends save file for this entity to a given byte-list
    /// </summary>
    /// <param name="saveData">list to append the data to. If you call this from outside of the inheritance hierarchy then this should probably ne a new list </param>
    /// <returns></returns>
    public virtual List<byte> fillSaveData(List<byte> saveData)
    {
        saveData.AddRange(BitConverter.GetBytes(m_entityPrefabID));
        saveData.AddRange(BitConverter.GetBytes(m_entity_UID));
        saveData.AddRange(BitConverter.GetBytes(m_gridFieldUID));
        saveData.AddRange(BitConverter.GetBytes(m_position.x));
        saveData.AddRange(BitConverter.GetBytes(m_position.y));
        saveData.AddRange(BitConverter.GetBytes(m_position.z));
        saveData.AddRange(BitConverter.GetBytes(m_rotation.eulerAngles.x));
        saveData.AddRange(BitConverter.GetBytes(m_rotation.eulerAngles.y));
        saveData.AddRange(BitConverter.GetBytes(m_rotation.eulerAngles.z));

        return saveData;
    }

    //      int:        4 byte 
    //      float:      4 byte

    /// <summary>
    /// fills the data entity with save data created with "fillSaveData"
    /// </summary>
    /// <param name="data"></param>
    /// <param name="startIndex"></param>
    /// <returns></returns>
    public virtual int setFromSaveData(byte[] data, int startIndex)
    {
        m_entity_UID = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;
        m_gridFieldUID = BitConverter.ToInt32(data, startIndex);
        startIndex += 4;
        m_position.x = BitConverter.ToSingle(data, startIndex);
        startIndex += 4;
        m_position.y = BitConverter.ToSingle(data, startIndex);
        startIndex += 4;
        m_position.z = BitConverter.ToSingle(data, startIndex);
        startIndex += 4;
        float rotX = BitConverter.ToSingle(data, startIndex);
        startIndex += 4;
        float rotY = BitConverter.ToSingle(data, startIndex);
        startIndex += 4;
        float rotZ = BitConverter.ToSingle(data, startIndex);
        startIndex += 4;

        m_rotation = Quaternion.Euler(rotX, rotY, rotZ);

        m_observingDistantPlayers = new List<int>();

        return startIndex;
    }
}
