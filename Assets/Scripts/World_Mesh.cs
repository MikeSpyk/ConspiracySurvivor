using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World_Mesh : Custom_Mesh
{
    private float m_minVertexDistance = 1;
    public WorldMeshData m_worldMeshData = null;
    [SerializeField] private float m_hideMoveDistance = 100f;
    [SerializeField] private WorldManager.vertexMapTextureNames m_missingTexture;
    [SerializeField] private int m_shaderTextureLength = 169;

    private Texture2D m_shaderTextureMap;
    private Color[] m_shaderTextureColors;

    // Use this for initialization
    protected void Awake()
    {
        base.Awake();

        m_shaderTextureMap = new Texture2D(25, 25, TextureFormat.RGBAFloat, false);
        m_shaderTextureMap.filterMode = FilterMode.Point;
        m_shaderTextureColors = new Color[25 * 25];
        for (int i = 0; i < m_shaderTextureColors.Length; i++)
        {
            m_shaderTextureColors[i] = new Color();
        }

        m_minVertexDistance = WorldManager.singleton.getDefaultSubmeshVertDistance();
    }

    public WorldMeshData getWorldMeshData()
    {
        return m_worldMeshData;
    }

    /// <summary>
    /// disables the renderer and collider and moves the terrain downwards
    /// </summary>
    public void hide_TerrainCreation()
    {
        setVisibility(false);
        transform.position = transform.position + Vector3.down * m_hideMoveDistance;
    }
    /// <summary>
    /// moves terrain to its position
    /// </summary>
    public void unhide_TerrainCreation()
    {
        transform.position = m_worldMeshData.m_objectWorldPosition;
    }

    public void initialize(WorldMeshData meshData)
    {
        if (meshData.verifyIntegrity() == false)
        {
            Debug.LogError("World_Mesh: verifing meshData failed.");
            return;
        }

        m_worldMeshData = meshData;
        m_vertexDistance = meshData.m_vertexDistance;
        m_vertex_count_x = meshData.m_vertexEdgeCount;
        m_vertex_count_z = meshData.m_vertexEdgeCount;
        m_highestVertex = meshData.m_highestVertex;
        m_meshHeightMiddle = meshData.m_vertices[(m_vertex_count_z / 2) * m_vertex_count_x + m_vertex_count_x / 2].y;

        m_meshFilter.mesh.Clear();
        m_meshFilter.mesh.vertices = meshData.m_vertices;
        m_meshFilter.mesh.triangles = meshData.m_triangles;
        m_meshFilter.mesh.uv = meshData.m_UVs;
        m_meshFilter.mesh.normals = meshData.m_normals;
        m_meshFilter.mesh.tangents = meshData.m_tangents;
        m_meshFilter.mesh.name = "Terrain-Mesh";
        m_meshCollider.sharedMesh = m_meshFilter.mesh;

        transform.position = meshData.m_objectWorldPosition + Vector3.down * m_hideMoveDistance;

        refreshBounds(meshData.m_objectWorldPosition);

        setVisibility(true);

        setMaterialShaderVertexTextureMap(meshData.m_textures, meshData.m_lowerLeftHeightmapPos.x, meshData.m_lowerLeftHeightmapPos.y);
    }

    public void setMaterialShaderVertexTextureMap(byte[] inTextureMap, int UVOffsetX, int UVOffsetY)
    {
        /*

        float[] floatOutputArray = new float[m_shaderTextureLength];

        if (inTextureMap.Length != m_shaderTextureLength)
        {
            Debug.LogError("input-TextureMap-Array-length is wrong size. expecting " + m_shaderTextureLength + ". input-size: " + inTextureMap.Length);

            for (int i = 0; i < floatOutputArray.Length; i++)
            {
                floatOutputArray[i] = (byte)m_missingTexture;
            }
        }
        else
        {
            for (int i = 0; i < floatOutputArray.Length; i++)
            {
                floatOutputArray[i] = inTextureMap[i];
            }
        }

        m_renderer.material.SetInt("_UVWorldOffsetX", UVOffsetX);
        m_renderer.material.SetInt("_UVWorldOffsetZ", UVOffsetY);

        m_renderer.material.SetFloatArray("_VertexTextureMap", floatOutputArray);
        m_renderer.material.SetFloat("_TexScaleFactor", m_vertexDistance / m_minVertexDistance);

        */

        for (int i = 0; i < m_vertex_count_x - 1; i++)
        {
            for (int j = 0; j < m_vertex_count_z - 1; j++)
            {
                m_shaderTextureColors[j * m_vertex_count_x + i].r = inTextureMap[j * m_vertex_count_x + i] / 100f;
                m_shaderTextureColors[j * m_vertex_count_x + i].g = inTextureMap[j * m_vertex_count_x + i + 1] / 100f;
                m_shaderTextureColors[j * m_vertex_count_x + i].b = inTextureMap[j * m_vertex_count_x + i + m_vertex_count_x] / 100f;
                m_shaderTextureColors[j * m_vertex_count_x + i].a = inTextureMap[j * m_vertex_count_x + i + m_vertex_count_x + 1] / 100f;
            }
        }

        m_shaderTextureMap.SetPixels(m_shaderTextureColors);
        m_shaderTextureMap.Apply();

        m_renderer.material.SetTexture("Texture2D_EF3655E9", m_shaderTextureMap);
    }

}
