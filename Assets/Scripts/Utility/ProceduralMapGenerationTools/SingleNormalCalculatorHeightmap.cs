using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleNormalCalculatorHeightmap : ProceduralGenThreadingBase
{
    public SingleNormalCalculatorHeightmap(float vertexDistance, ShortCompressedFloat heightmap, Vector2Int startPos, Vector2Int endPos)
    {
        m_VertexDistance = vertexDistance;
        m_heightmap = heightmap;

        m_startPos = startPos;

        m_startPosX = startPos.x;
        m_startPosY = startPos.y;

        m_endPosX = endPos.x;
        m_endPosY = endPos.y;
    }

    public Vector2Int m_startPos { get; private set; }

    private int m_startPosX;
    private int m_startPosY;
    private int m_endPosX;
    private int m_endPosY;
    private float m_VertexDistance;
    private ShortCompressedFloat m_heightmap;

    public Vector3[,] m_result_normals;
    public float[,] m_result_normalsAngle;

    protected override void mainThreadProcedure()
    {
        int deltaX = m_endPosX - m_startPosX;
        int deltaY = m_endPosY - m_startPosY;

        m_result_normals = new Vector3[deltaX, deltaY];
        m_result_normalsAngle = new float[deltaX, deltaY];

        /*
		 * 			C
		 * 	D		i		B
		 * 			A
		 */

        Vector3 i_B = new Vector3(1, 0, 0) * m_VertexDistance;
        Vector3 i_C = new Vector3(0, 0, 1) * m_VertexDistance;

        Vector3 BCNormal;

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
                i_B.y = m_heightmap[i + 1, j] - m_heightmap[i, j];
                i_C.y = m_heightmap[i, (j + 1)] - m_heightmap[i, j];

                BCNormal = Vector3.Cross(i_C, i_B).normalized;

                m_result_normals[i - m_startPosX, j - m_startPosY] = BCNormal;
                m_result_normalsAngle[i - m_startPosX, j - m_startPosY] = Vector3.Angle(Vector3.up, BCNormal);
            }
        }

        setIsDoneState(true);
    }

}
