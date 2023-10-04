using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BillboardCreator : MonoBehaviour {

	public bool startCreation = false;
	public GameObject[] ObjectsToBillboard;
	public string[] objectsName;
	public Vector2[] cameraOffsets;
	public bool[] includedObjects;

	public int numberOfPics = 4;
	private float deltaDir;

	public int picSizeX = 100;
	public int picSizeY= 100;

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(startCreation)
		{
			startCreation = false;

			if(ObjectsToBillboard.Length != objectsName.Length || cameraOffsets.Length != objectsName.Length || includedObjects.Length != objectsName.Length)
			{
				Debug.LogError("input array-sizes differ !");
				return;
			}

			deltaDir = 360f / numberOfPics;
			Vector3 rotVec;
			GameObject tempObj;

			for(int j = 0; j < ObjectsToBillboard.Length; j++)
			{
				if(!includedObjects[j])
				{
					continue;
				}

				rotVec = Vector3.forward * cameraOffsets[j].x + new Vector3(0,cameraOffsets[j].y,0);
				tempObj = Instantiate(ObjectsToBillboard[j], Vector3.zero, Quaternion.identity) as GameObject;
				Camera.main.transform.position = rotVec;
				for(int i = 0; i < numberOfPics;i++)
				{
					takeCameraScreenshot(objectsName[j], (""+(deltaDir * i)).Replace(",","_").Replace(".","_") +"_DEGREES"  );
					rotVec = Quaternion.Euler(0,deltaDir,0) * rotVec; // camera position
					Camera.main.transform.position = rotVec;
					Camera.main.transform.Rotate(new Vector3(0,deltaDir,0));
				}
				Destroy(tempObj);
			}

		}
	}

	private void takeCameraScreenshot(string objName, string instanceName)
	{
		RenderTexture rt = new RenderTexture(picSizeX, picSizeY, 32);
		rt.autoGenerateMips = false;
		rt.antiAliasing = 1;
		rt.format = RenderTextureFormat.ARGB32;
		Camera.main.targetTexture = rt;
		Texture2D screenShot = new Texture2D(picSizeX, picSizeY, TextureFormat.ARGB32, false);
		Camera.main.Render();
		RenderTexture.active = rt;
		screenShot.ReadPixels(new Rect(0, 0, picSizeX, picSizeY), 0, 0, false);
		screenShot.Apply();
		Camera.main.targetTexture = null;
		RenderTexture.active = null; // JC: added to avoid errors
		Destroy(rt);

		Color[] pixels = screenShot.GetPixels();

		Color transparant = new Color(0.0f,0.0f,0.0f,0.0f);

		for(int i = 0; i < pixels.Length;i++)
		{
			if(pixels[i] == Color.white)
			{
				pixels[i] = transparant;
			}
			else
			{
				pixels[i] = new Color(pixels[i].r,pixels[i].g,pixels[i].b,1.0f);
			}
		}

		screenShot.SetPixels(pixels);
		screenShot.Apply();

		byte[] bytes = screenShot.EncodeToPNG();
		string dirName = Application.dataPath + "/Sandbox/Mike/BillboardCreator" +objName;
		string filename = dirName + "/" +objName +"_" +instanceName + ".png";

		if(Directory.Exists(dirName))
		{

		}
		else
		{
			Directory.CreateDirectory(dirName);
		}
		System.IO.File.WriteAllBytes(filename, bytes);
	}

}
