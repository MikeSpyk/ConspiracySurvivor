using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AreaTools
{
    /// <summary>
    /// might not be working for complicated forms. keep it to simple geometric figures
    /// </summary>
    /// <param name="point"></param>
    /// <param name="corners"></param>
    /// <returns></returns>
    public static float distanceToAreaEdge(Vector3 point, params Vector3[] corners)
    {
        int closestIndex = -1;
        float closestDistance = float.MaxValue;
        float tempDistance;

        for (int i = 0; i < corners.Length; i++)
        {
            tempDistance = Vector3.Distance(point, corners[i]);

            if (tempDistance < closestDistance)
            {
                closestDistance = tempDistance;
                closestIndex = i;
            }
        }

        // find closest neighbor corner to closest corner

        if (closestIndex == corners.Length - 1) // special procedure the end of the array. we need to compare to the beginning of the array instead
        {
            if (Vector3.Distance(point, corners[closestIndex - 1]) < Vector3.Distance(point, corners[0]))
            {
                return VectorTools.DistanceToClampedLine(corners[closestIndex], corners[closestIndex - 1], point);
            }
            else
            {
                return VectorTools.DistanceToClampedLine(corners[closestIndex], corners[0], point);
            }
        }
        else if (closestIndex == 0)  // special procedure the start of the array. we need to compare to the end of the array instead
        {
            if (Vector3.Distance(point, corners[corners.Length - 1]) < Vector3.Distance(point, corners[closestIndex + 1]))
            {
                return VectorTools.DistanceToClampedLine(corners[closestIndex], corners[corners.Length - 1], point);
            }
            else
            {
                return VectorTools.DistanceToClampedLine(corners[closestIndex], corners[closestIndex + 1], point);
            }
        }
        else
        {
            if (Vector3.Distance(point, corners[closestIndex - 1]) < Vector3.Distance(point, corners[closestIndex + 1]))
            {
                return VectorTools.DistanceToClampedLine(corners[closestIndex], corners[closestIndex - 1], point);
            }
            else
            {
                return VectorTools.DistanceToClampedLine(corners[closestIndex], corners[closestIndex + 1], point);
            }
        }
    }

    public static float distanceToAreaEdgeExpensive(Vector3 point, params Vector3[] corners)
    {
        float closestDistance = float.MaxValue;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            closestDistance = Mathf.Min(closestDistance, VectorTools.DistanceToClampedLine(corners[i], corners[i + 1], point));
        }

        closestDistance = Mathf.Min(closestDistance, VectorTools.DistanceToClampedLine(corners[corners.Length - 1], corners[0], point));

        return closestDistance;
    }
}
