using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class Random_mesh_world : MonoBehaviour {

	private struct fixedWorldPoints
	{
		public int x;
		public int y;
		public float value;
	}
		
	public bool AutoApplyChanges = true;
	public bool ApplyChanges = true;
	public bool BuildMesh = false;
	public bool GetRandomSeed = false;
	public bool ShowColors = true;
	public bool setBorderHeightToZero = true;
	public bool raiseToMid = true;
	public bool smoothHeightMap = true;
	public bool useBaseHeightMapOnly = false;

	public int x_Size_vertex = 150;
	public int z_Size_vertex = 150;

	private float x_Offset = 0; // offset to start sampeling the perlin map
	private float z_Offset = 0; // offset to start sampeling the perlin map

	private const float SampleDistance = 1.1f; // distance between 2 sample point in perlin map

	public float seed = 0; // random seed defining the x and y offset

	private float[] baseVertHeightMap; // base height map on top which the random values get added
	private float[] NoisePixels; // flattend 2D array holding random pixels between -1 and 1

	// start colors on this height
	public float waterHeight = 8f;
	public float MountainHeight = 16f;
	public float SnowMountainHeight = 19f;
	public float CostSandHeight = 9.5f;

	public float vertexDistance = .01f; // distance between vertexes in mesh
	public float vertexHeightMultLin = -82f; // raise mesh linear from perlin map
	public float vertexHeightMultExp = 59f;  // raise mesh x² from perlin map
	public float MidRaise = 9e-05f;  // raise mesh more from center to border
	public float y_multiplier = 1f; // raises everything liniear at the end fo the creating of the mesh
	public float y_multiplier_random_only = 1f; // raises everything liniear coming from the perlin noise, is ignoring base height map

	public int fixedPointsCount = 100; // vertex count not affected by smoothing (only active on stage 2)
	private fixedWorldPoints[] fixedPoints;

	private Renderer Rend;

	public int smoothLevelStage1 = 10; // smooth level without reconizing fixed point
	public int smoothLevelStage2 = 30; // smooth level with reconizing fixed point
	public int smoothLevelStage3 = 10; // smooth level without reconizing fixed point

	public Texture2D WorldColorMap = null;
	private Color[] WorldColorMapPixels; // flattend array holding colors for the world color texture

	private Vector3[] MeshVerts;
	private Vector2[] MeshUV;
	private int[] triangles;

	private Vector3 meshOffset;

	private long LOCKED_has_HeightMapThreadStarted = 0;
	private long LOCKED_has_HeightMapThreadEnded = 0;

	// Use this for initialization
	void Awake () 
	{
		Rend = GetComponent<Renderer>();
		Rend.material.mainTexture = WorldColorMap;
	}

	// Update is called once per frame
	void Update () 
	{
		if(ApplyChanges)
		{
			buildVertexHeightMap();
			if(baseVertHeightMap != null)
			{
				AddBaseHeight();
			}
			buildMeshColorTexture();
			if(BuildMesh)
			{
				buildMesh();
			}
			ApplyChanges = false;
		}

		if(Interlocked.Read(ref LOCKED_has_HeightMapThreadEnded) == 1)
		{
			// heightMapThread has finished before this Frame
			Interlocked.Decrement(ref LOCKED_has_HeightMapThreadStarted); // thread ended --> it can start again
			Interlocked.Decrement(ref LOCKED_has_HeightMapThreadEnded); // thread ended and got recognized
			if(baseVertHeightMap != null)
			{
				AddBaseHeight();
			}
			buildMesh();
		}
	}

	public void setBaseVertHeightMap(float[] newHeightMap)
	{
		if(newHeightMap.Length != x_Size_vertex * z_Size_vertex)
		{
			Debug.LogError("new Base-Heightmap-array-size is not fitting to given array-size");
			return;
		}
		baseVertHeightMap = newHeightMap;
	}

	public float[] getHeightMapVerts()
	{
		return NoisePixels;
	}

	public float getTotalSizeX()
	{
		return (x_Size_vertex -1) * vertexDistance;
	}

	public float getTotalSizeZ()
	{
		return (z_Size_vertex -1) * vertexDistance;
	}

	public float getPerlinSampleDistance()
	{
		return SampleDistance;
	}

	public void setYMultiplierRandomOnly(float newMultiplier)
	{
		y_multiplier_random_only = newMultiplier;
	}

	public void setYMultiplier(float newMultiplier)
	{
		y_multiplier = newMultiplier;
	}

	public void setVertexDistance(float newDistance)
	{
		vertexDistance = newDistance;
	}

	public void setVertexSize(int x_size, int z_size)
	{
		x_Size_vertex = x_size;
		z_Size_vertex = z_size;
	}

	public void setPerlinOffsetX(float newValue)
	{
		if(newValue > -100000 && newValue < 100000)
		{
			x_Offset = newValue;
		}
		else
		{
			Debug.LogError("Random Mesh (" +gameObject.name +"): x-offset (" +newValue +") out of bounds");
		}
	}

	public void setPerlinOffsetZ(float newValue)
	{
		if(newValue > -100000 && newValue < 100000)
		{
			z_Offset = newValue;
		}
		else
		{
			Debug.LogError("Random Mesh (" +gameObject.name +"): z-offset (" +newValue +") out of bounds");
		}
	}

	public void startBuildHeightMapAndMeshThread()
	{
		if(Interlocked.Read(ref LOCKED_has_HeightMapThreadStarted) == 1)
		{
			// thread already running --> do nothing
		}
		else if(Interlocked.Read(ref LOCKED_has_HeightMapThreadStarted) == 0)
		{
			Interlocked.Increment(ref LOCKED_has_HeightMapThreadStarted);
			// no thread running --> start thread

			ThreadStart threadDelegate = new ThreadStart(buildVertexHeightMapThread);
			Thread newThread = new Thread(threadDelegate);
			newThread.Name = "heightMap";
			newThread.Start();

		}
		else
		{
			Debug.LogError("Unkown index: "+Interlocked.Read(ref LOCKED_has_HeightMapThreadStarted));
		}
	}

	private void buildVertexHeightMapThread()
	{
		buildVertexHeightMap();
		Interlocked.Increment(ref LOCKED_has_HeightMapThreadEnded);
	}

	public void AddBaseHeight()
	{
		if(NoisePixels == null)
		{
			NoisePixels = new float[x_Size_vertex * z_Size_vertex];
		}

			for(int i = 0 ; i <  NoisePixels.Length ;i++)
			{
				NoisePixels[i] += baseVertHeightMap[i];
			}
	}

	public void buildVertexHeightMap()
	{
		if(baseVertHeightMap != null)
		{
			if(useBaseHeightMapOnly)
			{
				return;
			}
		}
			
		NoisePixels = getRandomValuesSeed();

		if(raiseToMid)
		{
			// add more to the middle of the texture, than at the borders
			Vector2 MidPostition = new Vector2(x_Size_vertex/2, z_Size_vertex / 2);
			Vector2 CurrentPosition;
			float maxVecDistnace = Vector2.Distance(MidPostition, new Vector2(0,0));

			// raise from border to middle, middle is hightest point
			for(int i = 0; i < x_Size_vertex;i++)
			{
				for(int j = 0; j < z_Size_vertex; j++)
				{
					CurrentPosition = new Vector2(i,j);
					NoisePixels[i + j * z_Size_vertex] -=  MidRaise * (maxVecDistnace - Vector2.Distance(CurrentPosition, MidPostition));
				}
			}
		}

		if(setBorderHeightToZero)
		{
			// set the borders of the mesh to 0
			for(int i = 1; i < z_Size_vertex ;i++)
			{
				NoisePixels[i * x_Size_vertex ] = 0.0f;
				NoisePixels[i * (x_Size_vertex) -1] =0.0f;
			}
			for(int i = 0; i < x_Size_vertex ;i++)
			{
				NoisePixels[i] =  0.0f;
				NoisePixels[i + ((z_Size_vertex -1) * x_Size_vertex)] = 0.0f;
			}
		}

		if(smoothHeightMap)
		{
			// smooth terrain depening if some points should stay fixed or not
			smoothNoisePixels(smoothLevelStage1, true);
			NewFixedPointsRandomSeed();
			smoothNoisePixels(smoothLevelStage2, false);
			smoothNoisePixels(smoothLevelStage3, true);
		}

		// add mult factors
		processPixels(NoisePixels,vertexHeightMultLin, vertexHeightMultExp);
	}

	private void buildMeshColorTexture()
	{
		WorldColorMap = new Texture2D(x_Size_vertex, z_Size_vertex);
		WorldColorMapPixels = new Color[x_Size_vertex * z_Size_vertex];
		// add colors to texture
		if(ShowColors)
		{
			for(int i = 0; i < x_Size_vertex;i++)
			{
				for(int j = 0; j < z_Size_vertex; j++)
				{
					if(NoisePixels[i + j * z_Size_vertex] < waterHeight)
					{
						WorldColorMapPixels[i + j * z_Size_vertex] = new Color(0.0f, 0.0f, 1.0f);
					}
					else if (NoisePixels[i + j * z_Size_vertex] < CostSandHeight)
					{
						WorldColorMapPixels[i + j * z_Size_vertex] = new Color(1.0f, 1.0f, 0.0f);
					}
					else if (NoisePixels[i + j * z_Size_vertex] < MountainHeight)
					{
						WorldColorMapPixels[i + j * z_Size_vertex] = new Color(0.1f, 0.5f + (0.5f * Mathf.PerlinNoise(.5f +i ,.5f+ j)) , 0.1f);
					}
					else if(NoisePixels[i + j * z_Size_vertex] < SnowMountainHeight)
					{
						WorldColorMapPixels[i + j * z_Size_vertex] = new Color(0.5f, 0.5f, 0.5f);
					}
					else
					{
						WorldColorMapPixels[i + j * z_Size_vertex] = new Color(1.0f, 1.0f, 1.0f);
					}

				}
			}
		}
		else
		{
			for(int i = 0; i < x_Size_vertex;i++)
			{
				for(int j = 0; j < z_Size_vertex; j++)
				{
					WorldColorMapPixels[i + j * z_Size_vertex] = new Color(1.0f, 1.0f, 1.0f);
				}
			}
		}
		WorldColorMap.SetPixels(WorldColorMapPixels);
		WorldColorMap.Apply();
	}

	public void buildMesh()
	{
		meshOffset = - new Vector3( vertexDistance * x_Size_vertex/2 - vertexDistance/2, 0f, vertexDistance * z_Size_vertex/2 - vertexDistance/2);
		MeshVerts = new Vector3[x_Size_vertex * z_Size_vertex];
		MeshUV = new Vector2[x_Size_vertex * z_Size_vertex];	

		int count2 = 0;
		for(int i = 0; i < z_Size_vertex;i++)
		{
			for(int j = 0; j < x_Size_vertex; j++)
			{
				MeshVerts[count2] = meshOffset + new Vector3(j*vertexDistance, NoisePixels[j+x_Size_vertex*i] * y_multiplier,i*vertexDistance);
				MeshUV[count2] = new Vector2(1.0f * j/x_Size_vertex,1.0f*i/z_Size_vertex);
				count2++;
			}
		}

		int[]trianglesTest = new int[x_Size_vertex * z_Size_vertex*6];

		for(int i = 0; i < x_Size_vertex * z_Size_vertex*6; i++)
		{
			trianglesTest[i] = -1;
		}

		int count = 0;
		for(int i = 0; i < z_Size_vertex-1;i++)
		{
			for(int j = 0; j < x_Size_vertex-1; j++)
			{
				trianglesTest[count] = j + x_Size_vertex * i;
				count++;
				trianglesTest[count] = j+1 + x_Size_vertex * (i+1);
				count++;
				trianglesTest[count] = j+1 + x_Size_vertex * i;
				count++;
			}
		}
		for(int i = 0; i < z_Size_vertex-1;i++)
		{
			for(int j = 0; j < x_Size_vertex-1; j++)
			{
				trianglesTest[count] = j + x_Size_vertex * i;
				count++;
				trianglesTest[count] = j + x_Size_vertex * (i+1);
				count++;
				trianglesTest[count] = j+1 + x_Size_vertex * (i+1);
				count++;
			}
		}

		for(int i = 0; i < x_Size_vertex * z_Size_vertex * 10; i++)
		{
			if(trianglesTest[i] == -1)
			{
				triangles = new int[i];

				for(int j = 0; j < i; j++)
				{
					triangles[j] = trianglesTest[j];
				}

				break;
			}
		}

		if(GetComponent<MeshRenderer>() != null)
		{
			GetComponent<MeshRenderer>().material.mainTexture = WorldColorMap;
		}
		else
		{
			Debug.LogWarning("No renderer found");
		}

		if(GetComponent<MeshFilter>() != null)
		{
			GetComponent<MeshFilter>().mesh.vertices = MeshVerts;
			GetComponent<MeshFilter>().mesh.triangles = triangles;
			GetComponent<MeshFilter>().mesh.uv = MeshUV;
			GetComponent<MeshFilter>().mesh.RecalculateNormals();
			GetComponent<MeshFilter>().mesh.name = "World_Distance_Terrain";
			if(GetComponent<MeshCollider>() != null)
			{
				GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().mesh;
			}
		}
		else
		{
			Debug.LogWarning("No MeshFilter found");
		}
		refreshBounds();

		// debug rays

		Debug.DrawRay(transform.position + new Vector3(getTotalSizeX()/2,0.0f, getTotalSizeZ()/2), Vector3.up * 100, Color.red, 100f);
		Debug.DrawRay(transform.position + new Vector3(-getTotalSizeX()/2,0.0f, getTotalSizeZ()/2), Vector3.up * 100, Color.blue, 100f);
		Debug.DrawRay(transform.position + new Vector3(getTotalSizeX()/2,0.0f, -getTotalSizeZ()/2), Vector3.up * 100, Color.green, 100f);
		Debug.DrawRay(transform.position + new Vector3(-getTotalSizeX()/2,0.0f, -getTotalSizeZ()/2), Vector3.up * 100, Color.black, 100f);

	}

	private void refreshBounds()
	{
		Bounds myBounds;
		myBounds = gameObject.GetComponent<Renderer>().bounds;
		myBounds.center = new Vector3( vertexDistance * x_Size_vertex/2, 0.0f, vertexDistance * z_Size_vertex/2) + gameObject.transform.position;
		myBounds.extents = new Vector3( vertexDistance * x_Size_vertex/2, vertexDistance * z_Size_vertex/2, vertexDistance * z_Size_vertex/2);
		//Debug.DrawRay(myBounds.center, Vector3.up, Color.red, 30f);
	}

	private void NewFixedPointsRandomSeed()
	{
		fixedPoints = new fixedWorldPoints[fixedPointsCount];
		for(int i = 0; i < fixedPointsCount;i++)
		{
			fixedPoints[i].x = (int) (Mathf.PerlinNoise( (seed 						+i) * SampleDistance, SampleDistance) * x_Size_vertex);
			fixedPoints[i].y = (int) (Mathf.PerlinNoise( (seed + fixedPointsCount 	+i) * SampleDistance, SampleDistance) * z_Size_vertex);
			fixedPoints[i].value = NoisePixels[fixedPoints[i].x + (fixedPoints[i].y * x_Size_vertex)] ;
		}
	}

	private float[] getRandomValuesSeed()
	{
		float[] NewNoisePixels = new float[x_Size_vertex * z_Size_vertex];
		float noiseSampel;

		// create and save random pixels
		for(int i = 0; i < z_Size_vertex; i++)
		{
			for(int j = 0; j < x_Size_vertex; j++)
			{
				noiseSampel = (Mathf.PerlinNoise(x_Offset + j *SampleDistance, z_Offset + i*SampleDistance) -.5f) *2; // random value between 0 and 1 depending on a seed
				NewNoisePixels[j + (i * x_Size_vertex)] = noiseSampel;

			}
		}

		return NewNoisePixels;
	}  
		
	private void smoothNoisePixels(int count, bool IgnoreFixedPoints)
	{
		// smooth texture
		int currentSmoothStage = 0;
		float smoothedValue;

		while(currentSmoothStage < count)
		{
			// smooth terrain
			for(int i = 1; i < z_Size_vertex-1; i++)
			{
				for(int j = 1; j < x_Size_vertex-1; j++)
				{
					smoothedValue = 0;
					for(int k = 0; k < 3;k++)
					{
						for(int l = 0; l <3;l++)
						{
							smoothedValue += NoisePixels[j+ k-1 + x_Size_vertex * (i+l-1)];
						}
					}
					smoothedValue /= 9;

					NoisePixels[j + (i * x_Size_vertex)] = smoothedValue;

					if(!IgnoreFixedPoints)
					{
						for(int m = 0; m < fixedPointsCount; m++)
						{
							NoisePixels[fixedPoints[m].x + (fixedPoints[m].y * x_Size_vertex)] = fixedPoints[m].value;
						}
					}

				}
			}

			if(!setBorderHeightToZero)
			{
				// if border is not 0 but a random number smooth it here because it was ommited before
				for(int i = 1; i < z_Size_vertex ;i++)
				{
					NoisePixels[i * x_Size_vertex ] = NoisePixels[i * (x_Size_vertex)+1 ];
					NoisePixels[i * (x_Size_vertex) -1] =NoisePixels[i * (x_Size_vertex) -2];
				}
				for(int i = 0; i < x_Size_vertex ;i++)
				{
					NoisePixels[i] =  NoisePixels[i + x_Size_vertex ];
					NoisePixels[i + ((z_Size_vertex -1) * x_Size_vertex)] = NoisePixels[i + ((z_Size_vertex -2) * x_Size_vertex)];
				}
			}

			currentSmoothStage++;
		}
	}

	private void processPixels(float[] inputPixels, float Lin, float Quad)
	{
		// adds some faktors to the terain to raise or lower it
		// add mult factors
		for(int i = 0; i < z_Size_vertex; i++)
		{
			for(int j = 0; j < x_Size_vertex; j++)
			{
				inputPixels[j + (i * x_Size_vertex)] = (		-	Mathf.Pow( inputPixels[j + (i * x_Size_vertex)]*Quad,5f )		-inputPixels[j + (i * x_Size_vertex)]  * Lin) * y_multiplier_random_only;
			}
		}
	}

	void OnValidate()
	{
		if(AutoApplyChanges)
		{
			ApplyChanges = true;
		}
		if(GetRandomSeed)
		{
			seed = Random.value * 100000;
			GetRandomSeed = false;
		}
		if(seed > -100000 && seed < 100000)
		{
			x_Offset = seed;
			z_Offset = seed;
		}
	}

}
