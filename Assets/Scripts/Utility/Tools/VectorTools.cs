using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorTools
{
    public static Vector2 projectFoward(Vector2 forwardNormal, Vector2 vector)
    {
        float angleDeltaForward = Vector2.SignedAngle(forwardNormal, vector);

        return new Vector2(
                            -Mathf.Sin(Mathf.Deg2Rad * angleDeltaForward) * vector.magnitude,
                            Mathf.Cos(Mathf.Deg2Rad * angleDeltaForward) * vector.magnitude
                            );
    }

    public static Vector2 Vec2FromVec3XZ(Vector3 vec3)
    {
        return new Vector2(vec3.x, vec3.z);
    }

    public static Vector3 Vec3XZFromVec2(Vector2Int vec2)
    {
        return new Vector3(vec2.x, 0, vec2.y);
    }

    public static Vector2 Rotate(Vector2 vec, float degrees)
    {
        return Quaternion.Euler(0, 0, degrees) * vec;
    }
    public static Vector2Int Rotate(Vector2Int vec, float degrees)
    {
        Vector3 vec3 = new Vector3(vec.x, vec.y, 0);
        vec3 = Quaternion.Euler(0, 0, degrees) * vec3;
        return new Vector2Int((int)vec3.x, (int)vec3.y);
    }

    public static bool Vector3TryParse(out Vector3 vector, params string[] input)
    {
        vector = Vector3.zero;

        string text = "";

        for (int i = 0; i < input.Length; i++)
        {
            text += " " + input[i];
        }

        string[] components = text.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (components.Length < 3)
        {
            return false;
        }
        else
        {
            float x;
            float y;
            float z;

            if (!float.TryParse(components[0], out x))
            {
                return false;
            }

            if (!float.TryParse(components[1], out y))
            {
                return false;
            }

            if (!float.TryParse(components[2], out z))
            {
                return false;
            }

            vector = new Vector3(x, y, z);

            return true;
        }
    }

    /// <summary>
    /// distance a point has to an infinitely long line
    /// </summary>
    /// <param name="lineOrigin"></param>
    /// <param name="lineDir"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static float DistanceToLine(Vector3 lineOrigin, Vector3 lineDir, Vector3 point)
    {
        return Vector3.Cross(lineDir, point - lineOrigin).magnitude;
    }

    /// <summary>
    /// distance a point has to a line with a defined start and end
    /// </summary>
    /// <param name="lineOrigin"></param>
    /// <param name="lineEnd"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static float DistanceToClampedLine(Vector3 lineOrigin, Vector3 lineEnd, Vector3 point)
    {
        Vector3 dir = (lineEnd - lineOrigin).normalized;

        Vector3 outsideOrigin = lineOrigin - dir * 0.001f;
        Vector3 outsideEnd = lineEnd + dir * 0.001f;

        float distanceOrigin = Vector3.Distance(point, lineOrigin);
        float distanceEnd = Vector3.Distance(point, lineEnd);

        if (Vector3.Distance(point, outsideOrigin) < distanceOrigin)
        {
            // not parallel to line => distance to origin point is smaller than to line
            return distanceOrigin;
        }
        else if (Vector3.Distance(point, outsideEnd) < distanceEnd)
        {
            // not parallel to line => distance to end point is smaller than to line
            return distanceEnd;
        }
        else
        {
            return DistanceToLine(lineOrigin, dir, point);
        }
    }

}
