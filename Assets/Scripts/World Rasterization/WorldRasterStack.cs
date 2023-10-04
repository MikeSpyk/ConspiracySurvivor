using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldRasterStack
{
    public FieldResourcesStack[] m_fieldResourcesStack = null; // simpler version of all field resources below summed up
    private WorldRasterField[] m_worldRasterFields = null; // eighter this or m_worldRasterStacks will be populated
    private WorldRasterStack[] m_worldRasterStacks = null; // eighter this or m_worldRasterFields will be populated
    private WorldRasterStack m_parent = null;

    public void initialise(FieldResourcesStack[] fieldResourcesStack, WorldRasterField[] worldRasterFields)
    {
        if (fieldResourcesStack == null || fieldResourcesStack.Length < 1)
        {
            Debug.LogError("WorldRasterStack: constructor: fieldResourcesStack is null or empty");
        }

        if (worldRasterFields == null || worldRasterFields.Length < 1)
        {
            Debug.LogError("WorldRasterStack: constructor: worldRasterFields is null or empty");
        }

        if (worldRasterFields != null && worldRasterFields.Length > 4)
        {
            Debug.LogError("WorldRasterStack: constructor: worldRasterFields is bigger than 4");
        }

        m_fieldResourcesStack = fieldResourcesStack;
        m_worldRasterFields = worldRasterFields;
    }

    public void initialise(FieldResourcesStack[] fieldResourcesStack, WorldRasterStack[] worldRasterStacks)
    {
        if (fieldResourcesStack == null || fieldResourcesStack.Length < 1)
        {
            Debug.LogError("WorldRasterStack: constructor: fieldResourcesStack is null or empty");
        }

        if (worldRasterStacks == null || worldRasterStacks.Length < 1)
        {
            Debug.LogError("WorldRasterStack: constructor: worldRasterStacks is null or empty");
        }

        if (worldRasterStacks != null && worldRasterStacks.Length > 4)
        {
            Debug.LogError("WorldRasterStack: constructor: worldRasterStacks is bigger than 4");
        }

        m_fieldResourcesStack = fieldResourcesStack;
        m_worldRasterStacks = worldRasterStacks;
    }

    public void setParent(WorldRasterStack newParent)
    {
        m_parent = newParent;
    }

    public void addAllResources(FieldResources.ResourceType type)
    {
        if (m_worldRasterFields == null)
        {
            for (int i = 0; i < m_worldRasterStacks.Length; i++)
            {
                m_worldRasterStacks[i].addAllResources(type);
            }
        }
        else // children are stacks
        {
            for (int i = 0; i < m_worldRasterFields.Length; i++)
            {
                m_worldRasterFields[i].addAllResources((int)type);
            }
        }
    }

    public bool addResourceToMostNeeded(FieldResources.ResourceType type)
    {
        return addResourceToMostNeeded((int)type);
    }
    public bool addResourceToMostNeeded(int resourceIndex)
    {
        int bestIndex = -1;
        float lowestTime = float.MaxValue;

        if (m_worldRasterFields == null)
        {
            for (int i = 0; i < m_worldRasterStacks.Length; i++)
            {
                if (
                    m_worldRasterStacks[i].m_fieldResourcesStack[resourceIndex].m_lastTimeAdded < lowestTime &&
                    m_worldRasterStacks[i].m_fieldResourcesStack[resourceIndex].m_need > 0
                    )
                {
                    bestIndex = i;
                    lowestTime = m_worldRasterStacks[i].m_fieldResourcesStack[resourceIndex].m_lastTimeAdded;
                }
            }

            if (bestIndex == -1)
            {
                //Debug.Log("WorldRasterStack: no more resource of index \"" + resourceIndex + "\" needed");
                return false;
            }
            else
            {
                return m_worldRasterStacks[bestIndex].addResourceToMostNeeded(resourceIndex);
            }
        }
        else // children are stacks
        {
            for (int i = 0; i < m_worldRasterFields.Length; i++)
            {
                if (
                    m_worldRasterFields[i].m_FieldResources[resourceIndex].lastTimeAdded < lowestTime &&
                    m_worldRasterFields[i].m_FieldResources[resourceIndex].need > 0
                    )
                {
                    bestIndex = i;
                    lowestTime = m_worldRasterFields[i].m_FieldResources[resourceIndex].lastTimeAdded;
                }
            }

            if (bestIndex == -1)
            {
                Debug.Log("WorldRasterStack: no more resource of index \"" + resourceIndex + "\" needed");
                return false;
            }
            else
            {
                return m_worldRasterFields[bestIndex].addResource(resourceIndex);
            }
        }
    }

    public void decreaseResourcePassOn(int resourceIndex)
    {
        m_fieldResourcesStack[resourceIndex].m_need--;

        if (m_parent == null) // reached last node
        {
            //Debug.Log("WorldRasterStack: decreased resource (" + resourceIndex + ") need to " + m_fieldResourcesStack[resourceIndex].m_need);
        }
        else
        {
            m_parent.decreaseResourcePassOn(resourceIndex);
        }
    }

    public void increaseResourcePassOn(int resourceIndex)
    {
        m_fieldResourcesStack[resourceIndex].m_need++;

        if (m_parent == null) // reached last node
        {
            //Debug.Log("WorldRasterStack: decreased resource (" + resourceIndex + ") need to " + m_fieldResourcesStack[resourceIndex].m_need);
        }
        else
        {
            m_parent.increaseResourcePassOn(resourceIndex);
        }
    }

    public void updateResourceMinTimePassOn(int resourceIndex)
    {
        float minTime = Time.time;

        if (m_worldRasterFields == null)
        {
            for (int i = 0; i < m_worldRasterStacks.Length; i++)
            {
                if (m_worldRasterStacks[i].m_fieldResourcesStack[resourceIndex].m_need > 0)
                {
                    minTime = Mathf.Min(minTime, m_worldRasterStacks[i].m_fieldResourcesStack[resourceIndex].m_lastTimeAdded);
                }
            }
        }
        else
        {
            for (int i = 0; i < m_worldRasterFields.Length; i++)
            {
                if (m_worldRasterFields[i].m_FieldResources[resourceIndex].need > 0)
                {
                    minTime = Mathf.Min(minTime, m_worldRasterFields[i].m_FieldResources[resourceIndex].lastTimeAdded);
                }
            }
        }

        m_fieldResourcesStack[resourceIndex].m_lastTimeAdded = minTime;

        if (m_parent != null)
        {
            m_parent.updateResourceMinTimePassOn(resourceIndex);
        }
    }
}
