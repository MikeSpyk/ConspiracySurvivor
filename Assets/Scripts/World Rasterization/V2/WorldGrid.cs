using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WorldGrid
{
    public enum SpecialFieldUIDs { OutOfBound = -1 }

    private WorldGridField[,] m_fields;
    private WorldGridField m_outOfGridField; // entity that are not within the grid
    private Dictionary<int, WorldGridField> m_fieldID_field = new Dictionary<int, WorldGridField>();
    private float m_sizeX;
    private float m_sizeY;
    private float m_offsetX;
    private float m_offsetY;
    private float m_fieldSize;
    private List<WorldGridField> m_visibleEntityFields = new List<WorldGridField>();

    public List<WorldGridField> visibleEntityFields { get { return m_visibleEntityFields; } }

    public event EventHandler<WorldGridFieldArgs> GridFieldNoMoreEntityViewersEvent;
    public event EventHandler<WorldGridFieldIntegerArgs> GridFieldEntityViewerLeftEvent;

    public WorldGrid(float sizeX, float sizeY, float fieldSize, float offsetX = 0, float offsetY = 0)
    {
        m_sizeX = sizeX;
        m_sizeY = sizeY;
        m_offsetX = offsetX;
        m_offsetY = offsetY;
        m_fieldSize = fieldSize;

        m_fields = new WorldGridField[(int)(sizeX / fieldSize) + 1, (int)(sizeY / fieldSize) + 1];

        m_outOfGridField = new WorldGridField(new Vector2(float.MinValue, float.MinValue), new Vector2(float.MaxValue, float.MaxValue), -1);
        m_fieldID_field.Add(-1, m_outOfGridField);

        createGrid();
    }

    public void updateVisibleGridSections(List<Vector3> viewersPositions, List<int> viewersGameID, List<float> viewDistancesClose)
    {
        bool tempSeesOutOfRangeClose;
        bool outOfGridFieldHadViewersClose = m_outOfGridField.entityViewersGameID.Count > 0;

        for (int k = 0; k < viewersPositions.Count; k++)
        {
            Vector2Int startPos = new Vector2Int((int)((viewersPositions[k].x - m_offsetX - viewDistancesClose[k]) / m_fieldSize),
                                                    (int)((viewersPositions[k].z - m_offsetY - viewDistancesClose[k]) / m_fieldSize));

            Vector2Int endPos = new Vector2Int(((int)((viewersPositions[k].x - m_offsetX + viewDistancesClose[k]) / m_fieldSize)) + 1,
                                                    ((int)((viewersPositions[k].z - m_offsetY + viewDistancesClose[k]) / m_fieldSize)) + 1);

            tempSeesOutOfRangeClose = false;

            for (int i = startPos.x; i <= endPos.x; i++)
            {
                for (int j = startPos.y; j <= endPos.y; j++)
                {
                    if (i < 0 || i >= m_fields.GetLength(0) || j < 0 || j >= m_fields.GetLength(1))
                    {
                        if (!m_outOfGridField.entityViewersGameID.Contains(viewersGameID[k]))
                        {
                            m_outOfGridField.entityViewersGameID.Add(viewersGameID[k]);

                            if (m_outOfGridField.entityViewersGameID.Count == 1)
                            {
                                m_visibleEntityFields.Add(m_outOfGridField);
                            }
                        }

                        tempSeesOutOfRangeClose = true;
                    }
                    else
                    {
                        if (m_fields[i, j].isInRadius(new Vector2(viewersPositions[k].x, viewersPositions[k].z), viewDistancesClose[k]))
                        {
                            if (!m_fields[i, j].entityViewersGameID.Contains(viewersGameID[k]))
                            {
                                m_fields[i, j].entityViewersGameID.Add(viewersGameID[k]);

                                if (m_fields[i, j].entityViewersGameID.Count == 1)
                                {
                                    m_visibleEntityFields.Add(m_fields[i, j]);
                                }
                            }
                        }
                    }
                }
            }

            if (!tempSeesOutOfRangeClose)
            {
                if (m_outOfGridField.entityViewersGameID.Contains(viewersGameID[k]))
                {
                    onGridFieldEntityViewerLeft(m_outOfGridField, viewersGameID[k]);
                    m_outOfGridField.entityViewersGameID.Remove(viewersGameID[k]);
                }
            }
        }

        if (outOfGridFieldHadViewersClose && m_outOfGridField.entityViewersGameID.Count == 0)
        {
            m_visibleEntityFields.Remove(m_outOfGridField);
            onGridFieldNoMoreEntityViewers(m_outOfGridField);
        }

        Dictionary<int, Vector2> playerID_position = new Dictionary<int, Vector2>();
        Dictionary<int, float> playerID_viewDistance = new Dictionary<int, float>();

        for (int i = 0; i < viewersPositions.Count; i++)
        {
            playerID_position.Add(viewersGameID[i], new Vector2(viewersPositions[i].x, viewersPositions[i].z));
            playerID_viewDistance.Add(viewersGameID[i], viewDistancesClose[i]);
        }

        for (int i = 0; i < m_visibleEntityFields.Count; i++)
        {
            for (int j = 0; j < m_visibleEntityFields[i].entityViewersGameID.Count; j++)
            {
                if (playerID_position.ContainsKey(m_visibleEntityFields[i].entityViewersGameID[j]))
                {
                    if (!m_visibleEntityFields[i].isInRadius(playerID_position[m_visibleEntityFields[i].entityViewersGameID[j]], playerID_viewDistance[m_visibleEntityFields[i].entityViewersGameID[j]]))
                    {
                        onGridFieldEntityViewerLeft(m_visibleEntityFields[i], m_visibleEntityFields[i].entityViewersGameID[j]);
                        m_visibleEntityFields[i].entityViewersGameID.RemoveAt(j);
                        j--;

                        if (m_visibleEntityFields[i].entityViewersGameID.Count == 0)
                        {
                            onGridFieldNoMoreEntityViewers(m_visibleEntityFields[i]);
                            m_visibleEntityFields.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
                else // player no longer active
                {
                    onGridFieldEntityViewerLeft(m_visibleEntityFields[i], m_visibleEntityFields[i].entityViewersGameID[j]);
                    m_visibleEntityFields[i].entityViewersGameID.RemoveAt(j);
                    j--;

                    if (m_visibleEntityFields[i].entityViewersGameID.Count == 0)
                    {
                        onGridFieldNoMoreEntityViewers(m_visibleEntityFields[i]);
                        m_visibleEntityFields.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
        }
    }

    public WorldGridField getField(int fieldUID)
    {
        if (m_fieldID_field.ContainsKey(fieldUID))
        {
            return m_fieldID_field[fieldUID];
        }
        else
        {
            Debug.LogError("WorldGrid: getField: grid-ID \"" + fieldUID + "\" not found !");
            return null;
        }
    }

    public WorldGridField getFieldForPosition(Vector2 position)
    {
        int indexX = (int)((position.x - m_offsetX) / m_fieldSize);
        int indexY = (int)((position.y - m_offsetY) / m_fieldSize);

        if (indexX < 0 || indexX >= m_fields.GetLength(0) || indexY < 0 || indexY >= m_fields.GetLength(1))
        {
            Debug.Log("WorldGrid: getFieldForPosition: out of grid");
            return m_outOfGridField;
        }
        else
        {
            return m_fields[indexX, indexY];
        }
    }

    public void DEBUG_showVisibleEntityFields()
    {
        for (int i = 0; i < m_visibleEntityFields.Count; i++)
        {
            m_visibleEntityFields[i].DEBUG_drawRectangle(Color.red, false, 0, 0);
        }
    }

    public void DEBUG_showGrid()
    {
        for (int i = 0; i < m_fields.GetLength(0); i++)
        {
            for (int j = 0; j < m_fields.GetLength(1); j++)
            {
                m_fields[i, j].DEBUG_drawRectangle();
            }
        }
    }

    private void onGridFieldNoMoreEntityViewers(WorldGridField field)
    {
        EventHandler<WorldGridFieldArgs> handler = GridFieldNoMoreEntityViewersEvent;

        if (handler != null)
        {
            WorldGridFieldArgs args = new WorldGridFieldArgs();
            args.worldGridField = field;

            GridFieldNoMoreEntityViewersEvent(this, args);
        }
    }

    private void onGridFieldEntityViewerLeft(WorldGridField field, int playerGameID)
    {
        EventHandler<WorldGridFieldIntegerArgs> handler = GridFieldEntityViewerLeftEvent;

        if (handler != null)
        {
            WorldGridFieldIntegerArgs args = new WorldGridFieldIntegerArgs();
            args.worldGridField = field;
            args.integer = playerGameID;

            GridFieldEntityViewerLeftEvent(this, args);
        }
    }

    private void createGrid()
    {
        int counter = 0;

        for (int i = 0; i < m_fields.GetLength(0); i++)
        {
            for (int j = 0; j < m_fields.GetLength(1); j++)
            {
                m_fields[i, j] = new WorldGridField(new Vector2(i * m_fieldSize + m_offsetX, j * m_fieldSize + m_offsetY), new Vector2(i * m_fieldSize + m_fieldSize + m_offsetX, j * m_fieldSize + m_fieldSize + m_offsetY), counter);
                m_fieldID_field.Add(counter, m_fields[i, j]);
                counter++;
            }
        }

        List<WorldGridField> tempNeighbors;

        for (int i = 0; i < m_fields.GetLength(0); i++)
        {
            for (int j = 0; j < m_fields.GetLength(1); j++)
            {
                tempNeighbors = new List<WorldGridField>();

                if (i == 0 || i == (m_fields.GetLength(0) - 1) || j == 0 || j == (m_fields.GetLength(1) - 1))
                {
                    tempNeighbors.Add(m_outOfGridField);
                }

                for (int k = -1; k < 2; k++)
                {
                    for (int l = -1; l < 2; l++)
                    {
                        if (k == 0 && l == 0)
                        {
                            continue;
                        }

                        if (i + k > -1 && i + k < m_fields.GetLength(0) && j + l > -1 && j + l < m_fields.GetLength(1))
                        {
                            tempNeighbors.Add(m_fields[i + k, j + l]);
                        }
                    }
                }

                m_fields[i, j].setNeighborFields(tempNeighbors.ToArray());
            }
        }

        tempNeighbors = new List<WorldGridField>();

        for (int i = 0; i < m_fields.GetLength(0); i++)
        {
            tempNeighbors.Add(m_fields[i, 0]);
            tempNeighbors.Add(m_fields[i, m_fields.GetLength(1) - 1]);
        }

        for (int i = 0; i < m_fields.GetLength(1); i++)
        {
            tempNeighbors.Add(m_fields[0, i]);
            tempNeighbors.Add(m_fields[m_fields.GetLength(0) - 1, i]);
        }

        m_outOfGridField.setNeighborFields(tempNeighbors.ToArray());
    }

}
