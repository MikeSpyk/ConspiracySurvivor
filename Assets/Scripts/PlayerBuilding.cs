using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerBuilding
{
    private const float SOCKET_DISTANCE_MAX = 0.01f; // max distance between 2 sockets from 2 different building parts to make a connection
    private const float SOCKET_ANGLE_DISTANCE_MAX = 0.01f;
    private const float BUILDING_PART_DISTANCE_MAX = 5f; // [6]  max distance between 2 building parts to do socket connection check
    private const float BUILDING_RADIUS_ADDITIONAL = 40f; // building blocked radius/height after the farthest building part

    public PlayerBuilding(Vector3 origin, int playerGameID, int buildingUID)
    {
        m_buildingUID = buildingUID;
        m_buildingOrigin = origin;
        m_allowedPlayersBuilding.Add(playerGameID);
    }
    public PlayerBuilding(byte[] data, int startIndex, out int endIndex)
    {
        endIndex = loadFromGameSaveData(data, startIndex);
    }

    private Vector3 m_buildingOrigin = Vector3.zero;
    private float m_buildingRadius = BUILDING_RADIUS_ADDITIONAL; // default distance
    private float m_buildingHeight = BUILDING_RADIUS_ADDITIONAL;
    private List<int> m_buildingPartsEntityID = new List<int>();
    private List<int> m_allowedPlayersBuilding = new List<int>(); // which players are allowed to build
    private bool m_radiusDirty = false;
    private int m_buildingUID;
    private bool m_completlyLoaded = false;
    private int m_lastFrameCheckCompletlyLoaded = 0;
    private bool m_stabilityDirty = false;

    public Vector3 buildingOrigin { get { return m_buildingOrigin; } }
    public float buildingRadius { get { return m_buildingRadius; } }
    public float buildingHeight { get { return m_buildingHeight; } }
    public int buildingPartsCount { get { return m_buildingPartsEntityID.Count; } }
    public int buildingUID { get { return m_buildingUID; } }
    public bool completlyLoaded { get { return checkBuildingCompletelyLoaded(); } }

    public List<byte> getGameSaveData()
    {
        List<byte> returnValue = new List<byte>();

        returnValue.AddRange(BitConverter.GetBytes(m_buildingOrigin.x));
        returnValue.AddRange(BitConverter.GetBytes(m_buildingOrigin.y));
        returnValue.AddRange(BitConverter.GetBytes(m_buildingOrigin.z));

        returnValue.AddRange(BitConverter.GetBytes(m_buildingRadius));
        returnValue.AddRange(BitConverter.GetBytes(m_buildingHeight));
        returnValue.AddRange(BitConverter.GetBytes(m_buildingUID));

        returnValue.AddRange(BitConverter.GetBytes(m_allowedPlayersBuilding.Count));

        for (int i = 0; i < m_allowedPlayersBuilding.Count; i++)
        {
            returnValue.AddRange(BitConverter.GetBytes(m_allowedPlayersBuilding[i]));
        }

        returnValue.AddRange(BitConverter.GetBytes(m_buildingPartsEntityID.Count));

        for (int i = 0; i < m_buildingPartsEntityID.Count; i++)
        {
            returnValue.AddRange(BitConverter.GetBytes(m_buildingPartsEntityID[i]));
        }

        return returnValue;
    }

    private int loadFromGameSaveData(byte[] data, int index)
    {
        m_buildingOrigin.x = BitConverter.ToSingle(data, index);
        index += 4;
        m_buildingOrigin.y = BitConverter.ToSingle(data, index);
        index += 4;
        m_buildingOrigin.z = BitConverter.ToSingle(data, index);
        index += 4;

        m_buildingRadius = BitConverter.ToSingle(data, index);
        index += 4;

        m_buildingHeight = BitConverter.ToSingle(data, index);
        index += 4;

        m_buildingUID = BitConverter.ToInt32(data, index);
        index += 4;

        int tempCount = BitConverter.ToInt32(data, index);
        index += 4;

        for (int i = 0; i < tempCount; i++)
        {
            m_allowedPlayersBuilding.Add(BitConverter.ToInt32(data, index));
            index += 4;
        }

        tempCount = BitConverter.ToInt32(data, index);
        index += 4;

        for (int i = 0; i < tempCount; i++)
        {
            m_buildingPartsEntityID.Add(BitConverter.ToInt32(data, index));
            index += 4;
        }

        return index;
    }

    public void updateBuilding()
    {
        if (completlyLoaded)
        {
            if (m_stabilityDirty)
            {
                for (int i = 0; i < m_buildingPartsEntityID.Count; i++)
                {
                    PlayerConstruction buildingPart = EntityManager.singleton.getEntity(m_buildingPartsEntityID[i]) as PlayerConstruction;

                    if (buildingPart != null)
                    {
                        buildingPart.m_stability = 0;
                    }
                }

                for (int i = 0; i < m_buildingPartsEntityID.Count; i++)
                {
                    PlayerConstruction buildingPart = EntityManager.singleton.getEntity(m_buildingPartsEntityID[i]) as PlayerConstruction;

                    if (buildingPart != null && buildingPart.buildingType == PlayerConstruction.BuildingPartType.Foundation)
                    {
                        buildingPart.recalculateStability(true);
                    }
                }

                // remove unconnected/floating building parts
                for (int i = 0; i < m_buildingPartsEntityID.Count; i++)
                {
                    PlayerConstruction buildingPart = EntityManager.singleton.getEntity(m_buildingPartsEntityID[i]) as PlayerConstruction;

                    if (buildingPart != null)
                    {
                        if (buildingPart.m_stability <= 0) //&& !buildingPart.isDestroyed)
                        {
                            buildingPart.destroyBuildingPart(false);
                        }
                    }
                }

                m_stabilityDirty = false;
            }

            if (m_radiusDirty)
            {
                float minX = float.MaxValue;
                float maxX = float.MinValue;
                float minZ = float.MaxValue;
                float maxZ = float.MinValue;
                float minY = float.MaxValue;
                float maxY = float.MinValue;

                for (int i = 0; i < m_buildingPartsEntityID.Count; i++)
                {
                    PlayerConstruction buildingPart = EntityManager.singleton.getEntity(m_buildingPartsEntityID[i]) as PlayerConstruction;

                    if (buildingPart != null)
                    {
                        if (buildingPart.buildingType == PlayerConstruction.BuildingPartType.Foundation)
                        {
                            minX = Mathf.Min(buildingPart.transform.position.x, minX);
                            minZ = Mathf.Min(buildingPart.transform.position.z, minZ);
                            maxX = Mathf.Max(buildingPart.transform.position.x, maxX);
                            maxZ = Mathf.Max(buildingPart.transform.position.z, maxZ);
                            minY = Mathf.Min(buildingPart.transform.position.y, minY);
                        }
                        else if (buildingPart.buildingType == PlayerConstruction.BuildingPartType.Wall)
                        {
                            maxY = Mathf.Max(buildingPart.transform.position.y, maxY);
                        }
                    }
                }

                maxY = Mathf.Max(maxY, minY); // if no wall present

                m_buildingRadius = Mathf.Max(maxX - minX, maxZ - minZ) + BUILDING_RADIUS_ADDITIONAL;
                m_buildingOrigin = new Vector3(minX + (maxX - minX) / 2, minY, minZ + (maxZ - minZ) / 2);
                m_buildingHeight = maxY - minY + BUILDING_RADIUS_ADDITIONAL;

                //Debug.Log("m_buildingRadius: " + m_buildingRadius.ToString() + "; m_buildingOrigin: " + m_buildingOrigin.ToString() + "; m_buildingHeight: " + m_buildingHeight.ToString());

                m_radiusDirty = false;
            }
        }
    }

    public void addBuildingPart(PlayerConstruction buildingPart) // from new entity and unculled entity
    {
        if (buildingPart.buildingType == PlayerConstruction.BuildingPartType.Foundation || buildingPart.buildingType == PlayerConstruction.BuildingPartType.Wall)
        {
            m_radiusDirty = true;
        }

        m_buildingPartsEntityID.Add(buildingPart.entityUID);
        buildingPart.setAssociatedBuilding(this);

        if (completlyLoaded)
        {
            updateConnection(buildingPart);
            buildingPart.recalculateStability(true);
        }

        //Debug.Log("building part add: count: " + buildingPartsCount);
    }

    public void onBuildingPartDestroyed(PlayerConstruction buildingPart, bool updateBuildingStability)
    {
        if (buildingPart.buildingType == PlayerConstruction.BuildingPartType.Foundation || buildingPart.buildingType == PlayerConstruction.BuildingPartType.Wall)
        {
            m_radiusDirty = true;
        }

        int listsIndex = m_buildingPartsEntityID.IndexOf(buildingPart.entityUID);

        if (listsIndex != -1)
        {
            if (updateBuildingStability)
            {
                m_stabilityDirty = true;
            }
            m_buildingPartsEntityID.RemoveAt(listsIndex);
        }

        for (int i = 0; i < buildingPart.m_connectedBuildPartsEntityID.Length; i++)
        {
            for (int j = 0; j < buildingPart.m_connectedBuildPartsEntityID[i].Count; j++)
            {
                PlayerConstruction connectedBuildingPart = EntityManager.singleton.getEntity(buildingPart.m_connectedBuildPartsEntityID[i][j]) as PlayerConstruction;

                if (connectedBuildingPart != null)
                {
                    connectedBuildingPart.removeConnectionToSockets(buildingPart.entityUID);
                }
            }
        }
    }

    /// <summary>
    /// are all building-parts (entities) active (true). or is at least one culled (false)
    /// </summary>
    /// <returns></returns>
    public bool checkBuildingCompletelyLoaded()
    {
        if (m_lastFrameCheckCompletlyLoaded == Time.frameCount)
        {
            return m_completlyLoaded;
        }

        m_lastFrameCheckCompletlyLoaded = Time.frameCount;

        for (int i = 0; i < m_buildingPartsEntityID.Count; i++)
        {
            if (!EntityManager.singleton.isEntityActive(m_buildingPartsEntityID[i]))
            {
                m_completlyLoaded = false;
                return m_completlyLoaded;
            }
        }

        m_completlyLoaded = true;
        return m_completlyLoaded;
    }

    private void updateConnection(PlayerConstruction currentBuildingPart)
    {
        BuildingSocket[] currentSocketTemplate;
        Vector3[] currentSocketsPositions;
        Quaternion[] currentSocketsRotations;
        List<int>[] currentSocketsConnectedBuildings;

        currentBuildingPart.getBuildingSockets(out currentSocketTemplate, out currentSocketsPositions, out currentSocketsRotations, out currentSocketsConnectedBuildings);

        for (int i = 0; i < m_buildingPartsEntityID.Count; i++)
        {
            PlayerConstruction compareBuildingPart = EntityManager.singleton.getEntity(m_buildingPartsEntityID[i]) as PlayerConstruction;

            if (compareBuildingPart == null || currentBuildingPart == compareBuildingPart || Vector3.Distance(compareBuildingPart.transform.position, currentBuildingPart.transform.position) > BUILDING_PART_DISTANCE_MAX)
            {
                continue;
            }

            //Debug.DrawLine(compareBuildingPart.transform.position, currentBuildingPart.transform.position, Color.cyan, 2f);

            BuildingSocket[] compareSocketTemplate;
            Vector3[] compareSocketsPositions;
            Quaternion[] compareSocketsRotations;
            List<int>[] compareSocketsConnectedBuildings;

            compareBuildingPart.getBuildingSockets(out compareSocketTemplate, out compareSocketsPositions, out compareSocketsRotations, out compareSocketsConnectedBuildings);

            for (int k = 0; k < currentSocketTemplate.Length; k++)
            {
                if (!currentSocketTemplate[k].isBuildingPartAllowed(compareBuildingPart.buildingType))
                {
                    continue;
                }

                for (int l = 0; l < compareSocketTemplate.Length; l++)
                {
                    //Debug.DrawRay(compareSocketsPositions[l], Vector3.up, Color.cyan, 2f);

                    if (compareSocketTemplate[l].isBuildingPartAllowed(currentBuildingPart.buildingType) && Vector3.Distance(currentSocketsPositions[k], compareSocketsPositions[l]) < SOCKET_DISTANCE_MAX)
                    {
                        currentBuildingPart.addConnectionToSocket(k, compareBuildingPart);
                        compareBuildingPart.addConnectionToSocket(l, currentBuildingPart);

                        goto LOOP_END; // there can only be 1 connection between 2 building parts --> stop comparing
                    }
                }
            }

        LOOP_END:;
        }
    }

    private void recalculateOriginSize()
    {

    }

    public bool isPlayerAllowedBuilding(int gameID)
    {
        if (m_allowedPlayersBuilding.Contains(gameID))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool isPlayerAllowedOpenDoor(int gameID)
    {
        if (m_allowedPlayersBuilding.Contains(gameID))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void clearAllowedPlayersBuilding()
    {
        m_allowedPlayersBuilding.Clear();
    }
}
