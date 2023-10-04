using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoronoiMapMaker : MonoBehaviour
{
    public static VoronoiMapMaker singelton;

    [Header("Debug")]
    [SerializeField] private Texture2D m_tex;
    [SerializeField] private bool m_create = false;
    [SerializeField] private bool m_writeToDisk = false;
    [SerializeField] private bool m_pureHeightMap = false;
    [Header("General")]
    [SerializeField] private AnimationCurve m_randomDistribution;
    [SerializeField] private float m_seed = 1340.3f;
    [SerializeField] int m_size = 200;
    [SerializeField] private int m_upscaleCount = 0;
    [Header("Voronoi Setting up")]
    [SerializeField] float m_pointDensity = 1; // points per 80 pixel
    [SerializeField] float m_hillsDensity = 0.001f;  // points per 80 pixel
    [SerializeField] private int m_voronoiDistributionRelaxing = 3; // will the points be in a random distance to each other or will they be homogeneously distributed
    [SerializeField] private int m_hillsCountOverride = 1; // how many hills will there be ? overrides [m_hillsDensity] if higher than 0
    [SerializeField] private float m_borderElevation = -100f;
    [SerializeField] private float m_middleElevation = 100f;
    [SerializeField] private float m_biomeHeightRandomness = 29f;
    //[SerializeField, Range(0, 1)] private float m_fadingStrength = 0.6f;
    [SerializeField] private float m_fadingHeight = 1;
    [SerializeField, Range(0, 1f)] private float m_maxMiddleOffset = 0.7f;
    [Header("Voronoi Height by Distance")]
    [SerializeField] private bool m_landmassHeightStep = true;
    [SerializeField] private float m_heightDistanceWater = 20f; // height added per tile-distance to water
    [SerializeField] private float m_biomeHeightRandomnessLandmass = 29f;
    [Header("Voronoi Smoothing")]
    [SerializeField] private int m_voronoiSmoothCount = 0;
    [Header("Heightmap smoothing")]
    [SerializeField] private int m_smoothWaterLandBorder = 10; // changes the border beetween water and land
    [SerializeField] private int m_smoothWaterLand = 10; // changes water and land without altering the border

    public float[,] m_heightmap = null;

    private void Awake()
    {
        singelton = this;
    }

    void Update()
    {
        if (m_create)
        {
            m_create = false;
            compute();
        }
    }

    public void compute()
    {
        List<Vector2> points = new List<Vector2>();
        List<uint> colors = new List<uint>();
        uint counter1 = 0;

        // create random points

        Debug.Log("create random points");

        int pointsCount = (int)((m_size * m_size / 80) * m_pointDensity);
        Debug.Log("pointsCount: " + pointsCount);

        for (int i = 0; i < pointsCount; i++)
        {
            points.Add(new Vector2(m_randomDistribution.Evaluate(Mathf.PerlinNoise((i + m_seed) * 0.1f, (i + m_seed + 1) * 0.2f)) * m_size, m_randomDistribution.Evaluate(Mathf.PerlinNoise((i + m_seed) * 0.3f, (i + m_seed + 1) * 0.4f)) * m_size));
            colors.Add(counter1);
            counter1++;
        }

        // remove same points

        Debug.Log("remove same points");

        int removedPointsCounter = 0;

        for (int i = 0; i < points.Count; i++)
        {
            for (int j = 0; j < points.Count; j++)
            {
                if (points[i].Equals(points[j]))
                {
                    if (i != j)
                    {
                        points.RemoveAt(j);
                        j--;
                        removedPointsCounter++;
                    }
                }
            }
        }

        if(removedPointsCounter > 0)
        {
            Debug.Log("VoronoiMapMaker: compute: removed " + removedPointsCounter + " identical random points");
        }

        // create voronoi

        Debug.Log("create voronoi");

        Rect bounds = new Rect(Vector2.zero, new Vector2(m_size, m_size));
        Delaunay.Voronoi voroni = new Delaunay.Voronoi(points, colors, bounds, m_voronoiDistributionRelaxing);

        //List<Delaunay.Geo.LineSegment> diagram = voroni.VoronoiDiagram();

        /*
        // draw lines
        for(int i = 0; i < diagram.Count; i++)
        {
            Color col = new Color((float)i / diagram.Count, (float)i / diagram.Count, (float)i / diagram.Count);
            Debug.DrawLine(Vec2ToVec3(diagram[i].p0), Vec2ToVec3(diagram[i].p1), col, 3f);
        }
        */

        // set up nodes

        Debug.Log("set up nodes");

        List<Vector2> nodeOrigins = voroni.SiteCoords();
        Dictionary<Vector2, VoronoiNode> nodeOrigin_Node = new Dictionary<Vector2, VoronoiNode>();
        List<VoronoiNode> biomeNodes = new List<VoronoiNode>();

        for (int i = 0; i < nodeOrigins.Count; i++)
        {
            if (nodeOrigins[i].x < 0 || nodeOrigins[i].x > m_size || nodeOrigins[i].y < 0 || nodeOrigins[i].y > m_size)
            {
                Debug.LogWarning("VoronoiMapMaker: compute: removed random point out of range: " + nodeOrigins[i].ToString());
                nodeOrigins.RemoveAt(i);
                i--;
            }
        }

        for (int i = 0; i < nodeOrigins.Count; i++)
        {
            VoronoiNode temp_node = new VoronoiNode(nodeOrigins[i]);
            nodeOrigin_Node.Add(nodeOrigins[i], temp_node);
            biomeNodes.Add(temp_node);
        }

        for (int i = 0; i < nodeOrigins.Count; i++)
        {
            List<Vector2> neighbors = voroni.NeighborSitesForSite(nodeOrigins[i]);
            for (int j = 0; j < neighbors.Count; j++)
            {
                biomeNodes[i].addNeighborNodes(nodeOrigin_Node[neighbors[j]]);
            }
        }

        // assign heightmapPoints to voronoi nodes

        Debug.Log("assign heightmapPoints to voronoi nodes");

        Vector2 site = Vec2_Q_ToVec2(voroni.NearestSitePoint(0, 0));
        VoronoiNode lastRowStartNode = nodeOrigin_Node[site];

        for (int i = 0; i < m_size; i++)
        {
            // find first node in row

            Vector2Int temp_currentPos2 = new Vector2Int(i, 0);
            float closestDistanceRow = Vector2.Distance(lastRowStartNode.origin, temp_currentPos2);

            List<VoronoiNode> rowNeighborNodes = lastRowStartNode.getNeighborBiomes();

            for (int j = 0; j < rowNeighborNodes.Count; j++)
            {
                float temp_distance2 = Vector2.Distance(rowNeighborNodes[j].origin, temp_currentPos2);

                if (temp_distance2 < closestDistanceRow)
                {
                    closestDistanceRow = temp_distance2;
                    lastRowStartNode = rowNeighborNodes[j];
                }
            }

            VoronoiNode lastNode = lastRowStartNode;
            lastNode.addContaningPoint(temp_currentPos2);

            VoronoiNode closestNode;

            // find node for evers pixel in row

            for (int j = 1; j < m_size; j++)
            {
                //Vector2 site = Vec2_Q_ToVec2(voroni.NearestSitePoint(i, j));
                //nodeOrigin_Node[site].addContaningPoint(new Vector2Int(i, j));

                Vector2Int temp_currentPos = new Vector2Int(i, j);

                closestNode = lastNode;
                float closestDistance = Vector2.Distance(closestNode.origin, temp_currentPos);

                List<VoronoiNode> neighborNodes = lastNode.getNeighborBiomes();

                for (int k = 0; k < neighborNodes.Count; k++)
                {
                    float temp_distance = Vector2.Distance(neighborNodes[k].origin, temp_currentPos);

                    if (temp_distance < closestDistance)
                    {
                        closestDistance = temp_distance;
                        closestNode = neighborNodes[k];
                    }
                }

                closestNode.addContaningPoint(temp_currentPos);
                lastNode = closestNode;
            }
        }

        // calculate nodes elevation

        Debug.Log("calculate nodes elevation");

        for (int i = 0; i < biomeNodes.Count; i++)
        {
            biomeNodes[i].checkIfBorder(m_size);
            if (biomeNodes[i].isBorder)
            {
                biomeNodes[i].elevation = m_borderElevation;
            }
            //Debug.Log("bimome: " + biomeNodes[i].ToString());
        }

        // calculate land/water distribution

        Debug.Log("calculate land/water distribution");

        int hillsCount = (int)((m_size * m_size / 80) * m_hillsDensity);
        Debug.Log("hillsCount: " + hillsCount);

        // find hills

        Debug.Log("find hills");

        if (m_hillsCountOverride > 0)
        {
            hillsCount = m_hillsCountOverride;
        }

        List<VoronoiNode> nodesOpenList = new List<VoronoiNode>();
        List<VoronoiNode> nodesClosedList = new List<VoronoiNode>();

        nodesOpenList.AddRange(biomeNodes);

        for (int i = 0; i < nodesOpenList.Count; i++)
        {
            if (nodesOpenList[i].isBorder)
            {
                nodesClosedList.Add(nodesOpenList[i]);
                nodesOpenList.Remove(nodesOpenList[i]);
                i--;
            }
        }

        for (int i = 0; i < hillsCount; i++)
        {
            float randomMiddleOffsetX = (m_randomDistribution.Evaluate(Mathf.PerlinNoise(m_seed + i * 10, m_seed + i * 10 + 10)) - 0.5f) * m_maxMiddleOffset * m_size;
            float randomMiddleOffsetY = (m_randomDistribution.Evaluate(Mathf.PerlinNoise(m_seed + i * 10 + 20, m_seed + i * 10 + 30)) - 0.5f) * m_maxMiddleOffset * m_size;

            Vector2 currentPos = new Vector2((m_size / 2) + randomMiddleOffsetX, (m_size / 2) + randomMiddleOffsetY);

            float closestDistance = float.MaxValue;
            int closestIndex = -1;

            for (int j = 0; j < nodesOpenList.Count; j++)
            {
                if (Vector2.Distance(currentPos, nodesOpenList[j].origin) < closestDistance)
                {
                    closestDistance = Vector2.Distance(currentPos, nodesOpenList[j].origin);
                    closestIndex = j;
                }
            }

            nodesOpenList[closestIndex].m_distanceToHill = 0;
            nodesOpenList[closestIndex].elevation = m_middleElevation;// * Mathf.PerlinNoise(seed + i * 11, seed + i * 11 + 10);
        }

        // calculate nodes height

        Debug.Log("calculate nodes height");

        int currentNodeStage = 0;

        int loopCounter1 = 0;
        const int BREAK_LOOP_COUNT = 10000;

        while (nodesOpenList.Count > 0)
        {
            loopCounter1++;
            if(loopCounter1 > BREAK_LOOP_COUNT)
            {
                Debug.LogWarning("VoronoiMapMaker: compute: too many interations. Missing Nodes: ");

                for(int i = 0; i< nodesOpenList.Count; i++)
                {
                    Debug.LogWarning("missing node: " + nodesOpenList[i].origin.ToString());
                }

                break;
            }

            for (int i = 0; i < nodesOpenList.Count; i++)
            {
                if (nodesOpenList[i].m_distanceToHill != null && nodesOpenList[i].m_distanceToHill == currentNodeStage)
                {
                    List<VoronoiNode> neighbors = nodesOpenList[i].getNeighborBiomes();

                    for (int j = 0; j < neighbors.Count; j++)
                    {
                        if (neighbors[j].m_distanceToHill == null && !neighbors[j].isBorder)
                        {
                            neighbors[j].m_distanceToHill = currentNodeStage + 1;
                            neighbors[j].elevation = nodesOpenList[i].elevation - m_fadingHeight + (Mathf.PerlinNoise(m_seed + i * nodesOpenList.Count * 0.1f, m_seed + i * nodesOpenList.Count * 0.2f) - 0.5f) * 2 * m_biomeHeightRandomness;
                        }
                    }

                    nodesOpenList.RemoveAt(i);
                    i--;
                }
            }

            currentNodeStage++;
        }

        /*

        List<VoronoiNode> biomesSortedDistanceMiddle = new List<VoronoiNode>();

        float randomMiddleOffsetX = (m_randomDistribution.Evaluate(Mathf.PerlinNoise(seed, seed + 10)) - 0.5f) * m_maxMiddleOffset * size;
        float randomMiddleOffsetY = (m_randomDistribution.Evaluate(Mathf.PerlinNoise(seed + 20, seed + 30)) - 0.5f) * m_maxMiddleOffset * size;

        Vector2 middle = new Vector2((size / 2) + randomMiddleOffsetX, (size / 2) + randomMiddleOffsetY);

        while (biomeNodes.Count > 0)
        {
            float closestDistance = float.MaxValue;
            int closestIndex = -1;

            for (int i = 0; i < biomeNodes.Count; i++)
            {
                if (Vector2.Distance(middle, biomeNodes[i].origin) < closestDistance)
                {
                    closestDistance = Vector2.Distance(middle, biomeNodes[i].origin);
                    closestIndex = i;
                }
            }

            biomesSortedDistanceMiddle.Add(biomeNodes[closestIndex]);
            biomeNodes.RemoveAt(closestIndex);
        }

        biomesSortedDistanceMiddle[0].elevation = m_middleElevation;
        biomesSortedDistanceMiddle[0].isMainLand = true;

        for (int i = 1; i < biomesSortedDistanceMiddle.Count; i++)
        {
            if (biomesSortedDistanceMiddle[i].isBorder)
            {
                continue;
            }

            float elevationSum = 0;
            float neighborMax = float.MinValue;

            List<VoronoiNode> neighbors = biomesSortedDistanceMiddle[i].getNeighborBiomes();

            for (int j = 0; j < neighbors.Count; j++)
            {
                elevationSum += neighbors[j].elevation;
                neighborMax = Mathf.Max(neighborMax, neighbors[j].elevation);
            }

            elevationSum /= neighbors.Count;
            elevationSum += (Mathf.PerlinNoise(seed + i * 0.1f, seed + i * 0.2f) - 0.5f) * 2 * m_biomeHeightRandomness;

            biomesSortedDistanceMiddle[i].elevation = Mathf.Lerp(neighborMax, elevationSum, m_fadingStrength);
        }

        for (int i = 1; i < biomesSortedDistanceMiddle.Count; i++)
        {
            biomesSortedDistanceMiddle[i].checkConnectionMainLand();
        }

        biomeNodes = biomesSortedDistanceMiddle;

        */

        // landmass height distribution

        Debug.Log("landmass height distribution");

        if (m_landmassHeightStep)
        {
            for (int i = 0; i < biomeNodes.Count; i++)
            {
                if (biomeNodes[i].elevation < 0)
                {
                    biomeNodes[i].m_distanceToWater = 0;
                }
            }

            List<VoronoiNode> openListWaterDistance = new List<VoronoiNode>();
            openListWaterDistance.AddRange(biomeNodes);

            int distanceStage = 0;

            while (openListWaterDistance.Count > 0)
            {
                for (int i = 0; i < openListWaterDistance.Count; i++)
                {
                    if (openListWaterDistance[i].m_distanceToWater != null && openListWaterDistance[i].m_distanceToWater == distanceStage)
                    {
                        openListWaterDistance[i].updateNeighborsDistanceToWater();
                        openListWaterDistance.RemoveAt(i);
                        i--;
                    }
                }

                distanceStage++;
            }

            for (int i = 0; i < biomeNodes.Count; i++)
            {
                if (biomeNodes[i].m_distanceToWater > 0)
                {
                    biomeNodes[i].elevation = (int)biomeNodes[i].m_distanceToWater * m_heightDistanceWater + (Mathf.PerlinNoise(m_seed + i * 0.1f, m_seed + i * 0.2f) - 0.5f) * 2 * m_biomeHeightRandomnessLandmass;
                }
            }
        }

        // nodes height smoothing

        Debug.Log("nodes height smoothing");

        for (int i = 0; i < m_voronoiSmoothCount; i++)
        {
            for(int j= 0; j < biomeNodes.Count; j++)
            {
                List<VoronoiNode> neighbors = biomeNodes[j].getNeighborBiomes();

                float average = 0;

                for(int k = 0; k < neighbors.Count; k++)
                {
                    average += neighbors[k].elevation;
                }

                biomeNodes[j].elevation = (biomeNodes[j].elevation + average) / (neighbors.Count + 1);
            }
        }

        // create texture

        Debug.Log("create texture");

        Color[] pixels = new Color[m_size * m_size];
        List<float> distancesInverse = new List<float>();
        List<float> deltaHToMiddle = new List<float>();

        float[,] heightmap = new float[m_size, m_size];

        for (int i = 0; i < biomeNodes.Count; i++)
        {
            List<Vector2Int> biomePixels = biomeNodes[i].getContainingPoints();
            List<VoronoiNode> neighbors = biomeNodes[i].getNeighborBiomes();

            // one color per biome

            for (int j = 0; j < biomePixels.Count; j++)
            {
                heightmap[biomePixels[j].x, biomePixels[j].y] = biomeNodes[i].elevation;

                pixels[biomePixels[j].x + biomePixels[j].y * m_size] = biomeNodes[i].DEBUG_getColor();
            }


            // bad all neighbor points interpolation
            /*
            for (int j = 0; j < biomePixels.Count; j++)
            {
                distancesInverse.Clear();
                deltaHToMiddle.Clear();

                float tempDistance = Vector2.Distance(biomePixels[j], biomesSortedDistanceMiddle[i].origin);

                distancesInverse.Add(1 / Mathf.Pow(tempDistance, m_interpolationExponent));
                distanceInverse = 1 / Mathf.Pow(tempDistance, m_interpolationExponent);
                deltaHToMiddle.Add(0);

                for (int k = 0; k < neighbors.Count; k++)
                {
                    tempDistance = Vector2.Distance(biomePixels[j], neighbors[k].origin);

                    deltaHToMiddle.Add(neighbors[k].elevation - biomesSortedDistanceMiddle[i].elevation);

                    distancesInverse.Add(1 / Mathf.Pow(tempDistance, m_interpolationExponent));
                    distanceInverse += 1 / Mathf.Pow(tempDistance, m_interpolationExponent);
                }

                float height = biomesSortedDistanceMiddle[i].elevation;

                for(int k = 0; k < deltaHToMiddle.Count; k++)
                {
                    height += deltaHToMiddle[k] * (distancesInverse[k] / distanceInverse);
                }

                Color col;

                if (height > 0)
                {
                    col = new Color(1 - (height / 100), 1 - (height / 100), 1 - (height / 100));
                }
                else
                {
                    col = new Color(0, 0,0);
                }

                pixels[biomePixels[j].x + biomePixels[j].y * size] = col;// BiomeNode.elevationToColor(height);
            }                
            */
            // bad 2-Point-linear interpolation
            /*
            for (int j = 0; j < biomePixels.Count; j++)
            {
                float closestNeighborDist = float.MaxValue;
                float closestNeighborHeight = 0;
                Vector2 closestNeighborPos = Vector2.zero;

                for(int k = 0; k < neighbors.Count; k++)
                {
                    float distance = Vector2.Distance(neighbors[k].origin, biomePixels[j]);

                    if(distance < closestNeighborDist)
                    {
                        closestNeighborDist = distance;
                        closestNeighborHeight = neighbors[k].elevation;
                        closestNeighborPos = neighbors[k].origin;
                    }
                }

                float distanceFactor = Vector2.Distance(biomesSortedDistanceMiddle[i].origin, biomePixels[j])/ Vector2.Distance(biomesSortedDistanceMiddle[i].origin, closestNeighborPos);

                float height = Mathf.Lerp(biomesSortedDistanceMiddle[i].elevation, closestNeighborHeight, distanceFactor);

                pixels[biomePixels[j].x + biomePixels[j].y * size] = BiomeNode.elevationToColor(height);
            }
            */
        }

        // smooth heightmap

        Debug.Log("smooth heightmap");

        int iEnd = m_size - 1;
        int jEnd = m_size - 1;

        float averageHeight;

        for (int k = 0; k < m_smoothWaterLandBorder; k++)
        {
            for (int i = 1; i < iEnd; i++)
            {
                for (int j = 1; j < jEnd; j++)
                {
                    averageHeight = 0;

                    averageHeight += heightmap[i + 1, j];
                    averageHeight += heightmap[i, j + 1];
                    averageHeight += heightmap[i - 1, j];
                    averageHeight += heightmap[i, j - 1];
                    averageHeight += heightmap[i, j];

                    heightmap[i, j] = averageHeight / 5;
                }
            }
        }

        for (int k = 0; k < m_smoothWaterLand; k++)
        {
            for (int i = 1; i < iEnd; i++)
            {
                for (int j = 1; j < jEnd; j++)
                {
                    averageHeight = 0;

                    averageHeight += heightmap[i + 1, j];
                    averageHeight += heightmap[i, j + 1];
                    averageHeight += heightmap[i - 1, j];
                    averageHeight += heightmap[i, j - 1];
                    averageHeight += heightmap[i, j];

                    if (heightmap[i, j] > 0)
                    {
                        if (averageHeight > 0)
                        {
                            heightmap[i, j] = averageHeight / 5;
                        }
                    }
                    else
                    {
                        if (averageHeight < 0)
                        {
                            heightmap[i, j] = averageHeight / 5;
                        }
                    }
                }
            }
        }

        // upscaling

        Debug.Log("upscaling");

        heightmap = ArrayTools.upscaleHeightmapDiamondSquare(heightmap, m_upscaleCount);

        m_heightmap = heightmap;

        int upscaledSize = heightmap.GetLength(0);

        pixels = new Color[upscaledSize * upscaledSize];

        if (m_pureHeightMap)
        {
            for (int i = 0; i < upscaledSize; i++)
            {
                for (int j = 0; j < upscaledSize; j++)
                {
                    pixels[i + j * upscaledSize] = new Color((heightmap[i, j] + 100) / 200, (heightmap[i, j] + 100) / 200, (heightmap[i, j] + 100) / 200);
                }
            }
        }
        else
        {
            for (int i = 0; i < upscaledSize; i++)
            {
                for (int j = 0; j < upscaledSize; j++)
                {
                    //pixels[i + j * size] = BiomeNode.elevationToColor(heightmap[i, j]);

                    if (heightmap[i, j] > 0)
                    {
                        pixels[i + j * upscaledSize] = new Color(1f - (heightmap[i, j] / 100), 1f - (heightmap[i, j] / 100), 1f - (heightmap[i, j] / 100));
                    }
                    else
                    {
                        pixels[i + j * upscaledSize] = new Color(0, 0, 1 + heightmap[i, j] / 100);
                    }
                }
            }
        }

        m_tex = new Texture2D(upscaledSize, upscaledSize);

        for (int i = 0; i < upscaledSize; i++)
        {
            break;
            for (int j = 0; j < upscaledSize; j++)
            {
                Vector2? pos = voroni.NearestSitePoint(i, j);

                float colValue1 = Mathf.PerlinNoise(pos.Value.x, pos.Value.y);
                float colValue2 = Mathf.PerlinNoise(pos.Value.x + 1, pos.Value.y + 1);
                float colValue3 = Mathf.PerlinNoise(pos.Value.x + 2, pos.Value.y + 2);

                pixels[i + j * upscaledSize] = new Color(colValue1, colValue2, colValue3);
            }
        }

        m_tex.SetPixels(pixels);
        m_tex.Apply();

        voroni.Dispose();

        if (m_writeToDisk)
        {
            m_writeToDisk = false;

            byte[] bytes = m_tex.EncodeToPNG();

            System.IO.File.WriteAllBytes(@"C:\TEMP\Unity\SavedScreen.png", bytes);
        }
    }

    public static Vector3 Vec2ToVec3(Vector2? vec2)
    {
        return new Vector3(vec2.Value.x, 0, vec2.Value.y);
    }

    public static Vector2 Vec2_Q_ToVec2(Vector2? vec2Q)
    {
        return new Vector2(vec2Q.Value.x, vec2Q.Value.y);
    }
}
