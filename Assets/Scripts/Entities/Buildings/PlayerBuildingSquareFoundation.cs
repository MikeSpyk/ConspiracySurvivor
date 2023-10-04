using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuildingSquareFoundation : PlayerConstruction
{
    private static readonly BuildingSocket[] m_buildingSockets = new BuildingSocket[]
    {
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(0f, 1.5f, 1.5f),
            m_socketRotation = Quaternion.Euler(0,0,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Foundation },
            m_buildingPartSocketVisible = new bool[] { true, true }
        },
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(0f, 1.5f, -1.5f),
            m_socketRotation = Quaternion.Euler(0,180,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Foundation },
            m_buildingPartSocketVisible = new bool[] { true, true }
        },
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(1.5f, 1.5f, 0f),
            m_socketRotation = Quaternion.Euler(0,90,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Foundation },
            m_buildingPartSocketVisible = new bool[] { true, true }
        },
           new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(-1.5f, 1.5f, 0f),
            m_socketRotation = Quaternion.Euler(0,270,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Foundation },
            m_buildingPartSocketVisible = new bool[] { true, true }
        },
    };

    protected override BuildingSocket[] getBuildingSocketTemplate()
    {
        return m_buildingSockets;
    }
}
