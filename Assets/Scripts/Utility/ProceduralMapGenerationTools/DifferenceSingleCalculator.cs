using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DifferenceSingleCalculator : ProceduralGenThreadingBase
{

    public DifferenceSingleCalculator(int startPosX, int startPosY, int endPosX, int endPosY, ShortCompressedFloat heightmap, float minHeight)
    {
        m_startPosX = startPosX;
        m_startPosY = startPosY;
        m_endPosX = endPosX;
        m_endPosY = endPosY;
        m_heightmap = heightmap;
        m_minHeight = minHeight;
    }

    private int m_startPosX;
    private int m_startPosY;
    private int m_endPosX;
    private int m_endPosY;
    private float m_minHeight;
    private ShortCompressedFloat m_heightmap;
    public MinMaxDiff[,] m_result_MinMaxDiff;

    protected override void mainThreadProcedure()
    {
        if (m_heightmap.GetLength(0) != m_heightmap.GetLength(1))
        {
            Debug.LogError("non quadratic arrays are not tested and may not work !");
        }

        int deltaX = m_endPosX - m_startPosX;
        int deltaY = m_endPosY - m_startPosY;

        m_result_MinMaxDiff = new MinMaxDiff[deltaX, deltaY];

        int loopEndX = m_endPosX;
        int loopEndY = m_endPosY;

        if (m_endPosX >= m_heightmap.GetLength(0))
        {
            loopEndX = m_heightmap.GetLength(0) - 1;
        }
        if (m_endPosY >= m_heightmap.GetLength(1))
        {
            loopEndY = m_heightmap.GetLength(1) - 1;
        }

        for (int i = m_startPosX; i < loopEndX; i++)
        {
            for (int j = m_startPosY; j < loopEndY; j++)
            {
                if (m_heightmap[m_startPosX + i, j + m_startPosY] < m_minHeight ||
                    m_heightmap[m_startPosX + i + 1, j + m_startPosY] < m_minHeight ||
                    m_heightmap[m_startPosX + i, j + m_startPosY] < m_minHeight ||
                    m_heightmap[m_startPosX + i, j + 1 + m_startPosY] < m_minHeight
                    )
                {
                    m_result_MinMaxDiff[i - m_startPosX, j - m_startPosY].m_min = 0;
                    m_result_MinMaxDiff[i - m_startPosX, j - m_startPosY].m_max = float.MaxValue;
                    m_result_MinMaxDiff[i - m_startPosX, j - m_startPosY].m_diff = float.MaxValue;
                }
                else
                {
                    m_result_MinMaxDiff[i - m_startPosX, j - m_startPosY].m_min = Mathf.Min(
                                                                                            m_heightmap[m_startPosX + i, j + m_startPosY],
                                                                                            m_heightmap[m_startPosX + i + 1, j + m_startPosY],
                                                                                            m_heightmap[m_startPosX + i, j + 1 + m_startPosY]
                                                                                            );

                    m_result_MinMaxDiff[i - m_startPosX, j - m_startPosY].m_max = Mathf.Max(
                                                                                            m_heightmap[m_startPosX + i, j + m_startPosY],
                                                                                            m_heightmap[m_startPosX + i + 1, j + m_startPosY],
                                                                                            m_heightmap[m_startPosX + i, j + 1 + m_startPosY]
                                                                                            );

                    m_result_MinMaxDiff[i - m_startPosX, j - m_startPosY].m_diff = m_result_MinMaxDiff[i - m_startPosX, j - m_startPosY].m_max - m_result_MinMaxDiff[i - m_startPosX, j - m_startPosY].m_min;
                }
            }
        }

        setIsDoneState(true);
    }
}
