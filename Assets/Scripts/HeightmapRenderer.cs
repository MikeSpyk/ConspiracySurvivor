#define TRYCATCH
#undef TRYCATCH

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;

public class HeightmapRenderer
{
    public HeightmapRenderer(ShortCompressedFloat heightmap, byte[,] texturemap, float vertexDistance, int verticesPerEdge, byte texturesCount, byte undefinedTextureIndex, bool threadStartThread = true)
    {
        m_undefinedTextureIndex = undefinedTextureIndex;
        m_texturesCount = texturesCount;
        m_vertexDistance = vertexDistance;
        m_heightmap = heightmap;
        m_texturemap = texturemap;
        m_verticesCount = verticesPerEdge;
        m_heightmapSizeX = heightmap.GetLength(0);
        m_heightmapSizeY = heightmap.GetLength(1);
        m_heightmapMaxIteratorX = m_heightmapSizeX - 1;
        m_heightmapMaxIteratorY = m_heightmapSizeY - 1;
        m_LOCKED_isDone = 1;
        m_viewPoint = new Vector2Int(0, 0);
        m_texturesAverageCounters = new int[m_texturesCount];

        if (threadStartThread)
        {
            m_threadStarterThread = new Thread(new ThreadStart(threadStarterProcedure));
            m_threadStarterThread.Start();
        }
    }

    ~HeightmapRenderer()
    {
        dispose();
    }

    protected enum MeshConnectionBorder { right, left, top, bottom, lowerLeftCorner, upperLeftCorner, upperRightCorner, lowerRightCorner, none }

    protected int[] m_texturesAverageCounters;
    protected int m_texturesCount;
    protected byte m_undefinedTextureIndex;
    protected float m_maxMeshDistance;
    protected float m_vertexDistance;
    protected ShortCompressedFloat m_heightmap;
    protected byte[,] m_texturemap;
    protected int m_heightmapSizeX;
    protected int m_heightmapSizeY;
    protected int m_heightmapMaxIteratorX;
    protected int m_heightmapMaxIteratorY;
    protected long m_LOCKED_isDone;
    private long m_LOCKED_stopThreadStarterThread = 0;
    private Thread m_mainThread = null;
    private Thread m_threadStarterThread;
    protected ManualResetEvent m_startThreadMRE = new ManualResetEvent(false);
    protected Vector2Int m_viewPoint;
    private int[] m_submeshStagesCount; // how many submeshes for each quality level
    private int[] m_submeshStagesDiagonalStep; // how many meshes to go diagonal before beginning of stage calculation
    protected int m_verticesCount;
    private List<WorldMeshData> m_freeWorldMeshDataObjects = new List<WorldMeshData>();
    protected List<WorldMeshData> m_readyWorldMeshData = new List<WorldMeshData>();
    private float[] m_LODDistanceStages;

    public void recyleWorldMeshData(WorldMeshData objectToRecyle)
    {
        if (objectToRecyle == null)
        {
            Debug.LogError("HeightmapRenderer: recyleWorldMeshData: objectToRecyle is null");
            return;
        }

        objectToRecyle.m_highestVertex = float.MinValue;

        lock (m_freeWorldMeshDataObjects)
        {
            m_freeWorldMeshDataObjects.Add(objectToRecyle);
        }
    }

    /// <summary>
    /// The position on the heightmap, that represents the middle of the renderer-calculations.
    /// </summary>
    public Vector2Int currentPosition
    {
        get
        {
            return m_viewPoint;
        }
    }

    public bool isDone
    {
        get
        {
            if (Interlocked.Read(ref m_LOCKED_isDone) == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// start a new calculation step to calculate meshe-data around a point on the heightmap.
    /// </summary>
    /// <param name="viewerPosition">the position on the 2D-Heightmap-Array around which the meshes will get calculated</param>
    /// <param name="maxMeshDistance">the maximal (world) distance a mesh can reach to the viewerPosition before it gets ignored. All 4 corners of the mesh needed to be out of reach in order to get skipped</param>
    /// <param name="submeshStagesCount">how many mesh-objects will get drawn in each direction (x and y) of a stage</param>
    /// <param name="submeshStagesDiagonalStep">how many steps (with the size of a mesh) are taken in digonal direction, before starting to calculate meshes. this will be the lower left startpoint.</param>
    public void start(Vector2Int viewerPosition, float maxMeshDistance, int[] submeshStagesCount, int[] submeshStagesDiagonalStep, float[] LODTextureDistances)
    {
        if (isDone)
        {
            m_viewPoint = viewerPosition;
            m_LODDistanceStages = LODTextureDistances;

            m_maxMeshDistance = maxMeshDistance;
            m_submeshStagesCount = new int[submeshStagesCount.Length];
            for (int i = 0; i < submeshStagesCount.Length; i++)
            {
                m_submeshStagesCount[i] = submeshStagesCount[i];
            }

            m_submeshStagesDiagonalStep = new int[submeshStagesDiagonalStep.Length];
            for (int i = 0; i < submeshStagesDiagonalStep.Length; i++)
            {
                m_submeshStagesDiagonalStep[i] = submeshStagesDiagonalStep[i];
            }

            Interlocked.Exchange(ref m_LOCKED_isDone, 0);

            float rayHeight = 0;

            if (viewerPosition.x >= 0 && viewerPosition.y >= 0 && viewerPosition.x < m_heightmapSizeX && viewerPosition.y < m_heightmapSizeY)
            {
                rayHeight = m_heightmap[viewerPosition.x, viewerPosition.y];
            }

            //Debug.DrawRay(new Vector3(viewerPosition.x * m_vertexDistance, rayHeight, viewerPosition.y * m_vertexDistance), Vector3.up * 100, Color.green, 10);

            // no multithreading start
            //calculateMeshes();
            //return;
            // no multithreading end

            m_startThreadMRE.Set();
        }
        else
        {
            Debug.LogWarning("HeightmapRenderer: Attempt to start a new procedure while another one is still runnig.");
        }
    }

    /// <summary>
    /// gets the data, that the renderer has processed until now und clears the processed data
    /// </summary>
    /// <returns></returns>
    public List<WorldMeshData> collectReadyWorldMeshData()
    {
        List<WorldMeshData> returnValue = new List<WorldMeshData>();
        int count = m_readyWorldMeshData.Count;

        for (int i = 0; i < count; i++)
        {
            if (m_readyWorldMeshData[i] == null)
            {
                Debug.LogError("HeightmapRenderer: collectReadyWorldMeshData: found null object. skipping.");
            }
            else
            {
                returnValue.Add(m_readyWorldMeshData[i]);
            }
        }

        lock (m_readyWorldMeshData)
        {
            m_readyWorldMeshData.RemoveRange(0, count);
        }

        return returnValue;
    }

    public int readyWorldMeshDataCount
    {
        get
        {
            return m_readyWorldMeshData.Count;
        }
    }

    private void threadStarterProcedure()
    {
#if TRYCATCH
        try
        {
#endif
        while (Interlocked.Read(ref m_LOCKED_stopThreadStarterThread) == 0)
        {
            m_startThreadMRE.WaitOne();

            if (Interlocked.Read(ref m_LOCKED_stopThreadStarterThread) != 0)
            {
                break;
            }

            if (m_mainThread != null)
            {
                m_mainThread.Abort();
            }
            m_mainThread = new Thread(calculateMeshes);
            m_mainThread.Start();

            m_startThreadMRE.Reset();
        }
#if TRYCATCH
        }
        catch (Exception ex)
        {
            Debug.LogError("HeightmapRenderer: threadStarterProcedure failed: " + ex);
        }
#endif
    }

    private void calculateMeshes()
    {
#if TRYCATCH
        try
        {
#endif
        System.DateTime startTime = System.DateTime.Now;

        if (m_viewPoint == null)
        {
            Debug.LogError("HeightmapRenderer: m_viewPoint = null !");
            Interlocked.Exchange(ref m_LOCKED_isDone, 1);
            return;
        }

        if (m_submeshStagesCount == null)
        {
            Debug.LogError("HeightmapRenderer: m_submeshStagesCount = null !");
            Interlocked.Exchange(ref m_LOCKED_isDone, 1);
            return;
        }

        if (m_submeshStagesCount.Length == 0)
        {
            Debug.LogError("HeightmapRenderer: m_submeshStagesCount.Length = 0 !");
            Interlocked.Exchange(ref m_LOCKED_isDone, 1);
            return;
        }

        for (int i = 0; i < m_submeshStagesCount.Length; i++)
        {
            if (m_submeshStagesCount[i] % 2 != 0)
            {
                Debug.LogWarning("HeightmapRenderer: m_submeshStagesCount[" + i + "] is not divisible by 2. Increasing by 1...");
                m_submeshStagesCount[i] += 1;
            }
        }

        // iterate stages

        Vector2Int lastStageLowerLeft = Vector2Int.zero;
        Vector2Int lastStageUpperRight = Vector2Int.zero;

        Vector2Int lowerLeftStartPoint = m_viewPoint; // lower left startpoint of a stage

        Vector2Int currentPos;
        MeshConnectionBorder currentConnection;

        for (int i = 0; i < m_submeshStagesDiagonalStep.Length; i++) // for each stage
        {
            lowerLeftStartPoint -= new Vector2Int((m_verticesCount - 1), (m_verticesCount - 1)) * m_submeshStagesDiagonalStep[i] * (int)Mathf.Pow(2, i);

            for (int j = 0; j < m_submeshStagesCount[i]; j++) // render square for this stage x
            {
                for (int k = 0; k < m_submeshStagesCount[i]; k++) // render square for this stage y
                {
                    currentPos = lowerLeftStartPoint + new Vector2Int((m_verticesCount - 1) * j, (m_verticesCount - 1) * k) * (int)Mathf.Pow(2, i);

                    if (
                            // if not within last stages square
                            i == 0 ||
                            currentPos.x < lastStageLowerLeft.x || currentPos.x >= lastStageUpperRight.x ||
                            currentPos.y < lastStageLowerLeft.y || currentPos.y >= lastStageUpperRight.y
                        )
                    {
                        currentConnection = MeshConnectionBorder.none;

                        if (j == 0)
                        {
                            currentConnection = MeshConnectionBorder.left;
                            if (k == 0)
                            {
                                currentConnection = MeshConnectionBorder.lowerLeftCorner;
                            }
                            else if (k == m_submeshStagesCount[i] - 1)
                            {
                                currentConnection = MeshConnectionBorder.upperLeftCorner;
                            }
                        }
                        else if (j == m_submeshStagesCount[i] - 1)
                        {
                            currentConnection = MeshConnectionBorder.right;
                            if (k == 0)
                            {
                                currentConnection = MeshConnectionBorder.lowerRightCorner;
                            }
                            else if (k == m_submeshStagesCount[i] - 1)
                            {
                                currentConnection = MeshConnectionBorder.upperRightCorner;
                            }
                        }
                        else if (k == 0)
                        {
                            currentConnection = MeshConnectionBorder.bottom;
                        }
                        else if (k == m_submeshStagesCount[i] - 1)
                        {
                            currentConnection = MeshConnectionBorder.top;
                        }

                        WorldMeshData meshData = createMeshData(currentPos, i, currentConnection);

                        if (meshData != null)
                        {
                            lock (m_readyWorldMeshData)
                            {
                                m_readyWorldMeshData.Add(meshData);
                            }
                        }
                    }
                }
            }

            lastStageLowerLeft = lowerLeftStartPoint;
            lastStageUpperRight = lastStageLowerLeft + new Vector2Int(m_verticesCount - 1, m_verticesCount - 1) * m_submeshStagesCount[i] * (int)Mathf.Pow(2, i);
        }

        //createMeshData(lowerLeftStartPoint + new Vector2Int(m_verticesCount * j, m_verticesCount * k) * (int)Mathf.Pow(2, i), i);
        //lowerLeftStartPoint += new Vector2Int(m_verticesCount, m_verticesCount) * m_submeshStagesDiagonalStep[i] * (int)Mathf.Pow(2, i);

        //Debug.Log("HeightmapRenderer: Finished after " + (System.DateTime.Now - startTime).Milliseconds + " millisecounds");
        Interlocked.Exchange(ref m_LOCKED_isDone, 1);

#if TRYCATCH
        }
        catch (Exception ex)
        {
            Debug.LogError("HeightmapRenderer: Exception Error: " + ex);
            Interlocked.Exchange(ref m_LOCKED_isDone, 1);
        }
#endif

    }

    protected WorldMeshData createMeshData(Vector2Int lowerLeftStartPoint, int scalingStage, MeshConnectionBorder borderConnection, bool viewPointDistanceCheck = true, bool colliderOnly = false)
    {
        int stepWidth = (int)Mathf.Pow(2, scalingStage);
        int stepWidthHalf = stepWidth / 2;

        // check if too far away

        Vector2 viewPointPosWorld = new Vector2(m_viewPoint.x, m_viewPoint.y) * m_vertexDistance;

        Vector2 meshPosWorldLowerLeft = new Vector2(lowerLeftStartPoint.x, lowerLeftStartPoint.y) * m_vertexDistance;
        Vector2 meshPosWorldUpperLeft = meshPosWorldLowerLeft + (new Vector2(0, m_verticesCount) * m_vertexDistance * stepWidth);
        Vector2 meshPosWorldLowerRight = meshPosWorldLowerLeft + (new Vector2(m_verticesCount, 0) * m_vertexDistance * stepWidth);
        Vector2 meshPosWorldUpperRight = meshPosWorldLowerLeft + (new Vector2(m_verticesCount, m_verticesCount) * m_vertexDistance * stepWidth);

        if (
            viewPointDistanceCheck &&
            Vector2.Distance(viewPointPosWorld, meshPosWorldLowerLeft) > m_maxMeshDistance &&
            Vector2.Distance(viewPointPosWorld, meshPosWorldUpperLeft) > m_maxMeshDistance &&
            Vector2.Distance(viewPointPosWorld, meshPosWorldLowerRight) > m_maxMeshDistance &&
            Vector2.Distance(viewPointPosWorld, meshPosWorldUpperRight) > m_maxMeshDistance
            )
        {
            return null;
        }

        // get free obj or create new one
        WorldMeshData currentMeshData;

        if (m_freeWorldMeshDataObjects.Count > 0)
        {
            currentMeshData = m_freeWorldMeshDataObjects[0];
            lock (m_freeWorldMeshDataObjects)
            {
                m_freeWorldMeshDataObjects.RemoveAt(0);
            }
        }
        else
        {
            currentMeshData = new WorldMeshData(m_verticesCount);
        }

        if (currentMeshData == null)
        {
            Debug.LogError("HeightmapRenderer: currentMeshData = null");
            currentMeshData = new WorldMeshData(m_verticesCount);
            if (currentMeshData == null)
            {
                Debug.LogError("HeightmapRenderer: creating new WorldMeshData failed: m_verticesCount = " + m_verticesCount);
            }
        }

        // values for this stage

        currentMeshData.m_objectWorldPosition = new Vector3(
                                                            (lowerLeftStartPoint.x + (m_verticesCount / 2) * stepWidth) * m_vertexDistance,
                                                            getAveragedHeightmapHeight(lowerLeftStartPoint.x + (m_verticesCount / 2), lowerLeftStartPoint.y + (m_verticesCount / 2), stepWidthHalf),
                                                            (lowerLeftStartPoint.y + (m_verticesCount / 2) * stepWidth) * m_vertexDistance
                                                            );

        currentMeshData.m_vertexDistance = m_vertexDistance * stepWidth;
        currentMeshData.m_lowerLeftHeightmapPos = lowerLeftStartPoint;

        float distanceToPlayer;

        if (m_viewPoint.x < 0 || m_viewPoint.x >= m_heightmap.GetLength(0) || m_viewPoint.y < 0 || m_viewPoint.y >= m_heightmap.GetLength(1))
        {
            distanceToPlayer = Vector3.Distance(new Vector3(m_viewPoint.x * m_vertexDistance, 0f, m_viewPoint.y * m_vertexDistance), currentMeshData.m_objectWorldPosition);
        }
        else
        {
            distanceToPlayer = Vector3.Distance(new Vector3(m_viewPoint.x * m_vertexDistance, m_heightmap[m_viewPoint.x, m_viewPoint.y], m_viewPoint.y * m_vertexDistance), currentMeshData.m_objectWorldPosition);
        }

        if (colliderOnly)
        {
            currentMeshData.m_LODLevel = scalingStage;
        }
        else
        {
            for (int i = 0; i < m_LODDistanceStages.Length; i++)
            {
                if (distanceToPlayer < m_LODDistanceStages[i])
                {
                    currentMeshData.m_LODLevel = i;
                    break;
                }
            }
        }

        // calculate vertices, normals, UVs

        float normals_HeightX;
        float normals_HeightA;
        float normals_HeightB;
        float normals_HeightC;
        float normals_HeightD;

        Vector3 normals_XA = new Vector3(0, 0, -1) * m_vertexDistance * stepWidth;
        Vector3 normals_XB = new Vector3(1, 0, 0) * m_vertexDistance * stepWidth;
        Vector3 normals_XC = new Vector3(0, 0, 1) * m_vertexDistance * stepWidth;
        Vector3 normals_XD = new Vector3(-1, 0, 0) * m_vertexDistance * stepWidth;

        Vector3 normals_Face1;
        Vector3 normals_Face2;
        Vector3 normals_Face3;
        Vector3 normals_Face4;

        int posX;
        int posY;
        for (int i = 0; i < m_verticesCount; i++)
        {
            for (int j = 0; j < m_verticesCount; j++)
            {
                posX = lowerLeftStartPoint.x + i * stepWidth;
                posY = lowerLeftStartPoint.y + j * stepWidth;

                // UV

                currentMeshData.m_UVs[i + j * m_verticesCount] = new Vector2(1f * i / m_verticesCount, 1f * j / m_verticesCount);

                // vertices and textures

                currentMeshData.m_vertices[i + j * m_verticesCount].x = posX * m_vertexDistance;
                currentMeshData.m_vertices[i + j * m_verticesCount].z = posY * m_vertexDistance;

                if (posX < 0 || posX > m_heightmapMaxIteratorX || posY < 0 || posY > m_heightmapMaxIteratorY) // if out of heihgtmap bounds
                {
                    currentMeshData.m_vertices[i + j * m_verticesCount].y = 0; // vertex
                    currentMeshData.m_textures[i + j * m_verticesCount] = m_undefinedTextureIndex; // texture
                }
                else
                {
                    currentMeshData.m_vertices[i + j * m_verticesCount].y = getAveragedHeightmapHeight(posX, posY, stepWidthHalf); // vertex
                    //currentMeshData.m_vertices[i + j * m_verticesCount].y = m_heightmap[posX, posY]; // vertex
                    //currentMeshData.m_textures[i + j * m_verticesCount] = m_texturemap[posX, posY]; // texture
                    currentMeshData.m_textures[i + j * m_verticesCount] = getAverageTextures(posX, posY, stepWidthHalf); // texture
                }

                currentMeshData.m_vertices[i + j * m_verticesCount] -= currentMeshData.m_objectWorldPosition;

                currentMeshData.m_highestVertex = Mathf.Max(currentMeshData.m_highestVertex, currentMeshData.m_vertices[i + j * m_verticesCount].y);

                // normals

                /*
                 * 			C
                 * 	D		X		B
                 * 			A
                 */

                normals_HeightX = currentMeshData.m_vertices[i + j * m_verticesCount].y;

                // normal: height A

                posX = lowerLeftStartPoint.x + i * stepWidth;
                posY = lowerLeftStartPoint.y + j * stepWidth - stepWidth;

                if (posX < 0 || posX > m_heightmapMaxIteratorX || posY < 0 || posY > m_heightmapMaxIteratorY) // if out of heihgtmap bounds
                {
                    normals_HeightA = 0;
                }
                else
                {
                    normals_HeightA = m_heightmap[posX, posY];
                }

                // normal: height B

                posX = lowerLeftStartPoint.x + i * stepWidth + stepWidth;
                posY = lowerLeftStartPoint.y + j * stepWidth;

                if (posX < 0 || posX > m_heightmapMaxIteratorX || posY < 0 || posY > m_heightmapMaxIteratorY) // if out of heihgtmap bounds
                {
                    normals_HeightB = 0;
                }
                else
                {
                    normals_HeightB = m_heightmap[posX, posY];
                }

                // normal: height C

                posX = lowerLeftStartPoint.x + i * stepWidth;
                posY = lowerLeftStartPoint.y + j * stepWidth + stepWidth;

                if (posX < 0 || posX > m_heightmapMaxIteratorX || posY < 0 || posY > m_heightmapMaxIteratorY) // if out of heihgtmap bounds
                {
                    normals_HeightC = 0;
                }
                else
                {
                    normals_HeightC = m_heightmap[posX, posY];
                }

                // normal: height D

                posX = lowerLeftStartPoint.x + i * stepWidth - stepWidth;
                posY = lowerLeftStartPoint.y + j * stepWidth;

                if (posX < 0 || posX > m_heightmapMaxIteratorX || posY < 0 || posY > m_heightmapMaxIteratorY) // if out of heihgtmap bounds
                {
                    normals_HeightD = 0;
                }
                else
                {
                    normals_HeightD = m_heightmap[posX, posY];
                }

                // normal: rectangle vectors

                normals_XA.y = normals_HeightA - normals_HeightX;
                normals_XB.y = normals_HeightB - normals_HeightX;
                normals_XC.y = normals_HeightC - normals_HeightX;
                normals_XD.y = normals_HeightD - normals_HeightX;

                // normal: face normals

                normals_Face1 = Vector3.Cross(normals_XB, normals_XA);
                normals_Face2 = Vector3.Cross(normals_XC, normals_XB);
                normals_Face3 = Vector3.Cross(normals_XD, normals_XC);
                normals_Face4 = Vector3.Cross(normals_XA, normals_XD);

                currentMeshData.m_normals[i + j * m_verticesCount] = (normals_Face1 + normals_Face2 + normals_Face3 + normals_Face4).normalized;

                //Debug.DrawRay(currentMeshData.m_vertices[i + j * m_verticesCount], currentMeshData.m_normals[i + j * m_verticesCount] * 2, Color.red, 100f);
            }
        }

        // flatten borders to connected meshes with double vertexDistance

        if (borderConnection == MeshConnectionBorder.left || borderConnection == MeshConnectionBorder.lowerLeftCorner || borderConnection == MeshConnectionBorder.upperLeftCorner)
        {
            for (int i = 1; i < m_verticesCount; i += 2)
            {
                currentMeshData.m_vertices[m_verticesCount * i].y = (getAveragedHeightmapHeight(lowerLeftStartPoint.x, lowerLeftStartPoint.y + (i - 1) * stepWidth, stepWidth) + getAveragedHeightmapHeight(lowerLeftStartPoint.x, lowerLeftStartPoint.y + (i + 1) * stepWidth, stepWidth)) / 2 - currentMeshData.m_objectWorldPosition.y; // stepwith * 2 (:nextstage) / 2 (:half) = stepwith
            }
            for (int i = 0; i < m_verticesCount; i += 2)
            {
                currentMeshData.m_vertices[m_verticesCount * i].y = getAveragedHeightmapHeight(lowerLeftStartPoint.x, lowerLeftStartPoint.y + i * stepWidth, stepWidth) - currentMeshData.m_objectWorldPosition.y; // stepwith * 2 (:nextstage) / 2 (:half) = stepwith
            }
        }

        if (borderConnection == MeshConnectionBorder.right || borderConnection == MeshConnectionBorder.lowerRightCorner || borderConnection == MeshConnectionBorder.upperRightCorner)
        {
            int xEnd = m_verticesCount - 1;
            for (int i = 1; i < m_verticesCount; i += 2)
            {
                currentMeshData.m_vertices[m_verticesCount * i + xEnd].y = (getAveragedHeightmapHeight(lowerLeftStartPoint.x + xEnd * stepWidth, lowerLeftStartPoint.y + (i - 1) * stepWidth, stepWidth) + getAveragedHeightmapHeight(lowerLeftStartPoint.x + xEnd * stepWidth, lowerLeftStartPoint.y + (i + 1) * stepWidth, stepWidth)) / 2 - currentMeshData.m_objectWorldPosition.y; // stepwith * 2 (:nextstage) / 2 (:half) = stepwith
            }
            for (int i = 0; i < m_verticesCount; i += 2)
            {
                currentMeshData.m_vertices[m_verticesCount * i + xEnd].y = getAveragedHeightmapHeight(lowerLeftStartPoint.x + xEnd * stepWidth, lowerLeftStartPoint.y + i * stepWidth, stepWidth) - currentMeshData.m_objectWorldPosition.y; // stepwith * 2 (:nextstage) / 2 (:half) = stepwith
            }
        }

        if (borderConnection == MeshConnectionBorder.bottom || borderConnection == MeshConnectionBorder.lowerLeftCorner || borderConnection == MeshConnectionBorder.lowerRightCorner)
        {
            for (int i = 1; i < m_verticesCount; i += 2)
            {
                currentMeshData.m_vertices[i].y = (getAveragedHeightmapHeight(lowerLeftStartPoint.x + (i - 1) * stepWidth, lowerLeftStartPoint.y, stepWidth) + getAveragedHeightmapHeight(lowerLeftStartPoint.x + (i + 1) * stepWidth, lowerLeftStartPoint.y, stepWidth)) / 2 - currentMeshData.m_objectWorldPosition.y; // stepwith * 2 (:nextstage) / 2 (:half) = stepwith
            }
            for (int i = 0; i < m_verticesCount; i += 2)
            {
                currentMeshData.m_vertices[i].y = getAveragedHeightmapHeight(lowerLeftStartPoint.x + i * stepWidth, lowerLeftStartPoint.y, stepWidth) - currentMeshData.m_objectWorldPosition.y; // stepwith * 2 (:nextstage) / 2 (:half) = stepwith
            }
        }

        if (borderConnection == MeshConnectionBorder.top || borderConnection == MeshConnectionBorder.upperRightCorner || borderConnection == MeshConnectionBorder.upperLeftCorner)
        {
            int yEnd = m_verticesCount - 1;

            for (int i = 1; i < m_verticesCount; i += 2)
            {
                currentMeshData.m_vertices[i + yEnd * m_verticesCount].y = (getAveragedHeightmapHeight(lowerLeftStartPoint.x + (i - 1) * stepWidth, lowerLeftStartPoint.y + yEnd * stepWidth, stepWidth) + getAveragedHeightmapHeight(lowerLeftStartPoint.x + (i + 1) * stepWidth, lowerLeftStartPoint.y + yEnd * stepWidth, stepWidth)) / 2 - currentMeshData.m_objectWorldPosition.y; // stepwith * 2 (:nextstage) / 2 (:half) = stepwith
            }
            for (int i = 0; i < m_verticesCount; i += 2)
            {
                currentMeshData.m_vertices[i + yEnd * m_verticesCount].y = getAveragedHeightmapHeight(lowerLeftStartPoint.x + i * stepWidth, lowerLeftStartPoint.y + yEnd * stepWidth, stepWidth) - currentMeshData.m_objectWorldPosition.y; // stepwith * 2 (:nextstage) / 2 (:half) = stepwith
            }
        }

        // calculate triangles

        for (int i = 0; i < m_verticesCount * m_verticesCount * 6; i++)
        {
            currentMeshData.m_triangleTest[i] = -1;
        }

        int count = 0;
        for (int i = 0; i < m_verticesCount - 1; i++)
        {
            for (int j = 0; j < m_verticesCount - 1; j++)
            {
                currentMeshData.m_triangleTest[count] = j + m_verticesCount * i;
                count++;
                currentMeshData.m_triangleTest[count] = j + 1 + m_verticesCount * (i + 1);
                count++;
                currentMeshData.m_triangleTest[count] = j + 1 + m_verticesCount * i;
                count++;
            }
        }
        for (int i = 0; i < m_verticesCount - 1; i++)
        {
            for (int j = 0; j < m_verticesCount - 1; j++)
            {
                currentMeshData.m_triangleTest[count] = j + m_verticesCount * i;
                count++;
                currentMeshData.m_triangleTest[count] = j + m_verticesCount * (i + 1);
                count++;
                currentMeshData.m_triangleTest[count] = j + 1 + m_verticesCount * (i + 1);
                count++;
            }
        }

        for (int i = 0; i < currentMeshData.m_triangles.Length; i++)
        {
            currentMeshData.m_triangles[i] = currentMeshData.m_triangleTest[i];
        }

        // calculate tangents

        //variable definitions
        int triangleCount = currentMeshData.m_triangles.Length;
        int vertexCount = currentMeshData.m_vertices.Length;

        Vector3[] tan1 = new Vector3[vertexCount];
        Vector3[] tan2 = new Vector3[vertexCount];

        for (long a = 0; a < triangleCount; a += 3)
        {
            long i1 = currentMeshData.m_triangles[a + 0];
            long i2 = currentMeshData.m_triangles[a + 1];
            long i3 = currentMeshData.m_triangles[a + 2];

            Vector3 v1 = currentMeshData.m_vertices[i1];
            Vector3 v2 = currentMeshData.m_vertices[i2];
            Vector3 v3 = currentMeshData.m_vertices[i3];

            Vector2 w1 = currentMeshData.m_UVs[i1];
            Vector2 w2 = currentMeshData.m_UVs[i2];
            Vector2 w3 = currentMeshData.m_UVs[i3];

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
            Vector3 n = currentMeshData.m_normals[a];
            Vector3 t = tan1[a];

            //Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
            //tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
            Vector3.OrthoNormalize(ref n, ref t);
            currentMeshData.m_tangents[a].x = t.x;
            currentMeshData.m_tangents[a].y = t.y;
            currentMeshData.m_tangents[a].z = t.z;

            currentMeshData.m_tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
        }

        // done

        if (currentMeshData == null)
        {
            Debug.LogError("HeightmapRenderer: created currentMeshData = null");
            return null;
        }

        return currentMeshData;

        /*
        
        Vector3 startPoint = new Vector3(lowerLeftStartPoint.x * m_vertexDistance, 0, lowerLeftStartPoint.y * m_vertexDistance);
        Vector3 endPoint = new Vector3((lowerLeftStartPoint.x + m_verticesCount * (int)Mathf.Pow(2, scalingStage)) * m_vertexDistance, 0, (lowerLeftStartPoint.y + m_verticesCount * (int)Mathf.Pow(2, scalingStage)) * m_vertexDistance);

        Color colorToUse;

        //colorToUse = new Color((1f * scalingStage) / m_submeshStagesCount.Length, 0, 0);

        if(scalingStage == 0)
        {
            colorToUse = Color.red;
        }
        else if (scalingStage == 1)
        {
            colorToUse = Color.blue;
        }
        else if (scalingStage  == 2)
        {
            colorToUse = Color.green;
        }
        else
        {
            colorToUse = Color.yellow;
        }

        if(borderConnection != MeshConnectionBorder.none)
        {
            Debug.DrawRay(startPoint + Vector3.up * 50, Vector3.up * 10, Color.white, 100f);
        }

        Debug.DrawLine(startPoint, endPoint, colorToUse, 100f);
        Debug.DrawRay(startPoint, Vector3.up * 50, colorToUse, 100f);
        */

    }

    private float getAveragedHeightmapHeight(int startPosX, int startPosY, int stepWidthHalf)
    {
        if (stepWidthHalf < 1)
        {
            // no average needed
            if (startPosX < 0 || startPosX > m_heightmapMaxIteratorX || startPosY < 0 || startPosY > m_heightmapMaxIteratorY)
            {
                return 0;
            }
            else
            {
                return m_heightmap[startPosX, startPosY];
            }
        }
        else
        {
            int endPosX = startPosX + stepWidthHalf;
            int endPosY = startPosY + stepWidthHalf;
            float average = 0;

            for (int i = startPosX - stepWidthHalf; i < endPosX; i++)
            {
                for (int j = startPosY - stepWidthHalf; j < endPosY; j++)
                {
                    if (i < 0 || i > m_heightmapMaxIteratorX || j < 0 || j > m_heightmapMaxIteratorY)
                    {
                        // average += 0
                    }
                    else
                    {
                        average += m_heightmap[i, j];
                    }
                }
            }

            return average / ((stepWidthHalf * 2) * (stepWidthHalf * 2));
        }
    }

    private byte getAverageTextures(int startPosX, int startPosY, int stepWidthHalf)
    {
        if (stepWidthHalf < 1)
        {
            // no average needed
            if (startPosX < 0 || startPosX > m_heightmapMaxIteratorX || startPosY < 0 || startPosY > m_heightmapMaxIteratorY)
            {
                return m_undefinedTextureIndex;
            }
            else
            {
                return m_texturemap[startPosX, startPosY];
            }
        }
        else
        {
            int endPosX = startPosX + stepWidthHalf;
            int endPosY = startPosY + stepWidthHalf;

            for (int i = startPosX - stepWidthHalf; i < endPosX; i++)
            {
                for (int j = startPosY - stepWidthHalf; j < endPosY; j++)
                {
                    if (i < 0 || i > m_heightmapMaxIteratorX || j < 0 || j > m_heightmapMaxIteratorY)
                    {
                        m_texturesAverageCounters[m_undefinedTextureIndex]++;
                    }
                    else
                    {
                        m_texturesAverageCounters[m_texturemap[i, j]]++;
                    }
                }
            }

            int highestCounterCount = 0;
            int highestCounterTextureIndex = m_undefinedTextureIndex;

            for (int i = 0; i < m_texturesCount; i++)
            {
                if (m_texturesAverageCounters[i] > highestCounterCount)
                {
                    highestCounterCount = m_texturesAverageCounters[i];
                    highestCounterTextureIndex = i;
                }

                m_texturesAverageCounters[i] = 0;
            }

            return (byte)highestCounterTextureIndex;
        }
    }

    public virtual void dispose()
    {
        Interlocked.Exchange(ref m_LOCKED_stopThreadStarterThread, 1);

        if (m_startThreadMRE != null)
        {
            m_startThreadMRE.Set();
        }

        if (m_mainThread != null)
        {
            m_mainThread.Abort();
        }

        if (m_threadStarterThread != null)
        {
            m_threadStarterThread.Abort();
        }

        if (m_startThreadMRE != null)
        {
            m_startThreadMRE.Dispose();
            m_startThreadMRE = null;
        }
    }

}
