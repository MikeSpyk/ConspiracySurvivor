using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingSocket
{
    public Vector3 m_socketPositionOffset;
    public Quaternion m_socketRotation;
    public PlayerConstruction.BuildingPartType[] m_allowedBuildingParts;
    public bool[] m_buildingPartSocketVisible; // in building mode

    public bool isBuildingPartAllowed(PlayerConstruction.BuildingPartType type)
    {
        for(int i = 0; i < m_allowedBuildingParts.Length; i++)
        {
            if(m_allowedBuildingParts[i] == type)
            {
                return true;
            }
        }

        return false;
    }

    public bool isBuildingPartAllowedAndVisible(PlayerConstruction.BuildingPartType type)
    {
        for (int i = 0; i < m_allowedBuildingParts.Length; i++)
        {
            if (m_allowedBuildingParts[i] == type && m_buildingPartSocketVisible[i] == true)
            {
                return true;
            }
        }

        return false;
    }
}
