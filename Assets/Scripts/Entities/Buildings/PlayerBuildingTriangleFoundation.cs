using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuildingTriangleFoundation : PlayerConstruction
{
    private static readonly BuildingSocket[] m_buildingSockets = new BuildingSocket[]
    {
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(0.75f, 1.5f, 0),
            m_socketRotation = Quaternion.Euler(0,60,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Foundation },
            m_buildingPartSocketVisible = new bool[] { true, true }
        },
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(-0.75f, 1.5f, 0),
            m_socketRotation = Quaternion.Euler(0,300,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Foundation },
            m_buildingPartSocketVisible = new bool[] { true, true }
        },
        new BuildingSocket
        {
            m_socketPositionOffset = new Vector3(0f, 1.5f, -1.299f),
            m_socketRotation = Quaternion.Euler(0,180,0),
            m_allowedBuildingParts = new BuildingPartType[]{ BuildingPartType.Wall, BuildingPartType.Foundation },
            m_buildingPartSocketVisible = new bool[] { true, true }
        }
    };

    protected override BuildingSocket[] getBuildingSocketTemplate()
    {
        return m_buildingSockets;
    }
}
