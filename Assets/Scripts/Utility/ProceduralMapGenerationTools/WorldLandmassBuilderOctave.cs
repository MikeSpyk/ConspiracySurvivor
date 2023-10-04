using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldLandmassBuilderOctave : ProceduralGenThreadingBase
{
    public WorldLandmassBuilderOctave(WorldLandmassBuilderOctaveProperties properties, int sizeEdge)
    {
        m_octaveProperties = properties;
        m_sizeEdge = sizeEdge;
    }

    private WorldLandmassBuilderOctaveProperties m_octaveProperties;
    private int m_sizeEdge;
    private AnimationCurve m_heightCurve;

    protected override void mainThreadProcedure()
    {
        float[,] downScaledHeightmap;
        m_heightCurve = new AnimationCurve(m_octaveProperties.curveKeyframes);

        // downscaling

        bool dividingSuccess = true;
        int dividedEdgeLength = m_sizeEdge;
        for (int i = 0; i < m_octaveProperties.scalingCount; i++)
        {
            if (dividedEdgeLength % 2 != 0)
            {
                dividingSuccess = false;
                break;
            }
            dividedEdgeLength /= 2;
        }

        if (dividingSuccess == false)
        {
            Debug.LogWarning("WorldLandmassBuilderOctave: Size is not divisible by 2 for " + m_octaveProperties.scalingCount + " times.");
            dividedEdgeLength = m_sizeEdge;
        }

        downScaledHeightmap = new float[dividedEdgeLength, dividedEdgeLength];

        // create octave
        for (int i = 0; i < downScaledHeightmap.GetLength(0); i++)
        {
            for (int j = 0; j < downScaledHeightmap.GetLength(1); j++)
            {
                downScaledHeightmap[i, j] = m_heightCurve.Evaluate(Mathf.PerlinNoise(i * m_octaveProperties.frequency + m_octaveProperties.seedX, j * m_octaveProperties.frequency + m_octaveProperties.seedY)) * m_octaveProperties.amplitude;
            }
        }

        // upscale

        float[,] lastUpscaleStage = downScaledHeightmap;

        while (lastUpscaleStage.GetLength(0) < m_sizeEdge)
        {
           

            lastUpscaleStage = ArrayTools.upscaleHeightmapDiamondSquare(lastUpscaleStage);
        }

        m_result = lastUpscaleStage;

        // smoothing
        float[,] temp_blurArray;
        float temp_average;
        for (int i = 0; i < m_octaveProperties.smoothCount; i++)
        {
            temp_blurArray = new float[m_sizeEdge, m_sizeEdge];
            for (int j = 1; j < m_sizeEdge - 1; j++)
            {
                for (int k = 1; k < m_sizeEdge - 1; k++)
                {
                    temp_average = 0;
                    for (int l = -1; l < 2; l++)
                    {
                        for (int m = -1; m < 2; m++)
                        {
                            temp_average += m_result[j + l, k + m];
                        }
                    }
                    temp_average /= 9;

                    temp_blurArray[j, k] = temp_average;
                }
            }
            m_result = temp_blurArray;
        }

        setIsDoneState(true);
    }

}
