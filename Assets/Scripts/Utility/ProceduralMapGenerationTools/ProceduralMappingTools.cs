using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ProceduralMappingTools {

    public static float[,] getDiamondSquareMountain(int sizeExp, Keyframe[] roughnessStagesKeys,float roughnessFactor, float seedX, float seedY, float perlinNoiseFreqency, bool bordersZero, bool MiddleToRoughness )
    {
        int arrayEdgeLength = (int)Mathf.Pow(2, sizeExp) + 1;
        float[,] outputArray = new float[arrayEdgeLength, arrayEdgeLength];
        AnimationCurve roughnessCurve = new AnimationCurve(roughnessStagesKeys);

        for (int i = 0; i < arrayEdgeLength; i++)
        {
            for (int j = 0; j < arrayEdgeLength; j++)
            {
                outputArray[i, j] = Mathf.Lerp(-1, 1, Mathf.PerlinNoise(seedX + i * perlinNoiseFreqency, seedY + j * perlinNoiseFreqency));
            }
        }

        if (!bordersZero)
        {
            outputArray[0, 0] *= roughnessCurve.Evaluate(0) * roughnessFactor;
            outputArray[arrayEdgeLength - 1, 0] *= roughnessCurve.Evaluate(0) * roughnessFactor;
            outputArray[0, arrayEdgeLength - 1] *= roughnessCurve.Evaluate(0) * roughnessFactor;
            outputArray[arrayEdgeLength - 1, arrayEdgeLength - 1] *= roughnessCurve.Evaluate(0) * roughnessFactor;
        }

        int middlePosXAndY = arrayEdgeLength / 2 + 1;
        float distanceToMid;
        int iterationStep;
        bool putX;
        bool putZ;
        float stageRoughness;

        for (int i = sizeExp - 1; i > -1; i--)
        {
            iterationStep = (int)Mathf.Pow(2, i);
            stageRoughness = roughnessCurve.Evaluate(((float)(sizeExp -1 - i)) / (sizeExp - 1)) * roughnessFactor;
            putX = false;

            if (MiddleToRoughness && i == sizeExp - 1)
            {
                outputArray[iterationStep, iterationStep] = roughnessFactor;
            }
            for (int j = 0; j < arrayEdgeLength; j += iterationStep)
            {
                putZ = false;
                for (int k = 0; k < arrayEdgeLength; k += iterationStep)
                {
                    distanceToMid = (float)Mathf.Max(Mathf.Abs(middlePosXAndY - j), Mathf.Abs(middlePosXAndY - k)) / middlePosXAndY;

                    if (bordersZero && (j < 1 || j > arrayEdgeLength - 2 || k < 1 || k > arrayEdgeLength - 2))
                    {
                        outputArray[j, k] = 0;
                    }
                    else if (putX && putZ)
                    {
                        // diamond
                        outputArray[j, k] = (outputArray[j + iterationStep, k + iterationStep] +
                            outputArray[j - iterationStep, k + iterationStep] +
                            outputArray[j + iterationStep, k - iterationStep] +
                            outputArray[j - iterationStep, k - iterationStep]) / 4
                        +   Mathf.Lerp(stageRoughness, 0, distanceToMid) * outputArray[j, k];
                    }
                    else if (putX != putZ)
                    {
                        // squares
                        if (putX)
                        {
                            outputArray[j, k] = (outputArray[j + iterationStep, k] +
                                outputArray[j - iterationStep, k]) / 2
                            + Mathf.Lerp(stageRoughness, 0, distanceToMid) * outputArray[j, k];
                        }
                        else
                        {
                            outputArray[j, k] = (outputArray[j, k + iterationStep] +
                                outputArray[j, k - iterationStep]) / 2
                            + Mathf.Lerp(stageRoughness, 0, distanceToMid) * outputArray[j, k];
                        }
                    }
                    putZ = !putZ;
                }
                putX = !putX;
            }
        }

        return outputArray;
    }

}
