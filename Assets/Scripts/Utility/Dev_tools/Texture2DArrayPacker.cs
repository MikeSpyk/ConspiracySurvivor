#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

public class Texture2DArrayPacker : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Texture2D[] m_textures;

    [SerializeField] private string m_saveAssetPath = "Assets/Textures/TextureArrays/TerrainTextures/TerrainAlbedo.asset";

    [SerializeField] private int m_pixelsPerEdge = 2048;
    [SerializeField] private bool m_useFirstInputTextureFormat = false;
    [SerializeField] private TextureFormat m_textureFormat = TextureFormat.RGBA32;
    [SerializeField] private bool m_mipMap = false;
    [SerializeField, Min(0)] private int m_lodLevels = 0;
    [SerializeField] private bool m_lodsAsAtlas = true;
    [SerializeField] private bool m_linear = false;
    [SerializeField] private bool DEBUG_LODColor = false;

    [Header("Interface")]
    [SerializeField] private bool m_execute = false;

    private void Start()
    {
        Debug.Log("Reminder: Normal-Maps must not be of texture-type default and not normal-map");
    }

    void Update()
    {
        if (m_execute)
        {
            if(m_useFirstInputTextureFormat)
            {
                m_textureFormat = m_textures[0].format;
            }

            m_execute = false;

            string[] pathSplit = m_saveAssetPath.Split('.');

            string basePath = pathSplit[0];
            string extension = pathSplit[1];

            Texture2D[] inputArray = m_textures;

            Texture2DArray outputTextureArray = null;

            List<Texture2DArray> texture2DsLODs = new List<Texture2DArray>();

            for (int j = 0; j < m_lodLevels + 1; j++)
            {
                if (j == 0)
                {
                    // upscale different texture sizes to the same size

                    outputTextureArray = new Texture2DArray(m_pixelsPerEdge, m_pixelsPerEdge, m_textures.Length, m_textureFormat, m_mipMap, m_linear);

                    for (int i = 0; i < inputArray.Length; i++)
                    {
                        Texture2D tex = transformTexture(inputArray[i], m_pixelsPerEdge, m_textureFormat);
                        inputArray[i] = tex;
                        outputTextureArray.SetPixels(tex.GetPixels(), i);
                    }
                }
                else
                {
                    // downscale once

                    outputTextureArray = new Texture2DArray(outputTextureArray.width / 2, outputTextureArray.height / 2, m_textures.Length, m_textureFormat, m_mipMap, m_linear);

                    for (int i = 0; i < inputArray.Length; i++)
                    {
                        Color[] pixels = downscaleTexture(inputArray[i].GetPixels());

                        Color lerpColor;

                        if (j % 2 == 0)
                        {
                            lerpColor = Color.red;
                        }
                        else
                        {
                            lerpColor = Color.blue;
                        }

                        if (DEBUG_LODColor)
                        {
                            //float lerpValue = (float)(j+1) / m_lodLevels;
                            float lerpValue = 0.2f;

                            for (int k = 0; k < pixels.Length; k++)
                            {
                                pixels[k] = Color.Lerp(pixels[k], lerpColor, lerpValue);
                            }
                        }

                        int size = (int)Mathf.Sqrt(pixels.Length);

                        outputTextureArray.SetPixels(pixels, i);

                        inputArray[i] = new Texture2D(size, size, m_textureFormat, false);
                        inputArray[i].SetPixels(pixels);
                        inputArray[i].Apply();
                    }

                }

                texture2DsLODs.Add(outputTextureArray);

                if (!m_lodsAsAtlas)
                {
                    AssetDatabase.CreateAsset(outputTextureArray, basePath + "_LOD" + j + "." + extension);
                }
            }

            if (m_lodsAsAtlas)
            {
                Texture2DArray multipleLODTexture2D = new Texture2DArray(m_pixelsPerEdge + m_pixelsPerEdge / 2, m_pixelsPerEdge, m_textures.Length, m_textureFormat, m_mipMap, m_linear);

                for (int i = 0; i < m_textures.Length; i++)
                {
                    Color[] pixels = new Color[multipleLODTexture2D.width * multipleLODTexture2D.height];

                    int textureSizeX = multipleLODTexture2D.width;

                    int startPosX = 0;
                    int startPosY = 0;
                    int sizeX = m_pixelsPerEdge;
                    int sizeY = m_pixelsPerEdge;

                    for (int j = 0; j < m_lodLevels; j++)
                    {
                        Color[] mipPixels = texture2DsLODs[j].GetPixels(i);
                        int mipWidth = texture2DsLODs[j].width;

                        for (int k = 0; k < sizeX; k++)
                        {
                            for (int l = 0; l < sizeY; l++)
                            {
                                pixels[startPosX + k + (startPosY + l) * textureSizeX] = mipPixels[k + l * mipWidth];
                            }
                        }

                        if (j == 1) // from now on put the texture above the LOD1 (to save some space)
                        {
                            startPosY = m_pixelsPerEdge / 2;
                            startPosX = m_pixelsPerEdge;
                        }
                        else
                        {
                            startPosX += sizeX;
                        }

                        sizeX /= 2;
                        sizeY /= 2;
                    }

                    multipleLODTexture2D.SetPixels(pixels, i);
                }

                multipleLODTexture2D.filterMode = FilterMode.Point;
                AssetDatabase.CreateAsset(multipleLODTexture2D, basePath + "_Atlas_LOD" + "0" + "." + extension);
            }

        }
    }

    private static Texture2D transformTexture(Texture2D texture, int edgeLength, TextureFormat format, bool mipChain = false)
    {
        if (texture.width != texture.height)
        {
            Debug.LogError("Texture2DArrayPacker:transformTexture: texture isn't quadratic !");
        }

        if (texture.width % 2 != 0)
        {
            Debug.LogError("Texture2DArrayPacker:transformTexture: texture edge length isn't dividable by 2 !");
        }

        if (edgeLength % 2 != 0)
        {
            Debug.LogError("Texture2DArrayPacker:transformTexture: edgeLength isn't dividable by 2 !");
        }

        Color[] pixels = null;

        if (texture.width < edgeLength)
        {
            pixels = texture.GetPixels();

            while (System.Math.Sqrt(pixels.Length) < edgeLength)
            {
                pixels = upscaleTexture(pixels);
            }
        }

        if (texture.format != format || pixels != null)
        {
            // create new texture

            if (pixels == null)
            {
                pixels = texture.GetPixels();
            }

            Texture2D returnValue = new Texture2D(edgeLength, edgeLength, format, mipChain);

            returnValue.SetPixels(pixels);
            returnValue.Apply();

            return returnValue;
        }
        else
        {
            return texture;
        }
    }

    private static Color[] upscaleTexture(Color[] textureColors)
    {
        int oldEdgeLength = (int)System.Math.Sqrt(textureColors.Length);
        int newEdgeLength = oldEdgeLength * 2;

        Color[] outputPixels = new Color[newEdgeLength * newEdgeLength];

        for (int i = 0; i < oldEdgeLength; i++)
        {
            for (int j = 0; j < oldEdgeLength; j++)
            {
                outputPixels[newEdgeLength * j * 2 + i * 2] = textureColors[oldEdgeLength * j + i];
                outputPixels[newEdgeLength * j * 2 + i * 2 + 1] = textureColors[oldEdgeLength * j + i];
                outputPixels[newEdgeLength * (j * 2 + 1) + i * 2] = textureColors[oldEdgeLength * j + i];
                outputPixels[newEdgeLength * (j * 2 + 1) + i * 2 + 1] = textureColors[oldEdgeLength * j + i];
            }
        }

        return outputPixels;
    }

    private static Color[] downscaleTexture(Color[] textureColors)
    {
        int oldEdgeLength = (int)System.Math.Sqrt(textureColors.Length);
        int newEdgeLength = oldEdgeLength / 2;

        Color[] outputPixels = new Color[newEdgeLength * newEdgeLength];

        Vector4 tempColor = new Vector4();

        for (int i = 0; i < newEdgeLength; i++)
        {
            for (int j = 0; j < newEdgeLength; j++)
            {
                tempColor.x = (textureColors[i * 2 + j * 2 * oldEdgeLength].r + textureColors[i * 2 + 1 + j * 2 * oldEdgeLength].r + textureColors[i * 2 + (j * 2 + 1) * oldEdgeLength].r + textureColors[i * 2 + 1 + (j * 2 + 1) * oldEdgeLength].r) / 4;
                tempColor.y = (textureColors[i * 2 + j * 2 * oldEdgeLength].g + textureColors[i * 2 + 1 + j * 2 * oldEdgeLength].g + textureColors[i * 2 + (j * 2 + 1) * oldEdgeLength].g + textureColors[i * 2 + 1 + (j * 2 + 1) * oldEdgeLength].g) / 4;
                tempColor.z = (textureColors[i * 2 + j * 2 * oldEdgeLength].b + textureColors[i * 2 + 1 + j * 2 * oldEdgeLength].b + textureColors[i * 2 + (j * 2 + 1) * oldEdgeLength].b + textureColors[i * 2 + 1 + (j * 2 + 1) * oldEdgeLength].b) / 4;
                tempColor.w = (textureColors[i * 2 + j * 2 * oldEdgeLength].a + textureColors[i * 2 + 1 + j * 2 * oldEdgeLength].a + textureColors[i * 2 + (j * 2 + 1) * oldEdgeLength].a + textureColors[i * 2 + 1 + (j * 2 + 1) * oldEdgeLength].a) / 4;

                outputPixels[newEdgeLength * j + i] = new Color(tempColor.x, tempColor.y, tempColor.z, tempColor.w);
            }
        }

        return outputPixels;
    }

    private static Vector4 colorToVec4(Color col)
    {
        return new Vector4(col.r, col.g, col.b, col.a);
    }
}

#endif
