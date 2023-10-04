using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class NormalCalculatorHeightmap : ProceduralGenThreadingBase
{
    public NormalCalculatorHeightmap(float vertexDistance, ShortCompressedFloat heightmap, int numberOfThreads)
    {
        m_vertexDistance = vertexDistance;
        m_heightmap = heightmap;
        m_numberOfThreads = numberOfThreads;
    }

    private float m_vertexDistance;
    private ShortCompressedFloat m_heightmap;
    private int m_numberOfThreads;

    public Vector3[,] m_result_normals;
    public float[,] m_result_normalsAngle;

    protected override void mainThreadProcedure()
    {
        if (m_heightmap.GetLength(0) != m_heightmap.GetLength(1))
        {
            Debug.LogError("not tested for non quadratic arrays !");
            setIsDoneState(true);
            return;
        }

        List<SingleNormalCalculatorHeightmap> activeCalculators = new List<SingleNormalCalculatorHeightmap>();

        int threadPosXDelta = m_heightmap.GetLength(0) / m_numberOfThreads;
        int nextThreadStartPosX = 0;

        for (int i = 0; i < m_numberOfThreads - 1; i++)
        {
            SingleNormalCalculatorHeightmap tempCalc = new SingleNormalCalculatorHeightmap(m_vertexDistance, m_heightmap, new Vector2Int(nextThreadStartPosX, 0), new Vector2Int(nextThreadStartPosX + threadPosXDelta, m_heightmap.GetLength(1)));
            tempCalc.start();
            activeCalculators.Add(tempCalc);
            nextThreadStartPosX += threadPosXDelta;
        }

        SingleNormalCalculatorHeightmap tempCalc2 = new SingleNormalCalculatorHeightmap(m_vertexDistance, m_heightmap, new Vector2Int(nextThreadStartPosX, 0), new Vector2Int(m_heightmap.GetLength(0), m_heightmap.GetLength(1)));
        tempCalc2.start();
        activeCalculators.Add(tempCalc2);

        m_result_normals = new Vector3[m_heightmap.GetLength(0), m_heightmap.GetLength(1)];
        m_result_normalsAngle = new float[m_heightmap.GetLength(0), m_heightmap.GetLength(1)];

        bool somethingFinished;
        while (activeCalculators.Count > 0)
        {
            somethingFinished = false;
            for (int i = 0; i < activeCalculators.Count; i++)
            {
                if (activeCalculators[i].isDone)
                {
                    int startPosX = activeCalculators[i].m_startPos.x;
                    int startPosY = activeCalculators[i].m_startPos.y;

                    int loopLengthX = activeCalculators[i].m_result_normals.GetLength(0);
                    int loopLengthY = activeCalculators[i].m_result_normals.GetLength(1);

                    for (int j = 0; j < loopLengthX; j++)
                    {
                        for (int k = 0; k < loopLengthY; k++)
                        {
                            m_result_normals[startPosX + j, startPosY + k] = activeCalculators[i].m_result_normals[j, k];
                            m_result_normalsAngle[startPosX + j, startPosY + k] = activeCalculators[i].m_result_normalsAngle[j, k];
                        }
                    }

                    activeCalculators[i].dispose();
                    activeCalculators.RemoveAt(i);
                    somethingFinished = true;
                }
            }

            if (!somethingFinished)
            {
                Thread.Sleep(10);
            }
        }

        setIsDoneState(true);
    }

}
