using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldChunkCluster
{
    private List<Vector3> m_voroniPoints = new List<Vector3>();
    private WorldChunk[,] m_wordChunks = null;

    private int m_chunkEdgeLength;

    public WorldChunkCluster(int sizeX, int sizeY, int chunkSize)
    {
        m_chunkEdgeLength = chunkSize;

        int clusterSizeX = sizeX / chunkSize;

        if(sizeX % chunkSize != 0)
        {
            clusterSizeX++;
        }

        int clusterSizeY = sizeY / chunkSize;

        if (sizeY % chunkSize != 0)
        {
            clusterSizeY++;
        }

        m_wordChunks = new WorldChunk[clusterSizeX, clusterSizeY];
    }

}
