using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldMeshColliderOnly : Custom_Mesh
{
    public WorldMeshData m_meshData;

    public void initialize(WorldMeshData meshData)
    {
        if (meshData.verifyIntegrity() == false)
        {
            Debug.LogError("World_Mesh: verifing meshData failed.");
            return;
        }

        m_meshData = meshData;

        m_meshFilter.mesh.Clear();
        m_meshFilter.mesh.vertices = meshData.m_vertices;
        m_meshFilter.mesh.triangles = meshData.m_triangles;
        //m_meshFilter.mesh.uv = meshData.m_UVs;
        //m_meshFilter.mesh.normals = meshData.m_normals;
        //m_meshFilter.mesh.tangents = meshData.m_tangents;
        m_meshFilter.mesh.name = "Terrain-Mesh";
        m_meshCollider.sharedMesh = m_meshFilter.mesh;

        transform.position = meshData.m_objectWorldPosition;
    }
}
