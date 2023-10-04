using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaxDownscaleSingleCalculator : ProceduralGenThreadingBase
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="startPosX">startpos in the resulting array</param>
    /// <param name="startPosY">startpos in the resulting array</param>
    /// <param name="endPosX">endpos in the resulting array</param>
    /// <param name="endPosY">endpos in the resulting array</param>
    /// <param name="diffMap"></param>
    /// <param name="diffMapEdgeLength"></param>
    public MaxDownscaleSingleCalculator(int startPosX, int startPosY, int endPosX, int endPosY, MinMaxDiff[,] diffMap, int diffMapEdgeLength)
    {
        if(diffMapEdgeLength % 2 != 0)
        {
            Debug.LogError("MaxDownscaleSingleCalculator: Edge-lenght not divisble by 2");
        }

        m_startPosX = startPosX;
        m_startPosY = startPosY;
        m_endPosX = endPosX;
        m_endPosY = endPosY;
        m_diffMap = diffMap;

        m_resultMaxArrayLength = diffMapEdgeLength / 2;
    }

    private int m_startPosX;
    private int m_startPosY;
    private int m_endPosX;
    private int m_endPosY;
    private int m_resultMaxArrayLength;
    private MinMaxDiff[,] m_diffMap;
    public MinMaxDiff[,] m_result_MinMaxDiff;

    protected override void mainThreadProcedure()
    {
        m_result_MinMaxDiff = new MinMaxDiff[m_endPosX- m_startPosX, m_endPosY - m_startPosY];

        if (m_endPosX >= m_resultMaxArrayLength)
        {
            m_endPosX = m_resultMaxArrayLength - 1;
        }

        if (m_endPosY >= m_resultMaxArrayLength)
        {
            m_endPosY = m_resultMaxArrayLength - 1;
        }

        int deltaX = m_endPosX - m_startPosX;
        int deltaY = m_endPosY - m_startPosY;

        for (int i = 0; i < deltaX; i++)
        {
            for (int j = 0; j < deltaY; j++)
            {
                m_result_MinMaxDiff[i, j].m_max = Mathf.Max(
                                                m_diffMap[(m_startPosX + i * 2),    (m_startPosY + j * 2) ].m_max,
                                                m_diffMap[(m_startPosX + i * 2 + 1),(m_startPosY + j * 2) ].m_max,
                                                m_diffMap[(m_startPosX + i * 2),    (m_startPosY + j * 2 + 1)].m_max,
                                                m_diffMap[(m_startPosX + i * 2 + 1),(m_startPosY + j * 2 + 1)].m_max
                                            );

                m_result_MinMaxDiff[i, j].m_min = Mathf.Min(
                                                m_diffMap[(m_startPosX + i * 2), (m_startPosY + j * 2)].m_min,
                                                m_diffMap[(m_startPosX + i * 2 + 1), (m_startPosY + j * 2)].m_min,
                                                m_diffMap[(m_startPosX + i * 2), (m_startPosY + j * 2 + 1)].m_min,
                                                m_diffMap[(m_startPosX + i * 2 + 1), (m_startPosY + j * 2 + 1)].m_min
                                            );

                m_result_MinMaxDiff[i, j].m_diff = m_result_MinMaxDiff[i, j].m_max - m_result_MinMaxDiff[i, j].m_min;
            }
        }

        setIsDoneState(true);
    }
}
