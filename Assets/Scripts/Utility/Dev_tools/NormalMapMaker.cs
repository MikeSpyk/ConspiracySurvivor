using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalMapMaker : MonoBehaviour {

	[SerializeField] private Texture2D texInHeightMap;
	[SerializeField] private bool startCreation = false;
	[SerializeField] private float heightFactor = 1f;
	[SerializeField] private Texture2D resultingNormalMap;
	[SerializeField] private int blur_kernel = 1;
	[SerializeField] private int blur_iterations= 1;
	[SerializeField] private string FileName = "";
	[SerializeField] private bool saveFileToDisk = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(startCreation)
		{
			startCreation = false;

			float[] heightMap = new float[texInHeightMap.width * texInHeightMap.height];
			Color[] heightMapColors = texInHeightMap.GetPixels();

			// read input texture
			for(int i = 0; i < heightMap.Length; i++)
			{
				heightMap[i] = (heightMapColors[i].r * 2 -1f) *heightFactor;
			}

			applyGaussianBlur(heightMap,new Vector2Int(texInHeightMap.width, texInHeightMap.height),blur_kernel,blur_iterations);

			// create normals

			/*
		 * 			C
		 * 	D		i		B
		 * 			A
		 */

			Vector2Int indexA = new Vector2Int(0,0);
			Vector2Int indexB = new Vector2Int(0,0);
			Vector2Int indexC = new Vector2Int(0,0);
			Vector2Int indexD = new Vector2Int(0,0);
			int maxIndex = heightMap.Length - 1;

			Vector3 i_A = new Vector3 (0, 0, -1);
			Vector3 i_B = new Vector3 (1, 0, 0);
			Vector3 i_C = new Vector3 (0, 0, 1);
			Vector3 i_D = new Vector3 (-1, 0, 0);

			Vector3 ABNormal;
			Vector3 BCNormal;
			Vector3 CDNormal;
			Vector3 DANormal;

			Vector3 resultingNormal;

			Color[] normalMapPixels = new Color[heightMap.Length];

			for (int i = 0; i < texInHeightMap.width; i++)
			{
				for (int j = 0; j < texInHeightMap.height; j++)
				{
					indexA.x = i;
					indexA.y = j-1;
					if(indexA.y < 0)
					{
						indexA.y  = texInHeightMap.height-1;
					}

					indexB.x = i+1;
					if(indexB.x> texInHeightMap.width-1)
					{
						indexB.x  = 0;
					}
					indexB.y = j;

					indexC.x = i;
					indexC.y = j+1;
					if(indexC.y > texInHeightMap.height-1)
					{
						indexC.y = 0;
					}

					indexD.x = i-1;
					if(indexD.x < 0)
					{
						indexD.x = texInHeightMap.width-1;
					}
					indexD.y = j;

					i_A.y = heightMap [indexA.x + indexA.y * texInHeightMap.width] - heightMap [i + j* texInHeightMap.width];
					i_B.y = heightMap [indexB.x + indexB.y * texInHeightMap.width] - heightMap [i + j* texInHeightMap.width];
					i_C.y = heightMap [indexC.x + indexC.y * texInHeightMap.width] - heightMap [i + j* texInHeightMap.width];
					i_D.y = heightMap [indexD.x + indexD.y * texInHeightMap.width] - heightMap [i + j* texInHeightMap.width];

					ABNormal = Vector3.Cross (i_B, i_A).normalized;
					BCNormal = Vector3.Cross (i_C, i_B).normalized;
					CDNormal = Vector3.Cross (i_D, i_C).normalized;
					DANormal = Vector3.Cross (i_A, i_D).normalized;

					resultingNormal = (ABNormal + BCNormal + CDNormal + DANormal).normalized;

					normalMapPixels[i + j* texInHeightMap.width] = new Color( (resultingNormal.x +1)/2, (resultingNormal.z+1)/2, (resultingNormal.y+1)/2);
				}
			}

			/*
			for (int i = 0; i < normalMapPixels.Length; i++)
			{
				
				indexA = Mathf.Max (0, i - texInHeightMap.width);
					indexB = Mathf.Min (maxIndex, i + 1);
					indexD = Mathf.Max (0, i - 1);
				indexC = Mathf.Min (maxIndex, i + texInHeightMap.width);

				i_A.y = heightMap [indexA] - heightMap [i];
				i_B.y = heightMap [indexB] - heightMap [i];
				i_C.y = heightMap [indexC] - heightMap [i];
				i_D.y = heightMap [indexD] - heightMap [i];

					ABNormal = Vector3.Cross (i_B, i_A).normalized;
					BCNormal = Vector3.Cross (i_C, i_B).normalized;
					CDNormal = Vector3.Cross (i_D, i_C).normalized;
					DANormal = Vector3.Cross (i_A, i_D).normalized;

					resultingNormal = (ABNormal + BCNormal + CDNormal + DANormal).normalized;

				normalMapPixels[i] = new Color( (resultingNormal.x +1)/2, (resultingNormal.z+1)/2, (resultingNormal.y+1)/2);
			}
*/

			resultingNormalMap = new Texture2D(texInHeightMap.width,texInHeightMap.height);
			resultingNormalMap.SetPixels(normalMapPixels);
			resultingNormalMap.Apply();
		}

		if(saveFileToDisk)
		{
			saveFileToDisk = false;
			SaveTextureAsPNG(resultingNormalMap, Application.dataPath +"/ToolExport/NormalMapMaker/" + FileName +".png");
		}
	}

	private void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
	{
		byte[] _bytes =_texture.EncodeToPNG();
		System.IO.File.WriteAllBytes(_fullPath, _bytes);
		Debug.Log(_bytes.Length/1024  + "Kb was saved as: " + _fullPath);
	}

	private void applyGaussianBlur(float[] input, Vector2Int arrayDimension, int kernelSize, int iterations)
	{
		if(iterations < 1 || kernelSize < 1)
		{
			return;
		}

		float average;
		float[] tempArray = new float[arrayDimension.x * arrayDimension.y];
		int kernelSizeSquare = 0;

		for(int l = -kernelSize; l < kernelSize; l++)
		{
			for(int m = -kernelSize; m < kernelSize; m++)
			{
				kernelSizeSquare++;
			}
		}


		for(int k = 0; k < iterations; k++)
		{
			for(int i = kernelSize; i < arrayDimension.x-kernelSize; i++)
			{
				for(int j = kernelSize; j < arrayDimension.y-kernelSize; j++)
				{
					average = 0;
					for(int l = -kernelSize; l < kernelSize; l++)
					{
						for(int m = -kernelSize; m < kernelSize; m++)
						{
							average += input[i+l+(j+m)*arrayDimension.x];
						}
					}
					tempArray[i+j*arrayDimension.x] = average / kernelSizeSquare;
				}
			}

			for(int i = kernelSize; i < arrayDimension.x-kernelSize; i++)
			{
				for(int j = kernelSize; j < arrayDimension.y-kernelSize; j++)
				{
					input[i+j*arrayDimension.x] = tempArray[i+j*arrayDimension.x];
				}
			}
		}
	}

}
