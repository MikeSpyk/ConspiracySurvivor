using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataEntity_BuildingPartWall : DataEntity_BuildingPart
{
    public PlayerConstruction.BuildingPartType m_attachmentType = PlayerConstruction.BuildingPartType.Default;
    public bool m_hasAttachment = false;
    public Vector2 m_attachmentPosition;

    public override List<byte> fillSaveData(List<byte> saveData)
    {
        base.fillSaveData(saveData);

        saveData.AddRange(BitConverter.GetBytes((int)m_attachmentType));
        saveData.AddRange(BitConverter.GetBytes(m_hasAttachment));
        saveData.AddRange(BitConverter.GetBytes(m_attachmentPosition.x));
        saveData.AddRange(BitConverter.GetBytes(m_attachmentPosition.y));

        return saveData;
    }

    public override int setFromSaveData(byte[] data, int startIndex)
    {
        startIndex = base.setFromSaveData(data, startIndex);

        m_attachmentType = (PlayerConstruction.BuildingPartType)BitConverter.ToInt32(data, startIndex);
        startIndex += 4;
        m_hasAttachment = BitConverter.ToBoolean(data, startIndex);
        startIndex += 1;
        m_attachmentPosition = new Vector2(BitConverter.ToSingle(data, startIndex), BitConverter.ToSingle(data, startIndex + 4));
        startIndex += 8;

        return startIndex;
    }
}
