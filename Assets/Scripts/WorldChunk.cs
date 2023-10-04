using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldChunk
{
    private WorldChunkState m_chunkState = WorldChunkState.Void;
    private float[,] m_heightMap = null;
    private short[,] m_textureMap = null;
    private List<WorldChunk> m_neighborChunks = null;
    private WorldChunkCluster m_worldChunkCluster = null;
    private Vector2Int m_originPosition;
    private List<VoronoiNode> m_voronoiNodes = null;

    public WorldChunkState chunkState { get { return m_chunkState; } }
    public float[,] heightMap { get { return m_heightMap; } }
    public short[,] textureMap { get { return m_textureMap; } }

    public WorldChunk(WorldChunkCluster parentCluster, List<WorldChunk> neighborChunks, List<VoronoiNode> voronoiNodes, Vector2Int originPosition)
    {
        m_worldChunkCluster = parentCluster;
        m_neighborChunks = neighborChunks;
        m_voronoiNodes = voronoiNodes;
        m_originPosition = originPosition;
    }

    public void createChunk()
    {
        // 1. create heightmap from voroni

        // 2. add perlin noise octaves to heightmap

        throw new System.NotImplementedException();
    }

    public void saveToDisk()
    {
        throw new System.NotImplementedException();
    }

    public void unload()
    {
        throw new System.NotImplementedException();
    }

    public bool loadFromDisk()
    {
        throw new System.NotImplementedException();
    }
}
