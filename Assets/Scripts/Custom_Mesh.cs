using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Custom_Mesh : MonoBehaviour
{
    protected int m_vertex_count_x = 150;
    protected int m_vertex_count_z = 150;
    protected float m_vertexDistance = .01f; // distance between vertexes in mesh

    protected MeshCollider m_meshCollider;
    protected MeshFilter m_meshFilter;
    protected Renderer m_renderer;
    protected Bounds m_bounds;
    protected MeshRenderer m_meshRenderer;

    protected float m_meshHeightMiddle; // the height of the vertex in the middle of the mesh 
    protected float m_highestVertex;

    public void setVisibility(bool newState)
    {
        m_meshRenderer.enabled = newState;
        m_meshCollider.enabled = newState;
    }

    protected void Awake()
    {
        m_meshFilter = GetComponent<MeshFilter>();
        m_meshCollider = GetComponent<MeshCollider>();
        m_renderer = GetComponent<Renderer>();
        if (m_renderer != null)
        {
            m_bounds = m_renderer.bounds;
        }
        m_meshRenderer = GetComponent<MeshRenderer>();
    }

    public void buildMesh(float[] vertexHeightMap, bool buildUV = true, bool buildTangents = true, bool buildNormals = true)
    {
        if (vertexHeightMap.Length == m_vertex_count_x * m_vertex_count_z)
        {

        }
        else
        {
            Debug.LogError("wrong array-Size on new Vertex-Height-Map: \"" + vertexHeightMap.Length + "\". expecting: " + (m_vertex_count_x * m_vertex_count_z));
            return;
        }

        if (vertexHeightMap == null)
        {
            Debug.LogError("can't build mesh: Vertex-Height-Map is NULL");
        }

        m_meshHeightMiddle = vertexHeightMap[(m_vertex_count_z / 2) * m_vertex_count_x + m_vertex_count_x / 2];
        //Debug.DrawRay(new Vector3( transform.position.x,meshHeightMiddle,transform.position.z), Vector3.up, Color.red, 100f);

        Vector3 meshOffset = -new Vector3(m_vertexDistance * m_vertex_count_x / 2 - m_vertexDistance / 2, +m_meshHeightMiddle, m_vertexDistance * m_vertex_count_z / 2 - m_vertexDistance / 2);
        Vector3[] MeshVerts = new Vector3[m_vertex_count_x * m_vertex_count_z];
        Vector2[] MeshUV = new Vector2[m_vertex_count_x * m_vertex_count_z];
        int[] triangles = null;

        int count2 = 0;
        for (int i = 0; i < m_vertex_count_z; i++)
        {
            for (int j = 0; j < m_vertex_count_x; j++)
            {
                m_highestVertex = Mathf.Max(m_highestVertex, vertexHeightMap[j + m_vertex_count_x * i]);

                MeshVerts[count2] = meshOffset + new Vector3(j * m_vertexDistance, vertexHeightMap[j + m_vertex_count_x * i], i * m_vertexDistance);
                MeshUV[count2] = new Vector2(1.0f * j / m_vertex_count_x, 1.0f * i / m_vertex_count_z);
                count2++;
            }
        }

        int[] trianglesTest = new int[m_vertex_count_x * m_vertex_count_z * 6];

        for (int i = 0; i < m_vertex_count_x * m_vertex_count_z * 6; i++)
        {
            trianglesTest[i] = -1;
        }

        int count = 0;
        for (int i = 0; i < m_vertex_count_z - 1; i++)
        {
            for (int j = 0; j < m_vertex_count_x - 1; j++)
            {
                trianglesTest[count] = j + m_vertex_count_x * i;
                count++;
                trianglesTest[count] = j + 1 + m_vertex_count_x * (i + 1);
                count++;
                trianglesTest[count] = j + 1 + m_vertex_count_x * i;
                count++;
            }
        }
        for (int i = 0; i < m_vertex_count_z - 1; i++)
        {
            for (int j = 0; j < m_vertex_count_x - 1; j++)
            {
                trianglesTest[count] = j + m_vertex_count_x * i;
                count++;
                trianglesTest[count] = j + m_vertex_count_x * (i + 1);
                count++;
                trianglesTest[count] = j + 1 + m_vertex_count_x * (i + 1);
                count++;
            }
        }

        for (int i = 0; i < m_vertex_count_x * m_vertex_count_z * 10; i++)
        {
            if (trianglesTest[i] == -1)
            {
                triangles = new int[i];

                for (int j = 0; j < i; j++)
                {
                    triangles[j] = trianglesTest[j];
                }

                break;
            }
        }

        if (m_meshFilter != null)
        {
            m_meshFilter.mesh.Clear();
            m_meshFilter.mesh.vertices = MeshVerts;
            m_meshFilter.mesh.triangles = triangles;

            if (buildUV)
            {
                m_meshFilter.mesh.uv = MeshUV;
            }

            if (buildNormals)
            {
                m_meshFilter.mesh.RecalculateNormals();
            }

            if (buildTangents)
            {
                calculateMeshTangents(m_meshFilter.mesh);
            }

            m_meshFilter.mesh.name = "World_Distance_Terrain Collider";

            m_meshFilter.sharedMesh = m_meshFilter.mesh;

            transform.position = new Vector3(transform.position.x, m_meshHeightMiddle, transform.position.z);
        }
        else
        {
            Debug.LogWarning("No MeshFilter found");
        }

        refreshBounds(gameObject.transform.position);

        // debug rays

        /*
		Debug.DrawRay(transform.position + new Vector3(getTotalSizeX()/2,0.0f, getTotalSizeZ()/2), Vector3.up * 100, Color.red, 100f);
		Debug.DrawRay(transform.position + new Vector3(-getTotalSizeX()/2,0.0f, getTotalSizeZ()/2), Vector3.up * 100, Color.blue, 100f);
		Debug.DrawRay(transform.position + new Vector3(getTotalSizeX()/2,0.0f, -getTotalSizeZ()/2), Vector3.up * 100, Color.green, 100f);
		Debug.DrawRay(transform.position + new Vector3(-getTotalSizeX()/2,0.0f, -getTotalSizeZ()/2), Vector3.up * 100, Color.black, 100f);
		*/
    }

    protected void refreshBounds(Vector3 meshCenter)
    {
        m_bounds.center = meshCenter;
        m_bounds.extents = new Vector3(m_vertexDistance * (m_vertex_count_x + 1) / 2, m_highestVertex / 2, m_vertexDistance * (m_vertex_count_x + 1) / 2);
        m_meshFilter.mesh.RecalculateBounds();
        //gameObject.GetComponent<Mesh>().bounds = myBounds;.
        //Debug.DrawRay(myBounds.center, Vector3.up, Color.red, 30f);
    }

    public float getTotalSizeX()
    {
        return (m_vertex_count_x - 1) * m_vertexDistance;
    }

    public float getTotalSizeZ()
    {
        return (m_vertex_count_z - 1) * m_vertexDistance;
    }

    private void calculateMeshTangents(Mesh mesh)
    {
        //speed up math by copying the mesh arrays
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        Vector2[] uv = mesh.uv;
        Vector3[] normals = mesh.normals;

        //variable definitions
        int triangleCount = triangles.Length;
        int vertexCount = vertices.Length;

        Vector3[] tan1 = new Vector3[vertexCount];
        Vector3[] tan2 = new Vector3[vertexCount];

        Vector4[] tangents = new Vector4[vertexCount];

        for (long a = 0; a < triangleCount; a += 3)
        {
            long i1 = triangles[a + 0];
            long i2 = triangles[a + 1];
            long i3 = triangles[a + 2];

            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 v3 = vertices[i3];

            Vector2 w1 = uv[i1];
            Vector2 w2 = uv[i2];
            Vector2 w3 = uv[i3];

            float x1 = v2.x - v1.x;
            float x2 = v3.x - v1.x;
            float y1 = v2.y - v1.y;
            float y2 = v3.y - v1.y;
            float z1 = v2.z - v1.z;
            float z2 = v3.z - v1.z;

            float s1 = w2.x - w1.x;
            float s2 = w3.x - w1.x;
            float t1 = w2.y - w1.y;
            float t2 = w3.y - w1.y;

            float r = 1.0f / (s1 * t2 - s2 * t1);

            Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
            Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

            tan1[i1] += sdir;
            tan1[i2] += sdir;
            tan1[i3] += sdir;

            tan2[i1] += tdir;
            tan2[i2] += tdir;
            tan2[i3] += tdir;
        }


        for (int a = 0; a < vertexCount; ++a)
        {
            Vector3 n = normals[a];
            Vector3 t = tan1[a];

            //Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
            //tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
            Vector3.OrthoNormalize(ref n, ref t);
            tangents[a].x = t.x;
            tangents[a].y = t.y;
            tangents[a].z = t.z;

            tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
        }

        mesh.tangents = tangents;
    }
}
