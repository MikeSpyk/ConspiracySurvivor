using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldRasterField
{
    public WorldRasterField(int startIndexX, int endIndexX, int startIndexY, int endIndexY, FieldResources[] FieldResources, byte[,] textureMapRef, int fieldID)
    {
        m_startIndexX = startIndexX;
        m_endIndexX = endIndexX;
        m_startIndexY = startIndexY;
        m_endIndexY = endIndexY;
        m_textureMapRef = textureMapRef;

        if (FieldResources == null || FieldResources.Length < 1)
        {
            Debug.LogError("WorldRasterField: constructor: FieldResources is empty or null");
        }

        m_FieldResources = FieldResources;
        m_fieldID = fieldID;
    }

    public FieldResources[] m_FieldResources = null;
    private WorldRasterStack m_parent = null;
    private byte[,] m_textureMapRef = null;

    private int m_fieldID = -1;

    private int m_startIndexX;
    private int m_endIndexX;
    private int m_startIndexY;
    private int m_endIndexY;

    public void setParent(WorldRasterStack newParent)
    {
        m_parent = newParent;
    }

    public void addAllResources(int resourceIndex)
    {
        while (m_FieldResources[resourceIndex].need > 0)
        {
            addResource(resourceIndex);
        }
    }

    public bool addResource(int resourceIndex)
    {
        if (m_FieldResources[resourceIndex].need < 1)
        {
            return false;
        }

        Vector2 resourcePosition = m_FieldResources[resourceIndex].addResource();

        m_parent.updateResourceMinTimePassOn(resourceIndex);

        switch (resourceIndex)
        {
            case (int)FieldResources.ResourceType.RandomLootBarrel:
                {
                    WorldManager.singleton.spawnRandomLootContainer(resourcePosition, m_fieldID);
                    break;
                }
            case (int)FieldResources.ResourceType.Tree:
                {
                    WorldManager.singleton.DEBUG_spawnTree(resourcePosition, m_fieldID);
                    break;
                }
            case (int)FieldResources.ResourceType.BerryPlant:
                {
                    WorldManager.singleton.spawnBerryPlant(resourcePosition, m_fieldID);
                    break;
                }
            default:
                {
                    Debug.LogError("unknown resource index: " + resourceIndex);
                    break;
                }
        }

        m_parent.decreaseResourcePassOn(resourceIndex);

        return true;
    }
    public void registerResource(FieldResources.ResourceType resourceType, Vector2 position)
    {
        registerResource((int)resourceType, position);
    }
    public void registerResource(int resourceIndex, Vector2 position)
    {
        if (m_FieldResources[resourceIndex].registerResource(position))
        {
            m_parent.decreaseResourcePassOn(resourceIndex);
        }
    }

    public void removeResource(int resourceIndex, Vector2 position)
    {
        if (m_FieldResources[resourceIndex].removeResource(position))
        {
            m_parent.increaseResourcePassOn(resourceIndex);
        }
        else
        {
            Debug.LogWarning("WorldRasterField: removeResource: resource not found: " + resourceIndex + ", at: " + position.ToString());
        }
    }
    public void removeResource(FieldResources.ResourceType resourceType, Vector2 position)
    {
        removeResource((int)resourceType, position);
    }
}
