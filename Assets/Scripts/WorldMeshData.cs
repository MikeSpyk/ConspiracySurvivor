using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldMeshData
{
    public WorldMeshData(int vertexEdgeCount)
    {
        m_textures = new byte[vertexEdgeCount * vertexEdgeCount];
        m_vertices = new Vector3[vertexEdgeCount * vertexEdgeCount];
        m_triangles = new int[vertexEdgeCount * vertexEdgeCount * 6 - 12* vertexEdgeCount + 6]; // determined by trial
        m_triangleTest = new int[vertexEdgeCount * vertexEdgeCount * 6];
        m_UVs = new Vector2[vertexEdgeCount * vertexEdgeCount];
        m_normals = new Vector3[vertexEdgeCount * vertexEdgeCount];
        m_tangents = new Vector4[vertexEdgeCount * vertexEdgeCount];
        m_highestVertex = float.MinValue;
        m_vertexEdgeCount = vertexEdgeCount;
    }

    public Vector2Int m_lowerLeftHeightmapPos;
    public Vector3 m_objectWorldPosition;
    public int m_vertexEdgeCount;
    public float m_vertexDistance;
    public float m_highestVertex;
    public byte[] m_textures;
    public Vector3[] m_vertices;
    public int[] m_triangles;
    public int[] m_triangleTest;
    public Vector2[] m_UVs;
    public Vector3[] m_normals;
    public Vector4[] m_tangents;
    public int m_ID = -1;
    public int m_LODLevel = -1;

    /// <summary>
    /// returns false if there are problems with this object
    /// </summary>
    /// <returns></returns>
    public bool verifyIntegrity()
    {
        bool foundError = false;

        if(m_vertexEdgeCount < 0 )
        {
            Debug.LogError("WorldMeshData: verifyIntegrity: vertexEdgeCount = " + m_vertexEdgeCount);
            foundError = true;
        }
        else if(m_vertexEdgeCount > 250)
        {
            Debug.LogWarning("WorldMeshData: verifyIntegrity: vertexEdgeCount very close to the unity-vertices-limt (vertexEdgeCount = " + m_vertexEdgeCount + ")");
        }

        if(m_vertexDistance <= 0)
        {
            Debug.LogError("WorldMeshData: verifyIntegrity: m_vertexDistance = " + m_vertexDistance);
            foundError = true;
        }

        if(m_textures.Length != (m_vertexEdgeCount * m_vertexEdgeCount))
        { 
            Debug.LogError("WorldMeshData: verifyIntegrity: m_textures.Length out of bounds. m_textures.Length = " + m_textures.Length);
            foundError = true;
        }

        if (m_vertices.Length != (m_vertexEdgeCount * m_vertexEdgeCount))
        {
            Debug.LogError("WorldMeshData: verifyIntegrity: m_vertices.Length out of bounds. m_vertices.Length = " + m_vertices.Length);
            foundError = true;
        }

        if (m_triangles.Length != (m_vertexEdgeCount * m_vertexEdgeCount * 6 - 12 * m_vertexEdgeCount + 6))
        {
            Debug.LogError("WorldMeshData: verifyIntegrity: m_triangles.Length out of bounds. m_triangles.Length = " + m_triangles.Length);
            foundError = true;
        }

        if (m_triangleTest.Length != (m_vertexEdgeCount * m_vertexEdgeCount * 6))
        {
            Debug.LogError("WorldMeshData: verifyIntegrity: m_triangleTest.Length out of bounds. m_triangleTest.Length = " + m_triangleTest.Length);
            foundError = true;
        }

        if (m_UVs.Length != (m_vertexEdgeCount * m_vertexEdgeCount))
        {
            Debug.LogError("WorldMeshData: verifyIntegrity: m_UVs.Length out of bounds. m_UVs.Length = " + m_UVs.Length);
            foundError = true;
        }

        if (m_normals.Length != (m_vertexEdgeCount * m_vertexEdgeCount))
        {
            Debug.LogError("WorldMeshData: verifyIntegrity: m_normals.Length out of bounds. m_normals.Length = " + m_normals.Length);
            foundError = true;
        }

        if (m_tangents.Length != (m_vertexEdgeCount * m_vertexEdgeCount))
        {
            Debug.LogError("WorldMeshData: verifyIntegrity: m_tangents.Length out of bounds. m_tangents.Length = " + m_tangents.Length);
            foundError = true;
        }

        if(foundError)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

}
