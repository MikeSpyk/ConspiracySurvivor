using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuildingPentagonFloor : PlayerConstruction
{
    private static readonly BuildingSocket[] m_buildingSockets = new BuildingSocket[]
    {
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(1.2135f, 0f, 1.4265f),
            m_socketRotation = Quaternion.Euler(0,36,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Floor },
            m_buildingPartSocketVisible = new bool[] { true, true }
        },
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(1.9635f, 0f, -0.8816f),
            m_socketRotation = Quaternion.Euler(0,108,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Floor },
            m_buildingPartSocketVisible = new bool[] { true, true }
        },
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(0f, 0f, -2.3082f),
            m_socketRotation = Quaternion.Euler(0,180,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Floor },
            m_buildingPartSocketVisible = new bool[] { true, true }
        },
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(-1.9635f, 0f, -0.8816f),
            m_socketRotation = Quaternion.Euler(0,252,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Floor },
            m_buildingPartSocketVisible = new bool[] { true, true }
        },
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(-1.2135f, 0f, 1.4265f),
            m_socketRotation = Quaternion.Euler(0,324,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Floor },
            m_buildingPartSocketVisible = new bool[] { true, true }
        }
    };

    protected override BuildingSocket[] getBuildingSocketTemplate()
    {
        return m_buildingSockets;
    }
}
