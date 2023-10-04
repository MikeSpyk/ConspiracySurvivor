using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ArrayTools
{
    public static void normalizeArray(float[,] inArray)
    {
        float maxValue = float.MinValue;
        for (int i = 0; i < inArray.GetLength(0); i++)
        {
            for (int j = 0; j < inArray.GetLength(1); j++)
            {
                maxValue = Mathf.Max(maxValue, Mathf.Abs(inArray[i, j]));
            }
        }

        for (int i = 0; i < inArray.GetLength(0); i++)
        {
            for (int j = 0; j < inArray.GetLength(1); j++)
            {
                inArray[i, j] /= maxValue;
            }
        }
    }

    public static float[,] turbulenceDisplacement(float[,] input, Keyframe[] animationCurveKeys, int amplitude)
    {
        AnimationCurve turbulenceCurveTemp = new AnimationCurve(animationCurveKeys);

        float[,] output1 = new float[input.GetLength(0), input.GetLength(1)];
        float[,] output2 = new float[input.GetLength(0), input.GetLength(1)];

        int currentOffset;
        int currentIndex;
        for (int i = 0; i < input.GetLength(0); i++)
        {
            currentOffset = (int)(turbulenceCurveTemp.Evaluate((float)i / input.GetLength(0)) * amplitude);
            for (int j = 0; j < input.GetLength(1); j++)
            {
                currentIndex = j + currentOffset;

                if (currentIndex > 0 && currentIndex < input.GetLength(1))
                {
                    output1[i, j] = input[i, currentIndex];
                }
            }
        }

        for (int i = 0; i < input.GetLength(0); i++)
        {
            currentOffset = (int)(turbulenceCurveTemp.Evaluate((float)i / input.GetLength(0)) * amplitude);
            for (int j = 0; j < input.GetLength(1); j++)
            {
                currentIndex = j + currentOffset;

                if (currentIndex > 0 && currentIndex < input.GetLength(1))
                {
                    output2[i, j] = output1[currentIndex, i];
                }
            }
        }

        return output2;
    }

    public static void applyGaussianBlur(float[,] input, int kernelSize, int iterations)
    {
        float average;
        float[,] tempArray = new float[input.GetLength(0), input.GetLength(1)];
        int kernelSizeSquare = 0;

        for (int l = -kernelSize; l < kernelSize; l++)
        {
            for (int m = -kernelSize; m < kernelSize; m++)
            {
                kernelSizeSquare++;
            }
        }


        for (int k = 0; k < iterations; k++)
        {
            for (int i = kernelSize; i < input.GetLength(0) - kernelSize; i++)
            {
                for (int j = kernelSize; j < input.GetLength(1) - kernelSize; j++)
                {

                    average = 0;
                    for (int l = -kernelSize; l < kernelSize; l++)
                    {
                        for (int m = -kernelSize; m < kernelSize; m++)
                        {
                            average += input[i + l, j + m];
                        }
                    }
                    tempArray[i, j] = average / kernelSizeSquare;
                }
            }

            for (int i = kernelSize; i < input.GetLength(0) - kernelSize; i++)
            {
                for (int j = kernelSize; j < input.GetLength(1) - kernelSize; j++)
                {
                    input[i, j] = tempArray[i, j];
                }
            }
        }
    }

    public static void raiseAboveZeroArray(float[,] inArray)
    {
        float minValue = float.MaxValue;
        for (int i = 0; i < inArray.GetLength(0); i++)
        {
            for (int j = 0; j < inArray.GetLength(1); j++)
            {
                minValue = Mathf.Min(minValue, inArray[i, j]);
            }
        }

        if (minValue > 0)
        {
            return;
        }

        minValue = Mathf.Abs(minValue);

        for (int i = 0; i < inArray.GetLength(0); i++)
        {
            for (int j = 0; j < inArray.GetLength(1); j++)
            {
                inArray[i, j] += minValue;
            }
        }
    }

    public static void raiseToMid(float[] inArray, int edgeLength, float midRaise)
    {
        Vector2 MidPostition = new Vector2(edgeLength / 2, edgeLength / 2);
        Vector2 CurrentPosition;
        float maxVecDistnace = Vector2.Distance(MidPostition, new Vector2(0, 0));

        // raise from border to middle, middle is hightest point
        for (int i = 0; i < edgeLength; i++)
        {
            for (int j = 0; j < edgeLength; j++)
            {
                CurrentPosition = new Vector2(i, j);
                inArray[i + edgeLength * j] += midRaise * (maxVecDistnace - Vector2.Distance(CurrentPosition, MidPostition));
            }
        }
    }

    public static void setBordersToZero(float[] inArray, int edgeLength)
    {
        // set the borders of the mesh to 0
        for (int i = 0; i < edgeLength; i++)
        {
            inArray[i] = 0.0f;
            inArray[i + edgeLength * (edgeLength - 1)] = 0.0f;
        }
        for (int i = 0; i < edgeLength; i++)
        {
            inArray[i * edgeLength] = 0.0f;
            inArray[edgeLength - 1 + edgeLength * i] = 0.0f;
        }
    }

    public static void addToArrayMax(float[,] receivingArray, float[,] arrayToAdd, Vector2Int offset)
    {
        for (int i = 0; i < arrayToAdd.GetLength(0); i++)
        {
            for (int j = 0; j < arrayToAdd.GetLength(1); j++)
            {
                receivingArray[i + offset.x, j + offset.y] = Mathf.Max(arrayToAdd[i, j], receivingArray[i + offset.x, j + offset.y]);
                //receivingArray[i+offset.x, j + offset.y] = arrayToAdd[i,j];
            }
        }
    }
    public static void addToArrayMax(float[] receivingArray, int receivingArrayEdgeSize, float[,] arrayToAdd, Vector2Int offset)
    {
        if (arrayToAdd.GetLength(0) != arrayToAdd.GetLength(1))
        {
            Debug.LogError("ArrayTools: combineArray: arrayToAdd is not quadratic");
            return;
        }

        if (Mathf.Sqrt(receivingArray.Length) < arrayToAdd.GetLength(0))
        {
            Debug.LogError("ArrayTools: combineArray: receivingArray-Edge-Length is smaller than arrayToAdd-Egde-Length");
            return;
        }

        int arrayToAddSize = arrayToAdd.GetLength(0);

        if (offset.x - arrayToAddSize / 2 < 0 || offset.y - arrayToAddSize / 2 < 0 || offset.x + arrayToAddSize / 2 >= receivingArrayEdgeSize || offset.y + arrayToAddSize / 2 >= receivingArrayEdgeSize)
        {
            Debug.LogError("ArrayTools: combineArray: offset out of range");
        }


        int offsetX = offset.x - arrayToAddSize / 2;
        int offsetY = offset.y - arrayToAddSize / 2;

        for (int i = 0; i < arrayToAddSize; i++)
        {
            for (int j = 0; j < arrayToAddSize; j++)
            {
                receivingArray[i + offsetX + (j + offsetY) * receivingArrayEdgeSize] = Mathf.Max(arrayToAdd[i, j], receivingArray[i + offsetX + (j + offsetY) * receivingArrayEdgeSize]);
            }
        }
    }
    public static void addToArrayMax(ShortCompressedFloat receivingArray, float[,] arrayToAdd, Vector2Int offset)
    {
        // WARNING: not tested for non quadratic receivingArray

        if (arrayToAdd.GetLength(0) != arrayToAdd.GetLength(1))
        {
            Debug.LogError("ArrayTools: combineArray: arrayToAdd is not quadratic");
            return;
        }

        if (receivingArray.GetLength(0) < arrayToAdd.GetLength(0))
        {
            Debug.LogError("ArrayTools: combineArray: receivingArray-Edge-Length is smaller than arrayToAdd-Egde-Length");
            return;
        }

        int arrayToAddSize = arrayToAdd.GetLength(0);

        if (offset.x - arrayToAddSize / 2 < 0 || offset.y - arrayToAddSize / 2 < 0 || offset.x + arrayToAddSize / 2 >= receivingArray.GetLength(0) || offset.y + arrayToAddSize / 2 >= receivingArray.GetLength(1))
        {
            Debug.LogError("ArrayTools: combineArray: offset out of range");
        }


        int offsetX = offset.x - arrayToAddSize / 2;
        int offsetY = offset.y - arrayToAddSize / 2;

        for (int i = 0; i < arrayToAddSize; i++)
        {
            for (int j = 0; j < arrayToAddSize; j++)
            {
                receivingArray[i + offsetX, j + offsetY] = Mathf.Max(arrayToAdd[i, j], receivingArray[i + offsetX, j + offsetY]);
            }
        }
    }

    public static void addToArray(float[,] inArray, float summand)
    {
        for (int i = 0; i < inArray.GetLength(0); i++)
        {
            for (int j = 0; j < inArray.GetLength(1); j++)
            {
                inArray[i, j] += summand;
            }
        }
    }
    public static float addToArray(float[] array, int arrayEdgeLength, Vector2Int startPos, Vector2Int endPos, float summand)
    {
        float minValue = float.MaxValue;

        for (int i = startPos.x; i < endPos.x; i++)
        {
            for (int j = startPos.x; j < endPos.x; j++)
            {
                array[i + j * arrayEdgeLength] += summand;
            }
        }

        return minValue;
    }

    public static void createPit(float[] array, int arrayEdgeLength, Vector2Int middlePos, int pitRadius, float pitLowPoint)
    {
        int startPosX = middlePos.x - pitRadius;
        int startPosY = middlePos.y - pitRadius;
        int endPosX = middlePos.x + pitRadius;
        int endPosY = middlePos.y + pitRadius;

        float distanceToMid;

        for (int i = startPosX; i < endPosX; i++)
        {
            for (int j = startPosY; j < endPosY; j++)
            {
                distanceToMid = Vector2Int.Distance(middlePos, new Vector2Int(i, j));

                if (distanceToMid < pitRadius)
                {
                    array[i + j * arrayEdgeLength] = Mathf.Lerp(pitLowPoint, array[i + j * arrayEdgeLength], distanceToMid / pitRadius);
                }
            }
        }
    }
    public static void createPit(ShortCompressedFloat array, Vector2Int middlePos, int pitRadius, float pitLowPoint)
    {
        int startPosX = middlePos.x - pitRadius;
        int startPosY = middlePos.y - pitRadius;
        int endPosX = middlePos.x + pitRadius;
        int endPosY = middlePos.y + pitRadius;

        float distanceToMid;

        for (int i = startPosX; i < endPosX; i++)
        {
            for (int j = startPosY; j < endPosY; j++)
            {
                distanceToMid = Vector2Int.Distance(middlePos, new Vector2Int(i, j));

                if (distanceToMid < pitRadius)
                {
                    array[i, j] = Mathf.Lerp(pitLowPoint, array[i, j], distanceToMid / pitRadius);
                }
            }
        }
    }

    /// <summary>
    /// each member of the receivingArray gets the value of the respective member of the arrayToAdd added, if the receivingArray member is greater than the minHeightValue. if minHeightValue is smaller than 0 then members will get added for sure
    /// </summary>
    /// <param name="receivingArray">Receiving array.</param>
    /// <param name="arrayToAdd">Array to add.</param>
    /// <param name="minHeightValue">Minimum height value.</param>
    public static void additionArrayMembers(float[] receivingArray, float[] arrayToAdd, float minHeightValue)
    {
        if (receivingArray.Length != arrayToAdd.Length)
        {
            Debug.LogError("Array-sizes differ. receivingArray: " + receivingArray.Length + ". arrayToAdd: " + arrayToAdd.Length + ".");
            return;
        }

        if (minHeightValue < 0)
        {
            for (int i = 0; i < receivingArray.Length; i++)
            {
                receivingArray[i] += arrayToAdd[i];
            }
        }
        else
        {
            for (int i = 0; i < receivingArray.Length; i++)
            {
                if (receivingArray[i] > minHeightValue)
                    receivingArray[i] += arrayToAdd[i];
            }
        }
    }
    public static void additionArrayMembers(float[] receivingArray, float[] arrayToAdd)
    {
        if (receivingArray.Length != arrayToAdd.Length)
        {
            Debug.LogError("Array-sizes differ. receivingArray: " + receivingArray.Length + ". arrayToAdd: " + arrayToAdd.Length + ".");
            return;
        }

        for (int i = 0; i < receivingArray.Length; i++)
        {
            receivingArray[i] += arrayToAdd[i];
        }
    }
    public static void additionArrayMembers(float[,] receivingArray, float[,] arrayToAdd)
    {
        for (int i = 0; i < arrayToAdd.GetLength(0); i++)
        {
            for (int j = 0; j < arrayToAdd.GetLength(1); j++)
            {
                receivingArray[i, j] += arrayToAdd[i, j];
            }
        }
    }

    /// <summary>
    /// each member of the receivingArray gets the value of the respective member of the arrayToAdd added, how much of the value will get added depends on the distance to the middle: in the middle the value will get added fully, at the border of the (2D) Array the value will be 0
    /// </summary>
    /// <param name="receivingArray">Receiving array.</param>
    /// <param name="arrayToAdd">Array to add.</param>
    public static void additionArrayMembersSloping(float[] receivingArray, float[] arrayToAdd)
    {
        if (receivingArray.Length != arrayToAdd.Length)
        {
            Debug.LogError("Array-sizes differ. receivingArray: " + receivingArray.Length + ". arrayToAdd: " + arrayToAdd.Length + ".");
            return;
        }
        int edgeLength = (int)Mathf.Sqrt(receivingArray.Length);
        Vector2Int middle = new Vector2Int(edgeLength / 2, edgeLength / 2);
        Vector2Int currentPos = new Vector2Int(0, 0);


        float maxDistance = Vector2Int.Distance(currentPos, middle);

        for (int i = 0; i < edgeLength; i++)
        {
            for (int j = 0; j < edgeLength; j++)
            {
                currentPos.x = (int)(i * (2f / 3f)); // 2/3 so that the distance-cirle is to be tangent to the middle of the egde of the array instead of the corner point of the array
                currentPos.y = (int)(j * (2f / 3f));
                receivingArray[i + j * edgeLength] += arrayToAdd[i + j * edgeLength] * ((maxDistance - Vector2Int.Distance(currentPos, middle)) / maxDistance);
            }
        }
    }
    public static void additionArrayMembersSloping(float[] receivingArray, float[,] arrayToAdd)
    {
        int edgeLength = arrayToAdd.GetLength(0);
        Vector2Int middle = new Vector2Int(edgeLength / 2, edgeLength / 2);
        Vector2Int currentPos = new Vector2Int(0, 0);

        float maxDistance = Vector2Int.Distance(currentPos, middle);

        for (int i = 0; i < edgeLength; i++)
        {
            for (int j = 0; j < edgeLength; j++)
            {
                currentPos.x = (int)(i * (2f / 3f)); // 2/3 so that the distance-cirle is to be tangent to the middle of the egde of the array instead of the corner point of the array
                currentPos.y = (int)(j * (2f / 3f));
                receivingArray[i + j * edgeLength] += arrayToAdd[i, j] * ((maxDistance - Vector2Int.Distance(currentPos, middle)) / maxDistance);
            }
        }
    }

    /// <summary>
    /// Add the members of one Array to another array. the arrayToAdd might be smaller than the receivingArray. The offset is where the arrayToAdd will be added to the receivingArray (offset = middle of arrayToAdd)
    /// </summary>
    /// <param name="receivingArray"></param>
    /// <param name="receivingArrayEdgeSize"></param>
    /// <param name="arrayToAdd"></param>
    /// <param name="offset"></param>
    public static void combineArray(float[] receivingArray, int receivingArrayEdgeSize, float[,] arrayToAdd, Vector2Int offset)
    {
        if (arrayToAdd.GetLength(0) != arrayToAdd.GetLength(1))
        {
            Debug.LogError("ArrayTools: combineArray: arrayToAdd is not quadratic");
            return;
        }

        if (Mathf.Sqrt(receivingArray.Length) < arrayToAdd.GetLength(0))
        {
            Debug.LogError("ArrayTools: combineArray: receivingArray-Edge-Length is smaller than arrayToAdd-Egde-Length");
            return;
        }

        int arrayToAddSize = arrayToAdd.GetLength(0);

        if (offset.x - arrayToAddSize / 2 < 0 || offset.y - arrayToAddSize / 2 < 0 || offset.x + arrayToAddSize / 2 >= receivingArrayEdgeSize || offset.y + arrayToAddSize / 2 >= receivingArrayEdgeSize)
        {
            Debug.LogError("ArrayTools: combineArray: offset out of range");
        }


        int offsetX = offset.x - arrayToAddSize / 2;
        int offsetY = offset.y - arrayToAddSize / 2;

        for (int i = 0; i < arrayToAddSize; i++)
        {
            for (int j = 0; j < arrayToAddSize; j++)
            {
                receivingArray[i + offsetX + (j + offsetY) * receivingArrayEdgeSize] += arrayToAdd[i, j];
            }
        }
    }
    public static void combineArray(ShortCompressedFloat receivingArray, float[,] arrayToAdd, Vector2Int offset)
    {
        // WARNING: not tested for non quadratic receivingArray

        if (arrayToAdd.GetLength(0) != arrayToAdd.GetLength(1))
        {
            Debug.LogError("ArrayTools: combineArray: arrayToAdd is not quadratic");
            return;
        }

        if (receivingArray.GetLength(0) < arrayToAdd.GetLength(0))
        {
            Debug.LogError("ArrayTools: combineArray: receivingArray-Edge-Length is smaller than arrayToAdd-Egde-Length");
            return;
        }

        int arrayToAddSize = arrayToAdd.GetLength(0);

        if (offset.x - arrayToAddSize / 2 < 0 || offset.y - arrayToAddSize / 2 < 0 || offset.x + arrayToAddSize / 2 >= receivingArray.GetLength(0) || offset.y + arrayToAddSize / 2 >= receivingArray.GetLength(1))
        {
            Debug.LogError("ArrayTools: combineArray: offset out of range");
        }


        int offsetX = offset.x - arrayToAddSize / 2;
        int offsetY = offset.y - arrayToAddSize / 2;

        for (int i = 0; i < arrayToAddSize; i++)
        {
            for (int j = 0; j < arrayToAddSize; j++)
            {
                receivingArray[i + offsetX, j + offsetY] += arrayToAdd[i, j];
            }
        }
    }

    public static float getMin(float[] array, int arrayEdgeLength, Vector2Int startPos, Vector2Int endPos)
    {
        float minValue = float.MaxValue;

        for (int i = startPos.x; i < endPos.x; i++)
        {
            for (int j = startPos.x; j < endPos.x; j++)
            {
                minValue = Mathf.Min(minValue, array[i + j * arrayEdgeLength]);
            }
        }

        return minValue;
    }

    public static float getAverage(float[] array, int arrayEdgeLength, Vector2Int startPos, Vector2Int endPos)
    {
        float average = 0;

        for (int i = startPos.x; i < endPos.x; i++)
        {
            for (int j = startPos.x; j < endPos.x; j++)
            {
                average += array[i + j * arrayEdgeLength];
            }
        }

        average /= (endPos.x - startPos.x) * (endPos.y - startPos.y);

        return average;
    }
    public static float getAverage(ShortCompressedFloat array, Vector2Int startPos, Vector2Int endPos)
    {
        float average = 0;

        for (int i = startPos.x; i < endPos.x; i++)
        {
            for (int j = startPos.x; j < endPos.x; j++)
            {
                average += array[i, j];
            }
        }

        average /= (endPos.x - startPos.x) * (endPos.y - startPos.y);

        return average;
    }

    public static void multiplicateArray(float[] inArray, float factor)
    {
        for (int i = 0; i < inArray.Length; i++)
        {
            inArray[i] *= factor;
        }
    }
    public static void multiplicateArray(float[,] inArray, float factor)
    {
        for (int i = 0; i < inArray.GetLength(0); i++)
        {
            for (int j = 0; j < inArray.GetLength(1); j++)
            {
                inArray[i, j] *= factor;
            }
        }
    }
    public static List<HeightmapPosition> findHighpoints(float[,] array)
    {
        List<HeightmapPosition> returnValue = new List<HeightmapPosition>();

        int loopEnd1 = array.GetLength(0) - 1;
        int loopEnd2 = array.GetLength(1) - 1;

        for (int i = 1; i < loopEnd1; i++)
        {
            for (int j = 1; j < loopEnd2; j++)
            {
                if (
                    array[i, j] > array[i + 1, j] &&
                    array[i, j] > array[i + 1, j + 1] &&
                    array[i, j] > array[i, j + 1] &&
                    array[i, j] > array[i - 1, j + 1] &&

                    array[i, j] > array[i - 1, j] &&
                    array[i, j] > array[i - 1, j - 1] &&
                    array[i, j] > array[i, j - 1] &&
                    array[i, j] > array[i + 1, j - 1]
                    )
                {
                    returnValue.Add(new HeightmapPosition(array[i, j], new Vector2Int(i, j)));
                }
            }
        }

        return returnValue;
    }
    public static List<HeightmapPosition> findHighpoints(ShortCompressedFloat array, int minDistanceToEdge)
    {
        List<HeightmapPosition> returnValue = new List<HeightmapPosition>();

        int loopEnd1 = array.GetLength(0) - 1 - minDistanceToEdge;
        int loopEnd2 = array.GetLength(1) - 1 - minDistanceToEdge;

        for (int i = 1 + minDistanceToEdge; i < loopEnd1; i++)
        {
            for (int j = 1 + minDistanceToEdge; j < loopEnd2; j++)
            {
                if (
                    array[i, j] > array[i + 1, j] &&
                    array[i, j] > array[i + 1, j + 1] &&
                    array[i, j] > array[i, j + 1] &&
                    array[i, j] > array[i - 1, j + 1] &&

                    array[i, j] > array[i - 1, j] &&
                    array[i, j] > array[i - 1, j - 1] &&
                    array[i, j] > array[i, j - 1] &&
                    array[i, j] > array[i + 1, j - 1]
                    )
                {
                    returnValue.Add(new HeightmapPosition(array[i, j], new Vector2Int(i, j)));
                }
            }
        }

        return returnValue;
    }
    public static List<HeightmapPosition> findHighpoints(float[] array, int arrayEdgeLength, int minDistanceToEdge)
    {
        List<HeightmapPosition> returnValue = new List<HeightmapPosition>();

        int loopEnd1 = arrayEdgeLength - 1 - minDistanceToEdge;
        int loopEnd2 = arrayEdgeLength - 1 - minDistanceToEdge;

        for (int i = 1 + minDistanceToEdge; i < loopEnd1; i++)
        {
            for (int j = 1 + minDistanceToEdge; j < loopEnd2; j++)
            {
                if (
                    array[i + j * arrayEdgeLength] > array[i + 1 + j * arrayEdgeLength] &&
                    array[i + j * arrayEdgeLength] > array[i + 1 + (j + 1) * arrayEdgeLength] &&
                    array[i + j * arrayEdgeLength] > array[i + (j + 1) * arrayEdgeLength] &&
                    array[i + j * arrayEdgeLength] > array[i - 1 + (j + 1) * arrayEdgeLength] &&

                    array[i + j * arrayEdgeLength] > array[i - 1 + j * arrayEdgeLength] &&
                    array[i + j * arrayEdgeLength] > array[i - 1 + (j - 1) * arrayEdgeLength] &&
                    array[i + j * arrayEdgeLength] > array[i + (j - 1) * arrayEdgeLength] &&
                    array[i + j * arrayEdgeLength] > array[i + 1 + (j - 1) * arrayEdgeLength]
                    )
                {
                    returnValue.Add(new HeightmapPosition(array[i + j * arrayEdgeLength], new Vector2Int(i, j)));
                }
            }
        }

        return returnValue;
    }

    public static float[] downScaleHeightMapArray(float[] inArray)
    {
        int edgeLengthInArray = (int)Mathf.Sqrt(inArray.Length);
        int edgeLengthOutArray = edgeLengthInArray / 2;
        float[] outputArray = new float[edgeLengthOutArray * edgeLengthOutArray];

        for (int i = 0; i < edgeLengthOutArray - 1; i++)
        {
            for (int j = 0; j < edgeLengthOutArray - 1; j++)
            {
                outputArray[i + j * edgeLengthOutArray] = Mathf.Min(Mathf.Min(Mathf.Min(inArray[i * 2 + edgeLengthInArray * j * 2], inArray[i * 2 + 1 + edgeLengthInArray * j * 2]), inArray[i * 2 + (j * 2 + 1) * edgeLengthInArray]), inArray[i * 2 + 1 + (j * 2 + 1) * edgeLengthInArray]);
            }
        }
        return outputArray;
    }
    public static float[,] downScaleHeightMapArray(float[,] inArray)
    {
        float[,] outputArray = new float[inArray.GetLength(0) / 2, inArray.GetLength(1) / 2];

        for (int i = 0; i < outputArray.GetLength(0) - 1; i++)
        {
            for (int j = 0; j < outputArray.GetLength(1) - 1; j++)
            {
                outputArray[i, j] = Mathf.Min(Mathf.Min(Mathf.Min(inArray[i * 2, j * 2], inArray[i * 2 + 1, j * 2]), inArray[i * 2, j * 2 + 1]), inArray[i * 2 + 1, j * 2 + 1]);
            }
        }
        return outputArray;
    }

    private enum DiamondStep { SquareRow, SquareColumn };

    public static float[,] upscaleHeightmapDiamondSquare(float[,] heightmap)
    {
        return upscaleHeightmapDiamondSquare(heightmap, 1);
    }
    public static float[,] upscaleHeightmapDiamondSquare(float[,] heightmap, int scaleCount)
    {
        float[,] lastStage = heightmap;

        for (int i = 0; i < scaleCount; i++)
        {
            int currentSizeX = lastStage.GetLength(0) * 2;
            int currentSizeY = lastStage.GetLength(1) * 2;

            float[,] currentStage = new float[currentSizeX, currentSizeY];

            // copy old array
            for (int j = 0; j < lastStage.GetLength(0); j++)
            {
                for (int k = 0; k < lastStage.GetLength(1); k++)
                {
                    currentStage[j * 2, k * 2] = lastStage[j, k];
                }
            }

            DiamondStep currentStep = DiamondStep.SquareRow;

            // square step
            for (int j = 0; j < currentSizeY - 1; j++)
            {
                if (currentStep == DiamondStep.SquareRow)
                {
                    for (int k = 1; k < currentSizeX - 1; k += 2)
                    {
                        try
                        {
                            currentStage[k, j] = (currentStage[k - 1, j] + currentStage[k + 1, j]) / 2;
                        }
                        catch (System.Exception ex)
                        {
                            throw (new UnityException("" + ex + "; size: " + currentSizeX + "; k: " + k + "; j: " + j));
                        }
                    }
                }
                else
                {
                    for (int k = 0; k < currentSizeX - 1; k += 2)
                    {
                        currentStage[k, j] = (currentStage[k, j - 1] + currentStage[k, j + 1]) / 2;
                    }
                }
                if (currentStep == DiamondStep.SquareColumn)
                {
                    currentStep = DiamondStep.SquareRow;
                }
                else if (currentStep == DiamondStep.SquareRow)
                {
                    currentStep = DiamondStep.SquareColumn;
                }
            }

            // diamond step

            for (int j = 1; j < currentSizeY - 1; j += 2)
            {
                for (int k = 1; k < currentSizeX - 1; k += 2)
                {
                    currentStage[k, j] = (currentStage[k - 1, j] + currentStage[k + 1, j] + currentStage[k, j - 1] + currentStage[k, j + 1]) / 4;
                }
            }

            lastStage = currentStage;
        }

        return lastStage;
    }

}
