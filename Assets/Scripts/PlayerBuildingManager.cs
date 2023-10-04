using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerBuildingManager : MonoBehaviour
{
    private static PlayerBuildingManager m_singleton;

    [SerializeField] private bool m_updateBuildings = true;
    [SerializeField, ReadOnly] private int m_buildingCounter = 0;

    private List<PlayerBuilding> m_playerBuildings = new List<PlayerBuilding>();

    public static PlayerBuildingManager singleton
    {
        get
        {
            return m_singleton;
        }
    }

    protected void Awake()
    {
        m_singleton = this;
    }

    protected void Update()
    {
        if (m_updateBuildings)
        {
            for (int i = 0; i < m_playerBuildings.Count; i++)
            {
                m_playerBuildings[i].updateBuilding();
            }
        }
    }

    public List<byte> getGameSaveData()
    {
        List<byte> returnValue = new List<byte>();

        returnValue.AddRange(BitConverter.GetBytes(m_buildingCounter));
        
        returnValue.AddRange(BitConverter.GetBytes(m_playerBuildings.Count));

        for(int i = 0; i < m_playerBuildings.Count; i++)
        {
            returnValue.AddRange(m_playerBuildings[i].getGameSaveData());
        }

        return returnValue;
    }

    public int loadFromSaveData(byte[] data, int index)
    {
        m_buildingCounter = BitConverter.ToInt32(data, index);
        index += 4;

        int buildingCount = BitConverter.ToInt32(data, index);
        index += 4;

        for (int i = 0; i < buildingCount; i++)
        {
            int endIndex;

            m_playerBuildings.Add(new PlayerBuilding(data, index, out endIndex));

            index = endIndex;
        }

        return index;
    }

    public void server_onPlayerSetAdditionAttachment(int wallEntityID, int playerGameID, PlayerConstruction.BuildingPartType attachmentType)
    {
        PlayerBuilidingWall wall = EntityManager.singleton.getEntity(wallEntityID) as PlayerBuilidingWall;

        if (wall != null)
        {
            if (wall.m_associatedBuilding != null)
            {
                if (wall.m_associatedBuilding.isPlayerAllowedBuilding(playerGameID))
                {
                    if (!wall.additionSocketOccuipied && wall.additionalSocketType == attachmentType)
                    {
                        wall.occupieAdditionSocket(attachmentType);
                    }
                }
            }
        }
    }

    public void server_onPlayerAddAttachmentRequest(int wallEntityID, PlayerConstruction.BuildingPartType attachmentType, Vector2 attachmentPosition)
    {
        PlayerBuilidingWall wall = EntityManager.singleton.getEntity(wallEntityID) as PlayerBuilidingWall;

        if (wall != null)
        {
            wall.setAttachment(attachmentType, attachmentPosition);
        }
    }

    public void server_onPlayerBuildRequest(int prefabIndex, Vector3 position, Quaternion rotation, int playerGameID)
    {
        Player_base player = EntityManager.singleton.getActivePlayer(playerGameID);

        if (player != null)
        {
            if (checkPlayerBuildingAllowed(player))
            {
                GameObject spawnedBuildingPart = EntityManager.singleton.spawnEntity(prefabIndex, position, rotation);

                if (spawnedBuildingPart != null)
                {
                    PlayerConstruction script = spawnedBuildingPart.GetComponent<PlayerConstruction>();

                    if (script != null)
                    {
                        script.builtByPlayerGameID = playerGameID;
                    }
                }
            }
        }
    }

    public void server_onBuildingPartSpawned(PlayerConstruction buildingPart)
    {
        PlayerBuilding closestBuilding = getBuildingClaimingPosition(buildingPart.transform.position);

        if (closestBuilding == null)
        {
            PlayerBuilding newBuilding = new PlayerBuilding(buildingPart.transform.position, buildingPart.builtByPlayerGameID, m_buildingCounter);
            m_buildingCounter++;
            newBuilding.addBuildingPart(buildingPart);
            m_playerBuildings.Add(newBuilding);
        }
        else
        {
            closestBuilding.addBuildingPart(buildingPart);
        }
    }

    /// <summary>
    /// if this position is within the claimed area of a building, the associated building will be returned. if no building has claimed this position null will be returned
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    private PlayerBuilding getBuildingClaimingPosition(Vector3 position)
    {
        int associatedBuildingIndex = -1;

        for (int i = 0; i < m_playerBuildings.Count; i++)
        {
            if (Vector2.Distance(VectorTools.Vec2FromVec3XZ(m_playerBuildings[i].buildingOrigin), VectorTools.Vec2FromVec3XZ(position)) < m_playerBuildings[i].buildingRadius
                     && Mathf.Abs(position.y - m_playerBuildings[i].buildingOrigin.y) < m_playerBuildings[i].buildingHeight)
            {
                associatedBuildingIndex = i;
                break;
            }
        }

        if(associatedBuildingIndex == -1)
        {
            return null;
        }
        else
        {
            return m_playerBuildings[associatedBuildingIndex];
        }
    }

    public bool checkPlayerBuildingAllowed(Player_base player)
    {
        if (player == null)
        {
            return false;
        }
        else
        {
            PlayerBuilding closestBuilding = getBuildingClaimingPosition(player.transform.position);

            if (closestBuilding == null)
            {
                return true; // out of all building range
            }
            else
            {
                if(closestBuilding.isPlayerAllowedBuilding(player.m_gameID))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }

    public void clearAllowedPlayersBuilding(int buildingID)
    {
        if (buildingID > -1 && buildingID < m_playerBuildings.Count)
        {
            m_playerBuildings[buildingID].clearAllowedPlayersBuilding();
        }
    }

    public PlayerBuilding getPlayerBuildingForUID(int buildingUID)
    {
        if (buildingUID > -1 && buildingUID < m_playerBuildings.Count)
        {
            return m_playerBuildings[buildingUID];
        }
        else
        {
            Debug.LogWarning("PlayerBuildingManager: getPlayerBuildingForUID: UID out of range: " + buildingUID);
            return null;
        }
    }
}
