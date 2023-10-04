#define NO_MULTI_THREAD
#undef NO_MULTI_THREAD

using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightmapRendererCollider : HeightmapRenderer
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="heightmap"></param>
    /// <param name="texturemap"></param>
    /// <param name="vertexDistance"></param>
    /// <param name="verticesPerEdge"></param>
    /// <param name="texturesCount"></param>
    /// <param name="undefinedTextureIndex"></param>
    /// <param name="playerMeshRadius">how many meshes to create around a player</param>
    public HeightmapRendererCollider(ShortCompressedFloat heightmap, byte[,] texturemap, float vertexDistance, int verticesPerEdge, byte texturesCount, byte undefinedTextureIndex, int playerMeshRadius, float[] distanceScale) :
        base(heightmap, texturemap, vertexDistance, verticesPerEdge, texturesCount, undefinedTextureIndex, false)
    {
        m_playerMeshRadiusVerts = playerMeshRadius * verticesPerEdge;
        m_distanceScale = distanceScale;

#if !NO_MULTI_THREAD
        m_computeMeshesThread = new Thread(new ThreadStart(calculateMeshesProcedure));
        m_computeMeshesThread.Start();
#endif
    }

    ~HeightmapRendererCollider()
    {
        dispose();
    }

    private Thread m_computeMeshesThread = null;
    private long LOCKED_stopAllThreads = 0;
    private List<Vector2> m_playerPositions = new List<Vector2>();
    private int m_playerMeshRadiusVerts;
    private float[] m_distanceScale;
    private List<int> m_obsoleteObjectsID = new List<int>();

    public void startMeshCompute()
    {
#if NO_MULTI_THREAD
        calculateMeshesProcedure();
#else
        lock (m_startThreadMRE)
        {
            m_startThreadMRE.Set();
            Interlocked.Exchange(ref m_LOCKED_isDone, 0);
        }
#endif
    }

    private void calculateMeshesProcedure()
    {
        List<Vector2Int> m_newObjectsPos = new List<Vector2Int>();
        List<Vector2Int> m_obsoleteObjectsPos = new List<Vector2Int>();

        List<int> m_newObjectsScale = new List<int>();
        List<int> m_obsoleteObjectsScale = new List<int>();

        List<Vector2Int> playerPositionRaster = new List<Vector2Int>();
        List<Vector2Int> playerViewGroupStart = new List<Vector2Int>();
        List<Vector2Int> playerViewGroupEnd = new List<Vector2Int>();
        List<List<int>> viewGroupPlayers = new List<List<int>>();

        List<Vector2Int> objectCreatePositions = new List<Vector2Int>();
        List<int> objectCreateScale = new List<int>();
        List<Vector2Int> lastObjectCreatePositions = new List<Vector2Int>();
        List<int> lastObjectCreateScale = new List<int>();

        List<byte> hashBytes = new List<byte>();

        int vertCoundMinusOne = m_verticesCount - 1;

        System.Security.Cryptography.MD5 hashAlgorithm = System.Security.Cryptography.MD5.Create();

        byte[,] colliderLODMap = new byte[m_heightmap.GetLength(0) / vertCoundMinusOne, m_heightmap.GetLength(1) / vertCoundMinusOne];

        int playerPosX;
        int playerPosY;
        int playerPosXNoRange;
        int playerPosYNoRange;
        int playerPosXEnd;
        int playerPosYEnd;
        int playerViewDistance;

        int minValue;
        float distance;
        Vector2Int currentPos;
        Vector2Int middlePos;
        int currentScaleVerts;
        bool downScale;
        byte scaleParsed;
        byte nextScale;

        int maxInterationX;
        int maxInterationY;

        bool foundEntry;

        int hashValue;

        Interlocked.Exchange(ref m_LOCKED_isDone, 1);

#if !NO_MULTI_THREAD
        while (Interlocked.Read(ref LOCKED_stopAllThreads) == 0)
        {
            m_startThreadMRE.WaitOne();
#endif
            //Debug.Log("HeightmapRendererCollider: calculateMeshesProcedure: started compute: " + System.DateTime.Now.ToString());

            playerViewDistance = (int)Mathf.Pow(2, m_distanceScale.Length - 1) * vertCoundMinusOne;

            playerPositionRaster.Clear();
            playerViewGroupStart.Clear();
            playerViewGroupEnd.Clear();
            viewGroupPlayers.Clear();

            // find all players raster positions + create viewGroups

            for (int i = 0; i < m_playerPositions.Count; i++)
            {
                playerPosX = ((int)(m_playerPositions[i].x / (m_vertexDistance * vertCoundMinusOne))) * vertCoundMinusOne;
                playerPosY = ((int)(m_playerPositions[i].y / (m_vertexDistance * vertCoundMinusOne))) * vertCoundMinusOne;

                playerPositionRaster.Add(new Vector2Int(playerPosX, playerPosY));

                playerPosX = ((int)(m_playerPositions[i].x / (m_vertexDistance * vertCoundMinusOne * Mathf.Pow(2, m_distanceScale.Length)))) * vertCoundMinusOne * (int)Mathf.Pow(2, m_distanceScale.Length);
                playerPosY = ((int)(m_playerPositions[i].y / (m_vertexDistance * vertCoundMinusOne * Mathf.Pow(2, m_distanceScale.Length)))) * vertCoundMinusOne * (int)Mathf.Pow(2, m_distanceScale.Length);

                playerPosX -= vertCoundMinusOne * (int)Mathf.Pow(2, m_distanceScale.Length) * 2; // viewGroup start pos
                playerPosY -= vertCoundMinusOne * (int)Mathf.Pow(2, m_distanceScale.Length) * 2; // viewGroup start pos

                playerPosXNoRange = playerPosX;
                playerPosYNoRange = playerPosY;

                if (playerPosX < 0)
                {
                    playerPosX = 0;
                    playerPosXNoRange += vertCoundMinusOne * (int)Mathf.Pow(2, m_distanceScale.Length);
                }

                if (playerPosY < 0)
                {
                    playerPosY = 0;
                    playerPosYNoRange += vertCoundMinusOne * (int)Mathf.Pow(2, m_distanceScale.Length);
                }

                playerPosXEnd = playerPosXNoRange + vertCoundMinusOne * (int)Mathf.Pow(2, m_distanceScale.Length) * 4;
                playerPosYEnd = playerPosYNoRange + vertCoundMinusOne * (int)Mathf.Pow(2, m_distanceScale.Length) * 4;

                if (playerPosXEnd >= colliderLODMap.GetLength(0) * vertCoundMinusOne)
                {
                    playerPosXEnd = colliderLODMap.GetLength(0) * vertCoundMinusOne - 1;
                }

                if (playerPosYEnd >= colliderLODMap.GetLength(1) * vertCoundMinusOne)
                {
                    playerPosYEnd = colliderLODMap.GetLength(1) * vertCoundMinusOne - 1;
                }

                playerViewGroupStart.Add(new Vector2Int(playerPosX, playerPosY));
                playerViewGroupEnd.Add(new Vector2Int(playerPosXEnd, playerPosYEnd));
            }

            // associate viewGroups with players

            for (int i = 0; i < playerViewGroupStart.Count; i++)
            {
                viewGroupPlayers.Add(new List<int>());

                middlePos = new Vector2Int(playerViewGroupStart[i].x + (playerViewGroupEnd[i].x - playerViewGroupStart[i].x) / 2, playerViewGroupStart[i].y + (playerViewGroupEnd[i].y - playerViewGroupStart[i].y) / 2);

                for (int j = 0; j < playerPositionRaster.Count; j++)
                {
                    if (Vector2Int.Distance(middlePos, playerPositionRaster[j]) < playerViewDistance * 2.1f)
                    {
                        viewGroupPlayers[i].Add(j);
                    }
                }
            }

            for (int i = 0; i < colliderLODMap.GetLength(0); i++)
            {
                for (int j = 0; j < colliderLODMap.GetLength(1); j++)
                {
                    colliderLODMap[i, j] = byte.MaxValue;
                }
            }

            // create pre collider LOD map (circle map)

            for (int i = 0; i < playerViewGroupStart.Count; i++)
            {
                for (int j = playerViewGroupStart[i].x; j < playerViewGroupEnd[i].x; j += vertCoundMinusOne)
                {
                    for (int k = playerViewGroupStart[i].y; k < playerViewGroupEnd[i].y; k += vertCoundMinusOne)
                    {
                        currentPos = new Vector2Int(j, k);

                        minValue = int.MaxValue; // scale value

                        for (int l = 0; l < viewGroupPlayers[i].Count; l++)
                        {
                            distance = Vector2Int.Distance(currentPos, playerPositionRaster[viewGroupPlayers[i][l]]);

                            for (int m = 0; m < m_distanceScale.Length; m++)
                            {
                                if (distance < m_distanceScale[m])
                                {
                                    if (m < minValue)
                                    {
                                        //Debug.Log("found min value");
                                        minValue = m;
                                        //minValue = (int)(Random.value * 4); // TEST !!!!!!!!!!!!!!!!
                                        break;
                                    }
                                }
                            }
                        }

                        colliderLODMap[j / vertCoundMinusOne, k / vertCoundMinusOne] = (byte)minValue;
                    }
                }
            }

            showTexture(colliderLODMap, System.Environment.CurrentDirectory + "\\" + "bildbefore.png");

            // fit collider LOD map to collider scales = downscale low density colliders to higher density ones if the space is not enough (collider isnt complete)

            nextScale = byte.MaxValue;

            for (int i = m_distanceScale.Length; i > -1; i--)
            {
                currentScaleVerts = (int)Mathf.Pow(2, i) * vertCoundMinusOne;
                scaleParsed = (byte)i;

                if (i > 0)
                {
                    nextScale = (byte)(scaleParsed - 1);
                }

                for (int j = 0; j < playerViewGroupStart.Count; j++)
                {
                    for (int k = playerViewGroupStart[j].x; k < playerViewGroupEnd[j].x; k += currentScaleVerts)
                    {
                        maxInterationX = Mathf.Min(currentScaleVerts, playerViewGroupEnd[j].x - k);

                        for (int l = playerViewGroupStart[j].y; l < playerViewGroupEnd[j].y; l += currentScaleVerts)
                        {
                            maxInterationY = Mathf.Min(currentScaleVerts, playerViewGroupEnd[j].y - l);

                            downScale = false;

                            for (int m = 0; m < maxInterationX; m += vertCoundMinusOne)
                            {
                                for (int n = 0; n < maxInterationY; n += vertCoundMinusOne)
                                {
                                    if (colliderLODMap[(k + m) / vertCoundMinusOne, (l + n) / vertCoundMinusOne] < scaleParsed)
                                    {
                                        downScale = true;
                                        goto DONWSCALE_START; // break 2 loops
                                    }
                                }
                            }

                        DONWSCALE_START:

                            if (downScale)
                            {
                                for (int m = 0; m < maxInterationX; m += vertCoundMinusOne)
                                {
                                    for (int n = 0; n < maxInterationY; n += vertCoundMinusOne)
                                    {
                                        //Debug.Log("k + m: " + (k + m) + "; l + n: " + (l + n) + "; m: " + m + "; n: " + n + "; currentScaleVerts: " + currentScaleVerts);
                                        if (colliderLODMap[(k + m) / vertCoundMinusOne, (l + n) / vertCoundMinusOne] > nextScale)
                                        {
                                            colliderLODMap[(k + m) / vertCoundMinusOne, (l + n) / vertCoundMinusOne] = nextScale;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            showTexture(colliderLODMap, System.Environment.CurrentDirectory + "\\" + "bild.png");

            // create needed objects

            objectCreatePositions.Clear();
            objectCreateScale.Clear();

            for (int i = 0; i < m_distanceScale.Length; i++)
            {
                currentScaleVerts = (int)Mathf.Pow(2, i);
                scaleParsed = (byte)i;

                for (int j = 0; j < colliderLODMap.GetLength(0); j += currentScaleVerts)
                {
                    for (int k = 0; k < colliderLODMap.GetLength(1); k += currentScaleVerts)
                    {
                        if (colliderLODMap[j, k] == scaleParsed)
                        {
                            objectCreatePositions.Add(new Vector2Int(j * vertCoundMinusOne, k * vertCoundMinusOne));
                            objectCreateScale.Add(i);
                        }
                    }
                }
            }

            //Debug.Log("objectCreatePositions.count: " + objectCreatePositions.Count);

            // compare to find new and obsolete objects

            // new objects
            for (int i = 0; i < objectCreatePositions.Count; i++)
            {
                foundEntry = false;

                for (int j = 0; j < lastObjectCreatePositions.Count; j++)
                {
                    if (objectCreatePositions[i] == lastObjectCreatePositions[j])
                    {
                        if (objectCreateScale[i] == lastObjectCreateScale[j])
                        {
                            foundEntry = true;
                            break;
                        }
                    }
                }

                if (!foundEntry)
                {
                    m_newObjectsPos.Add(objectCreatePositions[i]);
                    m_newObjectsScale.Add(objectCreateScale[i]);
                }
            }

            // obsolete objects
            for (int i = 0; i < lastObjectCreatePositions.Count; i++)
            {
                foundEntry = false;

                for (int j = 0; j < objectCreatePositions.Count; j++)
                {
                    if (objectCreatePositions[j] == lastObjectCreatePositions[i])
                    {
                        if (objectCreateScale[j] == lastObjectCreateScale[i])
                        {
                            foundEntry = true;
                            break;
                        }
                    }
                }

                if (!foundEntry)
                {
                    m_obsoleteObjectsPos.Add(lastObjectCreatePositions[i]);
                    m_obsoleteObjectsScale.Add(lastObjectCreateScale[i]);
                }
            }

            lastObjectCreatePositions.Clear();
            lastObjectCreateScale.Clear();

            lastObjectCreatePositions.AddRange(objectCreatePositions);
            lastObjectCreateScale.AddRange(objectCreateScale);

            //Debug.Log("found " + m_newObjectsPos.Count + " new objects");
            //Debug.Log("found " + m_obsoleteObjectsPos.Count + " obsolete objects");

            for (int i = 0; i < m_newObjectsPos.Count; i++)
            {
                WorldMeshData meshData = createMeshData(m_newObjectsPos[i], m_newObjectsScale[i], MeshConnectionBorder.none, false, true);

                if (meshData != null)
                {
                    hashBytes.Clear();

                    hashBytes.AddRange(System.BitConverter.GetBytes(m_newObjectsPos[i].x));
                    hashBytes.AddRange(System.BitConverter.GetBytes(m_newObjectsPos[i].y));
                    hashBytes.AddRange(System.BitConverter.GetBytes(m_newObjectsScale[i]));

                    meshData.m_ID = System.BitConverter.ToInt32(hashAlgorithm.ComputeHash(hashBytes.ToArray()), 0);

                    lock (m_readyWorldMeshData)
                    {
                        m_readyWorldMeshData.Add(meshData);
                    }
                }
            }

            m_newObjectsPos.Clear();
            m_newObjectsScale.Clear();

            for (int i = 0; i < m_obsoleteObjectsScale.Count; i++)
            {
                hashBytes.Clear();

                hashBytes.AddRange(System.BitConverter.GetBytes(m_obsoleteObjectsPos[i].x));
                hashBytes.AddRange(System.BitConverter.GetBytes(m_obsoleteObjectsPos[i].y));
                hashBytes.AddRange(System.BitConverter.GetBytes(m_obsoleteObjectsScale[i]));

                hashValue = System.BitConverter.ToInt32(hashAlgorithm.ComputeHash(hashBytes.ToArray()), 0);

                lock (m_obsoleteObjectsID)
                {
                    m_obsoleteObjectsID.Add(hashValue);
                }
            }

            m_obsoleteObjectsPos.Clear();
            m_obsoleteObjectsScale.Clear();

            lock (m_startThreadMRE)
            {
                m_startThreadMRE.Reset();
                Interlocked.Exchange(ref m_LOCKED_isDone, 1);
            }

            //Debug.Log("HeightmapRendererCollider: calculateMeshesProcedure: finished compute: " + System.DateTime.Now.ToString());

#if !NO_MULTI_THREAD
        }
#endif
    }

    public void setPlayerMeshRadius(int playerMeshRadius)
    {
        if (isDone)
        {
            m_playerMeshRadiusVerts = playerMeshRadius * m_verticesCount;
        }
        else
        {
            throw new System.NotSupportedException("wait for \"isDone\" = true before setting player positions");
        }
    }

    public void setPlayerPositions(params Vector2[] positions)
    {
        if (isDone)
        {
            m_playerPositions.Clear();
            m_playerPositions.AddRange(positions);
        }
        else
        {
            throw new System.NotSupportedException("wait for \"isDone\" = true before setting player positions");
        }
    }
    public void setPlayerPositions(List<Vector2> positions)
    {
        if (isDone)
        {
            m_playerPositions.Clear();
            m_playerPositions.AddRange(positions);
        }
        else
        {
            throw new System.NotSupportedException("wait for \"isDone\" = true before setting player positions");
        }
    }

    public List<WorldMeshData> getAvailableWorldMeshData()
    {
        if (m_readyWorldMeshData.Count > 0)
        {
            List<WorldMeshData> data = new List<WorldMeshData>();

            lock (m_readyWorldMeshData)
            {
                data.AddRange(m_readyWorldMeshData);
                m_readyWorldMeshData.Clear();
            }

            return data;
        }
        else
        {
            return null;
        }
    }

    public List<int> getMeshesToRemoveHash()
    {
        if (m_obsoleteObjectsID.Count > 0)
        {
            List<int> data = new List<int>();

            lock (m_obsoleteObjectsID)
            {
                data.AddRange(m_obsoleteObjectsID);

                m_obsoleteObjectsID.Clear();
            }

            return data;
        }
        else
        {
            return null;
        }
    }

    public override void dispose()
    {
        Interlocked.Exchange(ref LOCKED_stopAllThreads, 1);

        base.dispose();

        if (m_computeMeshesThread != null)
        {
            m_computeMeshesThread.Abort();
            m_computeMeshesThread = null;
        }
    }

    private static void showTexture(byte[,] map, string savePath)
    {
#if !NO_MULTI_THREAD
        return;
#endif

        Texture2D DEBUG_Map;

        Color[] colors = new Color[map.GetLength(0) * map.GetLength(1)];

        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                colors[i + j * map.GetLength(0)] = new Color(Mathf.Max(1f - (float)map[i, j] / 5, 0), 0, 0);
            }
        }

        DEBUG_Map = new Texture2D(map.GetLength(0), map.GetLength(1));
        DEBUG_Map.SetPixels(colors);
        DEBUG_Map.Apply();

        SaveTextureAsPNG(DEBUG_Map, savePath);
    }

    private static void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
    {
        byte[] _bytes = _texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
        Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
    }
}
