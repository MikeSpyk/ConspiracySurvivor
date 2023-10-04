using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainShaderFiller : MonoBehaviour
{
    public static GameObject singleton;

    public enum WorldTexIndexToTexResolutionArray { Tex512 = 0, Tex1024 = 1, Tex2048 = 2 }

    [SerializeField] Material TerrainShaderMat;

    [SerializeField] Texture2D[] albedo512;
    [SerializeField] Texture2D[] normal512;
    [SerializeField] Texture2D[] albedo1024;
    [SerializeField] Texture2D[] normal1024;
    [SerializeField] Texture2D[] albedo2048;
    [SerializeField] Texture2D[] normal2048;

    [SerializeField] bool process512;
    [SerializeField] bool process1024;
    [SerializeField] bool process2048;

    [SerializeField] TextureFormat texFormat512;
    [SerializeField] TextureFormat texFormat1024;
    [SerializeField] TextureFormat texFormat2048;

    [SerializeField] bool linearColor512;
    [SerializeField] bool linearColor1024;
    [SerializeField] bool linearColor2048;

    [SerializeField] WorldTexIndexToTexResolutionArray[] worldIndexToTexResArray; // which texture index, form world-creation is to find in which texture array
    [SerializeField] int[] worldIndexToArrayIndex;
    [SerializeField] float[] texturesScales;
    [SerializeField] float[] texturesSmoothness;
    [SerializeField] float[] texturesMetallic;

    [SerializeField] Texture2DArray outputAlbedo512;
    [SerializeField] Texture2DArray outputNormal512;
    [SerializeField] Texture2DArray outputAlbedo1024;
    [SerializeField] Texture2DArray outputNormal1024;
    [SerializeField] Texture2DArray outputAlbedo2048;
    [SerializeField] Texture2DArray outputNormal2048;

    [SerializeField] Texture2D outputFirst512;

    void Awake()
    {
        singleton = gameObject;
    }

    public void injectTextures()
    {
        // check if inputs valid
        for (int i = 0; i < albedo512.Length; i++)
        {
            if (albedo512[i].height != 512 || albedo512[i].width != 512)
            {
                Debug.LogError("wrong texture dimensions on albedo512[" + i + "]. expecting 512 x 512");
            }
            if (normal512[i].height != 512 || normal512[i].width != 512)
            {
                Debug.LogError("wrong texture dimensions on normal512[" + i + "]. expecting 512 x 512");
            }
        }
        for (int i = 0; i < albedo1024.Length; i++)
        {
            if (albedo1024[i].height != 1024 || albedo1024[i].width != 1024)
            {
                Debug.LogError("wrong texture dimensions on albedo1024[" + i + "]. expecting 1024 x 1024");
            }
            if (normal1024[i].height != 1024 || normal1024[i].width != 1024)
            {
                Debug.LogError("wrong texture dimensions on normal1024[" + i + "]. expecting 1024 x 1024");
            }
        }
        for (int i = 0; i < albedo2048.Length; i++)
        {
            if (albedo2048[i].height != 2048 || albedo2048[i].width != 2048)
            {
                Debug.LogError("wrong texture dimensions on albedo2048[" + i + "]. expecting 2048 x 2048");
            }
            if (normal2048[i].height != 2048 || normal2048[i].width != 2048)
            {
                Debug.LogError("wrong texture dimensions on normal2048[" + i + "]. expecting 2048 x 2048");
            }
        }

        // convert texture format and fill output array

        Texture2DArray textureArrayAlbedo512 = null;

        Texture2DArray textureArrayAlbedo1024 = null;
        Texture2DArray textureArrayAlbedo2048 = null;

        Texture2DArray textureArrayNormal512 = null;
        Texture2DArray textureArrayNormal1024 = null;
        Texture2DArray textureArrayNormal2048 = null;

        Texture2D[] albedo512converted = null;
        Texture2D[] albedo1024converted = null;
        Texture2D[] albedo2048converted = null;

        Texture2D[] normal512converted = null;
        Texture2D[] normal1024converted = null;
        Texture2D[] normal2048converted = null;

        if (process512)
        {
            textureArrayAlbedo512 = new Texture2DArray(512, 512, albedo512.Length, texFormat512, true);
            textureArrayNormal512 = new Texture2DArray(512, 512, albedo512.Length, texFormat512, true);
            albedo512converted = new Texture2D[albedo512.Length];
            normal512converted = new Texture2D[albedo512.Length];
        }
        if (process1024)
        {
            textureArrayAlbedo1024 = new Texture2DArray(1024, 1024, albedo1024.Length, texFormat1024, true);
            textureArrayNormal1024 = new Texture2DArray(1024, 1024, albedo1024.Length, texFormat1024, true);
            albedo1024converted = new Texture2D[albedo1024.Length];
            normal1024converted = new Texture2D[albedo1024.Length];
        }
        if (process2048)
        {
            textureArrayAlbedo2048 = new Texture2DArray(2048, 2048, albedo2048.Length, texFormat2048, true);
            textureArrayNormal2048 = new Texture2DArray(2048, 2048, albedo2048.Length, texFormat2048, true);
            albedo2048converted = new Texture2D[albedo2048.Length];
            normal2048converted = new Texture2D[albedo2048.Length];
        }

        for (int i = 0; i < albedo512.Length; i++)
        {
            albedo512converted[i] = new Texture2D(512, 512, texFormat512, true, linearColor512);
            normal512converted[i] = new Texture2D(512, 512, texFormat512, true, linearColor512);
            albedo512converted[i].SetPixels(albedo512[i].GetPixels());
            normal512converted[i].SetPixels(normal512[i].GetPixels());
            albedo512converted[i].Apply();
            normal512converted[i].Apply();
            textureArrayAlbedo512.SetPixels(albedo512converted[i].GetPixels(), i);
            textureArrayNormal512.SetPixels(normal512converted[i].GetPixels(), i);
        }
        for (int i = 0; i < albedo1024.Length; i++)
        {
            albedo1024converted[i] = new Texture2D(1024, 1024, texFormat1024, true, linearColor1024);
            normal1024converted[i] = new Texture2D(1024, 1024, texFormat1024, true, linearColor1024);
            albedo1024converted[i].SetPixels(albedo1024[i].GetPixels());
            normal1024converted[i].SetPixels(normal1024[i].GetPixels());
            albedo1024converted[i].Apply();
            normal1024converted[i].Apply();
            textureArrayAlbedo1024.SetPixels(albedo1024converted[i].GetPixels(), i);
            textureArrayNormal1024.SetPixels(normal1024converted[i].GetPixels(), i);
        }
        for (int i = 0; i < albedo2048.Length; i++)
        {
            albedo2048converted[i] = new Texture2D(2048, 2048, texFormat2048, true, linearColor2048);
            normal2048converted[i] = new Texture2D(2048, 2048, texFormat2048, true, linearColor2048);
            albedo2048converted[i].SetPixels(albedo2048[i].GetPixels());
            normal2048converted[i].SetPixels(normal2048[i].GetPixels());
            albedo2048converted[i].Apply();
            normal2048converted[i].Apply();
            textureArrayAlbedo2048.SetPixels(albedo2048converted[i].GetPixels(), i);
            textureArrayNormal2048.SetPixels(normal2048converted[i].GetPixels(), i);
        }

        if (process512)
        {
            textureArrayAlbedo512.Apply();
            textureArrayNormal512.Apply();
            outputAlbedo512 = textureArrayAlbedo512;
            outputNormal512 = textureArrayNormal512;
            TerrainShaderMat.SetTexture("_AlbedoTextures512", textureArrayAlbedo512);
            TerrainShaderMat.SetTexture("_NormalTextures512", textureArrayNormal512);
            outputFirst512 = new Texture2D(512, 512);
            outputFirst512.SetPixels(textureArrayAlbedo512.GetPixels(0));
            outputFirst512.Apply();
        }
        if (process1024)
        {
            textureArrayAlbedo1024.Apply();
            textureArrayNormal1024.Apply();
            outputAlbedo1024 = textureArrayAlbedo1024;
            outputNormal1024 = textureArrayNormal1024;
            TerrainShaderMat.SetTexture("_AlbedoTextures1024", textureArrayAlbedo1024);
            TerrainShaderMat.SetTexture("_NormalTextures1024", textureArrayNormal1024);
        }
        if (process2048)
        {
            textureArrayAlbedo2048.Apply();
            textureArrayNormal2048.Apply();
            outputAlbedo2048 = textureArrayAlbedo2048;
            outputNormal2048 = textureArrayNormal2048;
            TerrainShaderMat.SetTexture("_AlbedoTextures2048", textureArrayAlbedo2048);
            TerrainShaderMat.SetTexture("_NormalTextures2048", textureArrayNormal2048);
        }

        float[] worldIndexToTexResArrayConverted = new float[worldIndexToTexResArray.Length];
        for (int i = 0; i < worldIndexToTexResArray.Length; i++)
        {
            worldIndexToTexResArrayConverted[i] = (float)worldIndexToTexResArray[i];
        }
        TerrainShaderMat.SetFloatArray("_WorldIndexToTexArray", worldIndexToTexResArrayConverted);

        float[] worldIndexToArrayIndexConverted = new float[worldIndexToArrayIndex.Length];
        for (int i = 0; i < worldIndexToArrayIndex.Length; i++)
        {
            worldIndexToArrayIndexConverted[i] = (float)worldIndexToArrayIndex[i];
        }
        TerrainShaderMat.SetFloatArray("_WorldIndexToTexArrayIndex", worldIndexToArrayIndexConverted);

        TerrainShaderMat.SetFloatArray("_TexturesSmoothness", texturesSmoothness);
        TerrainShaderMat.SetFloatArray("_TexturesMetallic", texturesMetallic);

        TerrainShaderMat.SetFloatArray("_TexturesScale", texturesScales);
    }
}
