using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldResources
{
    public enum ResourceType { Undefined = -1, Tree = 0, RandomLootBarrel = 1, BerryPlant = 2 }

    private const float LOOTBARREL_MIN_DISTANCE = 100f;
    private const float TREE_MIN_DISTANCE = 10f;
    private const float TREE_OCTAVE_FREQENCY = 1.9f;
    private const float TREE_OCTAVE_THRESHOLD = 0.85f;
    private const float BERRY_PLANT_MIN_DISTANCE = 150f;

    private ResourceType m_type = ResourceType.Undefined;
    private int m_capacity = 0;
    private float m_lastTimeAdded = 0;
    private System.Random m_random = new System.Random();
    private List<Vector2> m_activeChildrenPositions = new List<Vector2>();
    private List<Vector2> m_possiblePositions = new List<Vector2>();

    public ResourceType type { get { return m_type; } }
    public int capacity { get { return m_capacity; } }
    public int have { get { return m_activeChildrenPositions.Count; } }
    public int need { get { return m_possiblePositions.Count; } }
    public float lastTimeAdded { get { return m_lastTimeAdded; } }

    public Vector2 addResource()
    {
        if (m_type == ResourceType.Undefined)
        {
            Debug.LogError("FieldResources: addResource: can't add resource to undefined type !");
            return Vector2.zero;
        }

        if (need < 1)
        {
            Debug.LogError("FieldResources: addResource: can't add resource. there is no need !");
            return Vector2.zero;
        }

        m_lastTimeAdded = Time.time;

        int randomValue = m_random.Next(0, m_possiblePositions.Count - 1);

        Vector2 returnValue = m_possiblePositions[randomValue];

        m_activeChildrenPositions.Add(m_possiblePositions[randomValue]);
        m_possiblePositions.RemoveAt(randomValue);

        return returnValue;
    }

    public bool registerResource(Vector2 position)
    {
        int index = m_possiblePositions.IndexOf(position);

        if (index == -1)
        {
            Debug.LogWarning("FieldResources: registerResource: position not available: " + position.ToString());
            return false;
        }
        else
        {
            m_activeChildrenPositions.Add(m_possiblePositions[index]);
            m_possiblePositions.RemoveAt(index);
            return true;
        }
    }

    public bool removeResource(Vector2 position)
    {
        int index = m_activeChildrenPositions.IndexOf(position);

        if (index > -1)
        {
            m_possiblePositions.Add(m_activeChildrenPositions[index]);
            m_activeChildrenPositions.RemoveAt(index);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void computeCapacity(ResourceType inputType, byte[,] textureMap, int startPosX, int startPosY, int endPosX, int endPosY)
    {
        if (m_type == ResourceType.Undefined)
        {
            m_type = inputType;

            switch (m_type)
            {
                case ResourceType.Tree:
                    {
                        float vertDistance = WorldManager.singleton.getDefaultSubmeshVertDistance();

                        for (int i = startPosX; i < endPosX; i++)
                        {
                            for (int j = startPosY; j < endPosY; j++)
                            {
                                float randomValue = Mathf.PerlinNoise(startPosX * 1.1f + i * TREE_OCTAVE_FREQENCY, startPosY * 1.2f + j * TREE_OCTAVE_FREQENCY);

                                if (textureMap[i, j] == (byte)WorldManager.vertexMapTextureNames.forest && randomValue > TREE_OCTAVE_THRESHOLD)
                                {
                                    Vector2 position = new Vector2(i * vertDistance, j * vertDistance);

                                    for (int k = 0; k < m_possiblePositions.Count; k++)
                                    {
                                        if (Vector2.Distance(position, m_possiblePositions[k]) < TREE_MIN_DISTANCE)
                                        {
                                            goto TREE_LOOP_END_1;
                                        }
                                    }

                                    m_possiblePositions.Add(position);
                                }
                            TREE_LOOP_END_1:;
                            }
                        }
                        break;
                    }
                case ResourceType.RandomLootBarrel:
                    {
                        float vertDistance = WorldManager.singleton.getDefaultSubmeshVertDistance();

                        for (int i = startPosX; i < endPosX; i++)
                        {
                            for (int j = startPosY; j < endPosY; j++)
                            {
                                if (
                                        textureMap[i, j] != (byte)WorldManager.vertexMapTextureNames.rock &&
                                        textureMap[i, j] != (byte)WorldManager.vertexMapTextureNames.mountain_steep &&
                                        textureMap[i, j] != (byte)WorldManager.vertexMapTextureNames.underwater
                                    )
                                {
                                    Vector2 position = new Vector2(i * vertDistance, j * vertDistance);

                                    for (int k = 0; k < m_possiblePositions.Count; k++)
                                    {
                                        if (Vector2.Distance(position, m_possiblePositions[k]) < LOOTBARREL_MIN_DISTANCE)
                                        {
                                            goto LOOT_BARREL_LOOP_END_1;
                                        }
                                    }

                                    m_possiblePositions.Add(position);
                                }

                            LOOT_BARREL_LOOP_END_1:;
                            }
                        }

                        break;
                    }
                case ResourceType.BerryPlant:
                    {
                        float vertDistance = WorldManager.singleton.getDefaultSubmeshVertDistance();

                        for (int i = startPosX; i < endPosX; i += 1)
                        {
                            for (int j = startPosY; j < endPosY; j += 1)
                            {
                                if (
                                        textureMap[i, j] != (byte)WorldManager.vertexMapTextureNames.rock &&
                                        textureMap[i, j] != (byte)WorldManager.vertexMapTextureNames.mountain_steep &&
                                        textureMap[i, j] != (byte)WorldManager.vertexMapTextureNames.underwater &&
                                        textureMap[i, j] != (byte)WorldManager.vertexMapTextureNames.beachsand
                                    )
                                {
                                    float probability = RandomValuesSeed.getRandomValueSeed((float)i, (float)j);

                                    switch (textureMap[i, j])
                                    {
                                        case (byte)WorldManager.vertexMapTextureNames.forest:
                                            {
                                                probability += 0.05f;
                                                break;
                                            }
                                        case (byte)WorldManager.vertexMapTextureNames.beachGrassTransition:
                                            {
                                                probability -= 0.2f;
                                                break;
                                            }
                                        case (byte)WorldManager.vertexMapTextureNames.deadGrass:
                                            {
                                                probability -= 0.2f;
                                                break;
                                            }
                                        case (byte)WorldManager.vertexMapTextureNames.dirt:
                                            {
                                                probability -= 0.5f;
                                                break;
                                            }
                                        case (byte)WorldManager.vertexMapTextureNames.snow:
                                            {
                                                probability -= 0.6f;
                                                break;
                                            }
                                        default:
                                            {
                                                break;
                                            }
                                    }

                                    if (probability > 0.9911f)
                                    {
                                        if (RandomValuesSeed.getRandomBoolProbability(j - i, 40))
                                        {
                                            Vector2 position = new Vector2(i * vertDistance, j * vertDistance);
                                            m_possiblePositions.Add(position);
                                        }
                                    }
                                }

                            }
                        }

                        break;
                    }
                default:
                    {
                        Debug.LogError("FieldResources: computeCapacity: unknown resource type \"" + type.ToString() + "\"");
                        break;
                    }
            }

            m_capacity = need;
        }
        else
        {
            Debug.LogError("FieldResources: computeCapacity: cannot recompute !");
        }
    }
}
