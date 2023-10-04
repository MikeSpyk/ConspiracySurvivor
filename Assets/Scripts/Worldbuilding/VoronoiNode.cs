using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoronoiNode
{
    public enum BiomeType { Undefined, Water }

    public VoronoiNode(Vector2 originPoint)
    {
        m_originPoint = originPoint;
    }

    private Vector2 m_originPoint;
    private List<Vector2Int> m_containingPoints = new List<Vector2Int>();
    private List<VoronoiNode> m_neighborNodes = new List<VoronoiNode>();
    private BiomeType m_biomeType = BiomeType.Undefined;
    private bool m_isBorder = false;
    private float m_elevation = 0;
    private bool m_mainLand = false; // has connection to main land
    public int? m_distanceToWater = null; // how many tiles until water in reach
    public int? m_distanceToHill = null; // how many tiles until the hill, this node originates from, is in reach

    public override string ToString()
    {
        return "Pos: " + m_originPoint.ToString() + ", Points count: " + m_containingPoints.Count + ", neighbors count: " + m_neighborNodes.Count + ", border: " + m_isBorder;
    }

    public void addContaningPoint(params Vector2Int[] points)
    {
        m_containingPoints.AddRange(points);
    }

    public void addNeighborNodes(params VoronoiNode[] nodes)
    {
        m_neighborNodes.AddRange(nodes);
    }

    public List<Vector2Int> getContainingPoints()
    {
        return m_containingPoints;
    }

    public void checkIfBorder(int edgeLength)
    {
        int edgeIndex = edgeLength - 1;

        for (int i = 0; i < m_containingPoints.Count; i++)
        {
            if (m_containingPoints[i].x == edgeIndex || m_containingPoints[i].y == edgeIndex || m_containingPoints[i].x == 0 || m_containingPoints[i].y == 0)
            {
                m_isBorder = true;
                m_biomeType = BiomeType.Water;
                return;
            }
        }

        m_isBorder = false;
    }

    public List<VoronoiNode> getNeighborBiomes()
    {
        return m_neighborNodes;
    }

    public Color DEBUG_getColor()
    {
        if (elevation > 0)
        {
            if (isMainLand)
            {
                //return Color.red;
            }
            else
            {
                //return Color.gray;
            }

            return new Color(1 - (elevation / 100), 1 - (elevation / 100), 0);
        }
        else
        {
            return new Color(0, 0, Mathf.Max(1 - (Mathf.Abs(elevation) / 100), 0.5f));
        }
    }

    public bool checkConnectionMainLand()
    {
        return checkConnectionMainLand(new List<VoronoiNode>());
    }

    public bool checkConnectionMainLand(List<VoronoiNode> ignoreNodes)
    {
        if (m_mainLand)
        {
            return true;
        }
        else
        {
            if (elevation > 0)
            {
                ignoreNodes.Add(this);

                for (int i = 0; i < m_neighborNodes.Count; i++)
                {
                    if (!ignoreNodes.Contains(m_neighborNodes[i]))
                    {
                        if (m_neighborNodes[i].checkConnectionMainLand(ignoreNodes))
                        {
                            m_mainLand = true;
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    public int checkDistanceToWater()
    {
        return checkDistanceToWater(new List<VoronoiNode>());
    }
    public int checkDistanceToWater(List<VoronoiNode> ignoreNodes)
    {
        if(m_distanceToWater == null)
        {
            if(elevation < 0)
            {
                m_distanceToWater = 0;
            }
            else
            {
                int minDistance = int.MaxValue;
                ignoreNodes.Add(this);

                for (int i =0; i < m_neighborNodes.Count; i++)
                {
                    if(ignoreNodes.Contains(m_neighborNodes[i]))
                    {
                        continue;
                    }

                    minDistance = Mathf.Min(minDistance, m_neighborNodes[i].checkDistanceToWater(ignoreNodes) + 1);
                }

                if(minDistance == int.MaxValue)
                {
                    //throw new UnityEngine.UnityException("!foundSomething");
                    m_distanceToWater = null;
                }
                else
                {
                    m_distanceToWater = minDistance;
                }
            }
        }

        if(m_distanceToWater != null)
        {
            return (int) m_distanceToWater;
        }
        else
        {
            return int.MaxValue;
        }
    }

    public void updateNeighborsDistanceToWater()
    {
        if(m_distanceToWater == null)
        {
            Debug.LogError("VoronoiNode: updateNeighborsDistanceToWater: distance to water not set yet");
        }
        else
        {
           for(int i = 0; i < m_neighborNodes.Count; i++)
            {
                if(m_neighborNodes[i].m_distanceToWater == null)
                {
                    m_neighborNodes[i].m_distanceToWater = m_distanceToWater + 1;
                }
                else
                {
                    m_neighborNodes[i].m_distanceToWater = Mathf.Min((int)m_neighborNodes[i].m_distanceToWater, (int)m_distanceToWater + 1);
                }
            }
        }
    }

    public bool isBorder
    {
        get
        {
            return m_isBorder;
        }
    }

    public Vector2 origin
    {
        get
        {
            return m_originPoint;
        }
    }

    public float elevation
    {
        set
        {
            m_elevation = value;
        }
        get
        {
            return m_elevation;
        }
    }

    public bool isMainLand
    {
        get
        {
            return m_mainLand;
        }
        set
        {
            m_mainLand = value;
        }
    }

    public static Color elevationToColor(float elevation)
    {
        if (elevation > 0)
        {
            return new Color(1 - (elevation / 100), 1 - (elevation / 100), 0);
        }
        else
        {
            return new Color(0, 0, Mathf.Max(1 - (Mathf.Abs(elevation) / 100), 0.5f));
        }
    }
}
