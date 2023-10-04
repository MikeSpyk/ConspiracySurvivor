#define NO_MULTITHREADING
#undef NO_MULTITHREADING

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class WorldRasterizer
{
    // Thread-Class for creating the world raster

    public WorldRasterizer(int rasterDistance, byte[,] textureMap, int resourcesTypesCount)
    {
        m_rasterDistance = rasterDistance;
        m_textureMapRef = textureMap;
        m_resourcesTypesCount = resourcesTypesCount;
    }

    ~WorldRasterizer()
    {
        dispose();
    }

    private int m_rasterDistance = 0;
    private byte[,] m_textureMapRef = null;
    private int m_resourcesTypesCount = 0;
    private WorldRasterStack m_result = null;
    private Dictionary<int, WorldRasterField> m_fieldID_worldRasterField = null;

    private long LOCKED_isDone = 0;
    private long LOCKED_stopRequest = 0;
    private Thread m_mainThread = null;

    public void start()
    {
#if NO_MULTITHREADING
        createRaster();
        return;
#endif

        if (m_mainThread == null)
        {
            m_mainThread = new Thread(new ThreadStart(createRaster));
            m_mainThread.Start();
        }

    }

    private void createRaster()
    {
        int worldSizeX = m_textureMapRef.GetLength(0);
        int worldSizeY = m_textureMapRef.GetLength(1);

        int rasterCellCountX = worldSizeX / m_rasterDistance;

        if (worldSizeX % m_rasterDistance != 0)
        {
            rasterCellCountX++;
        }

        int rasterCellCountY = worldSizeY / m_rasterDistance;

        if (worldSizeY % m_rasterDistance != 0)
        {
            rasterCellCountY++;
        }

        //Debug.Log("WorldRasterizer: rasterCellCountX: " + rasterCellCountX + ", rasterCellCountY: " + rasterCellCountY);

        // prepare resource fields

        WorldRasterField[,] fields = new WorldRasterField[rasterCellCountX, rasterCellCountY];
        m_fieldID_worldRasterField = new Dictionary<int, WorldRasterField>();
        int fieldCounter = 0;

        for (int i = 0; i < rasterCellCountX; i++)
        {
            for (int j = 0; j < rasterCellCountY; j++)
            {
                int endPosX = Mathf.Min(i * m_rasterDistance + m_rasterDistance, worldSizeX);
                int endPosY = Mathf.Min(j * m_rasterDistance + m_rasterDistance, worldSizeY);

                FieldResources[] currentCellResources = new FieldResources[m_resourcesTypesCount]; // 0: trees, 1: lootBarrel, 2: berry plant

                for (int m = 0; m < currentCellResources.Length; m++)
                {
                    currentCellResources[m] = new FieldResources();
                    currentCellResources[m].computeCapacity((FieldResources.ResourceType)m, m_textureMapRef, i * m_rasterDistance, j * m_rasterDistance, endPosX, endPosY);
                }
                //Debug.Log("WorldRasterizer: res need: " + currentCellResources[0].m_need);
                //Debug.DrawRay(new Vector3(i * m_rasterDistance * WorldManager.singleton.getDefaultSubmeshVertDistance(), 0, j * m_rasterDistance * WorldManager.singleton.getDefaultSubmeshVertDistance()), Vector3.up * 100, Color.red, 100f);
                fields[i, j] = new WorldRasterField(i * m_rasterDistance, endPosX, j * m_rasterDistance, endPosY, currentCellResources, m_textureMapRef, fieldCounter);
                m_fieldID_worldRasterField.Add(fieldCounter, fields[i, j]);
                fieldCounter++;
            }
        }

        if (Interlocked.Read(ref LOCKED_stopRequest) == 1)
        {
            return;
        }

        // first stack instance

        int lastStageRasterCountX = rasterCellCountX;
        int lastStageRasterCountY = rasterCellCountY;

        if (rasterCellCountX % 2 != 0)
        {
            rasterCellCountX++;
        }

        if (rasterCellCountY % 2 != 0)
        {
            rasterCellCountY++;
        }

        rasterCellCountX /= 2;
        rasterCellCountY /= 2;

        WorldRasterStack[,] currentStackInstance = new WorldRasterStack[rasterCellCountX, rasterCellCountY];

        for (int i = 0; i < rasterCellCountX; i++)
        {
            for (int j = 0; j < rasterCellCountY; j++)
            {
                int startPosX = i * 2;
                int startPosY = j * 2;

                int endPosX = Mathf.Min(i * 2 + 2, lastStageRasterCountX);
                int endPosY = Mathf.Min(j * 2 + 2, lastStageRasterCountY);

                int loopLengthX = endPosX - startPosX;
                int loopLengthY = endPosY - startPosY;

                currentStackInstance[i, j] = new WorldRasterStack();

                FieldResourcesStack[] fielResourcesStack = new FieldResourcesStack[3]; // 0: trees, 1: random loot barrel, 2: berry plant
                for (int k = 0; k < fielResourcesStack.Length; k++)
                {
                    fielResourcesStack[k] = new FieldResourcesStack();
                }

                //Debug.Log("loopLengthX: " + loopLengthX + ", loopLengthY: " + loopLengthY);

                WorldRasterField[] worldFields = new WorldRasterField[loopLengthX * loopLengthY]; // x * y (mostly 2 * 2)

                for (int m = 0; m < fielResourcesStack.Length; m++)
                {
                    for (int k = 0; k < loopLengthX; k++) // goes for 2 (or less on border)
                    {
                        for (int l = 0; l < loopLengthY; l++) // goes for 2 (or less on border)
                        {
                            fielResourcesStack[m].m_need += fields[startPosX + k, startPosY + l].m_FieldResources[m].need;
                            worldFields[k + l * loopLengthX] = fields[startPosX + k, startPosY + l];
                            fields[startPosX + k, startPosY + l].setParent(currentStackInstance[i, j]);
                        }
                    }
                }

                currentStackInstance[i, j].initialise(fielResourcesStack, worldFields);
            }
        }

        // stack instances 

        WorldRasterStack[,] lastStackInstance;

        int stackCounter = 0; // debug

        while (rasterCellCountX / 2 > 0 && rasterCellCountY / 2 > 0 && Interlocked.Read(ref LOCKED_stopRequest) != 1)
        {
            lastStageRasterCountX = rasterCellCountX;
            lastStageRasterCountY = rasterCellCountY;

            if (rasterCellCountX % 2 != 0)
            {
                rasterCellCountX++;
            }

            if (rasterCellCountY % 2 != 0)
            {
                rasterCellCountY++;
            }

            rasterCellCountX /= 2;
            rasterCellCountY /= 2;

            lastStackInstance = currentStackInstance;
            currentStackInstance = new WorldRasterStack[rasterCellCountX, rasterCellCountY];

            for (int i = 0; i < rasterCellCountX; i++)
            {
                for (int j = 0; j < rasterCellCountY; j++)
                {
                    int startPosX = i * 2;
                    int startPosY = j * 2;

                    int endPosX = Mathf.Min(i * 2 + 2, lastStageRasterCountX);
                    int endPosY = Mathf.Min(j * 2 + 2, lastStageRasterCountY);

                    int loopLengthX = endPosX - startPosX;
                    int loopLengthY = endPosY - startPosY;

                    currentStackInstance[i, j] = new WorldRasterStack();

                    FieldResourcesStack[] fielResourcesStack = new FieldResourcesStack[3]; // 0: trees, 1: random loot barrel, 2: berry plant
                    for (int k = 0; k < fielResourcesStack.Length; k++)
                    {
                        fielResourcesStack[k] = new FieldResourcesStack();
                    }

                    //Debug.Log("loopLengthX: " + loopLengthX + ", loopLengthY: " + loopLengthY);

                    WorldRasterStack[] wordStacks = new WorldRasterStack[loopLengthX * loopLengthY]; // x * y (mostly 2 * 2)

                    for (int m = 0; m < fielResourcesStack.Length; m++)
                    {
                        for (int k = 0; k < loopLengthX; k++) // goes for 2 (or less on border)
                        {
                            for (int l = 0; l < loopLengthY; l++) // goes for 2 (or less on border)
                            {
                                fielResourcesStack[m].m_need += lastStackInstance[startPosX + k, startPosY + l].m_fieldResourcesStack[m].m_need;
                                wordStacks[k + l * loopLengthX] = lastStackInstance[startPosX + k, startPosY + l];
                                lastStackInstance[startPosX + k, startPosY + l].setParent(currentStackInstance[i, j]);
                            }
                        }
                    }

                    currentStackInstance[i, j].initialise(fielResourcesStack, wordStacks);
                }
            }

            stackCounter++;
        }

        m_result = currentStackInstance[0, 0];

        //Debug.Log("WorldRasterizer: Stacksize: " + (stackCounter + 2));

        // last steps

        Interlocked.Exchange(ref LOCKED_isDone, 1); // done
    }

    public void getResult(out WorldRasterStack mainStack, out Dictionary<int, WorldRasterField> fieldDict)
    {
        fieldDict = m_fieldID_worldRasterField;
        mainStack = m_result;
    }

    public bool checkIfDone()
    {
        if (Interlocked.Read(ref LOCKED_isDone) == 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void dispose()
    {
        Interlocked.Exchange(ref LOCKED_stopRequest, 1);

        if (m_mainThread != null)
        {
            m_mainThread.Abort();
            m_mainThread = null;
        }

        m_textureMapRef = null;
    }
}
