using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGridField
{
    private Vector2 m_startPos;
    private Vector2 m_endPos;
    private int m_uid;
    private bool m_entitiesLoaded;
    private Vector3[] m_corners = new Vector3[4];
    private List<Entity_base> m_activeEntities = new List<Entity_base>();
    private List<DataEntity_Base> m_unloadedEntities = new List<DataEntity_Base>();
    private WorldGridField[] m_neighborFields = null;
    private List<int> m_entityViewersGameID = new List<int>();

    public bool entitiesLoaded { get { return m_entitiesLoaded; } }
    public int uid { get { return m_uid; } }
    public WorldGridField[] neighborFields { get { return m_neighborFields; } }
    public Vector2 startPos { get { return m_startPos; } }
    public List<Entity_base> activeEntities { get { return m_activeEntities; } }
    public List<DataEntity_Base> unloadedEntities { get { return m_unloadedEntities; } }
    public List<int> entityViewersGameID { get { return m_entityViewersGameID; } }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startPos">lower-left corner of the field</param>
    /// <param name="endPos">upper-right corner of the field</param>
    public WorldGridField(Vector2 startPos, Vector2 endPos, int uid)
    {
        m_startPos = startPos;
        m_endPos = endPos;
        m_uid = uid;

        m_entitiesLoaded = false;

        m_corners[0] = new Vector3(startPos.x, 0, startPos.y);
        m_corners[1] = new Vector3(endPos.x, 0, startPos.y);
        m_corners[2] = new Vector3(endPos.x, 0, endPos.y);
        m_corners[3] = new Vector3(startPos.x, 0, endPos.y);
    }
    public void DEBUG_drawRectangle()
    {
        DEBUG_drawRectangle(Color.red, true);
    }
    public void DEBUG_drawRectangle(Color color, bool showNeighbors = false, float heightOffset = 0, float time = 10f)
    {
        Debug.DrawLine(new Vector3(m_corners[0].x, 100f + heightOffset, m_corners[0].z), new Vector3(m_corners[1].x, 100f + heightOffset, m_corners[1].z), color, time);
        Debug.DrawLine(new Vector3(m_corners[1].x, 100f + heightOffset, m_corners[1].z), new Vector3(m_corners[2].x, 100f + heightOffset, m_corners[2].z), color, time);
        Debug.DrawLine(new Vector3(m_corners[2].x, 100f + heightOffset, m_corners[2].z), new Vector3(m_corners[3].x, 100f + heightOffset, m_corners[3].z), color, time);
        Debug.DrawLine(new Vector3(m_corners[3].x, 100f + heightOffset, m_corners[3].z), new Vector3(m_corners[0].x, 100f + heightOffset, m_corners[0].z), color, time);

        if (showNeighbors)
        {
            for (int i = 0; i < m_neighborFields.Length; i++)
            {
                m_neighborFields[i].DEBUG_drawRectangle(Color.green, false, -10f);
            }
        }
    }

    public void setNeighborFields(params WorldGridField[] fields)
    {
        if (m_neighborFields != null)
        {
            Debug.LogError("WorldGridField: setNeighborFields: tryed to reset neighbors. neighbors can only get set once !");
        }

        m_neighborFields = new WorldGridField[fields.Length];

        for (int i = 0; i < fields.Length; i++)
        {
            m_neighborFields[i] = fields[i];
        }
    }

    public bool isWithin(Vector2 position)
    {
        if (position.x >= m_startPos.x && position.x <= m_endPos.x && position.y >= m_startPos.y && position.y <= m_endPos.y)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// are parts of this rectangle visible by a viewer ?
    /// </summary>
    /// <param name="viewer"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    public bool isInRadius(Vector2 viewer, float radius)
    {
        if (isWithin(viewer))
        {
            return true;
        }
        else
        {
            return AreaTools.distanceToAreaEdge(new Vector3(viewer.x, 0, viewer.y), m_corners) < radius;
        }
    }

    public void load()
    {
        m_entitiesLoaded = true;
    }

    public void unload()
    {
        m_entitiesLoaded = false;
    }
}
