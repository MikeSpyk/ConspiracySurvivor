using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureChannelSwitcher : MonoBehaviour
{
    public enum RGBATarget { R, G, B, A }

    [Header("Settings")]
    [SerializeField] private Texture2D m_inputTexture;
    [SerializeField] private string m_saveAssetPath = "Assets/Textures/TEST/TESTTexture.png";
    [SerializeField] private RGBATarget m_targetR;
    [SerializeField] private RGBATarget m_targetG;
    [SerializeField] private RGBATarget m_targetB;
    [SerializeField] private RGBATarget m_targetA;
    [Header("Interface")]
    [SerializeField] private bool m_execute = false;
    [SerializeField] private bool m_allowOverwrite = false;

    private void Update()
    {
        if (m_execute)
        {
            m_execute = false;

            Color[] pixels = m_inputTexture.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color tempPixel = new Color();

                switch (m_targetR)
                {
                    case RGBATarget.R:
                        {
                            tempPixel.r = pixels[i].r;
                            break;
                        }
                    case RGBATarget.G:
                        {
                            tempPixel.g = pixels[i].r;
                            break;
                        }
                    case RGBATarget.B:
                        {
                            tempPixel.b = pixels[i].r;
                            break;
                        }
                    case RGBATarget.A:
                        {
                            tempPixel.a = pixels[i].r;
                            break;
                        }
                }

                switch (m_targetG)
                {
                    case RGBATarget.R:
                        {
                            tempPixel.r = pixels[i].g;
                            break;
                        }
                    case RGBATarget.G:
                        {
                            tempPixel.g = pixels[i].g;
                            break;
                        }
                    case RGBATarget.B:
                        {
                            tempPixel.b = pixels[i].g;
                            break;
                        }
                    case RGBATarget.A:
                        {
                            tempPixel.a = pixels[i].g;
                            break;
                        }
                }

                switch (m_targetB)
                {
                    case RGBATarget.R:
                        {
                            tempPixel.r = pixels[i].b;
                            break;
                        }
                    case RGBATarget.G:
                        {
                            tempPixel.g = pixels[i].b;
                            break;
                        }
                    case RGBATarget.B:
                        {
                            tempPixel.b = pixels[i].b;
                            break;
                        }
                    case RGBATarget.A:
                        {
                            tempPixel.a = pixels[i].b;
                            break;
                        }
                }

                switch (m_targetA)
                {
                    case RGBATarget.R:
                        {
                            tempPixel.r = pixels[i].a;
                            break;
                        }
                    case RGBATarget.G:
                        {
                            tempPixel.g = pixels[i].a;
                            break;
                        }
                    case RGBATarget.B:
                        {
                            tempPixel.b = pixels[i].a;
                            break;
                        }
                    case RGBATarget.A:
                        {
                            tempPixel.a = pixels[i].a;
                            break;
                        }
                }

                pixels[i] = tempPixel;
            }

            Texture2D outputTex = new Texture2D(m_inputTexture.width, m_inputTexture.height, TextureFormat.RGBAFloat, false);
            outputTex.SetPixels(pixels);
            outputTex.Apply();

            byte[] data = outputTex.EncodeToPNG();

            if (System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, m_saveAssetPath)) && !m_allowOverwrite)
            {
                Debug.LogWarning("File \"" + m_saveAssetPath + "\" already exists. Use \"allowOverwrite\"-property if you want to overwrite it !");
            }
            else
            {
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, m_saveAssetPath), data);

                Debug.Log("Successfully created \"" + m_saveAssetPath + "\"");
            }

            m_allowOverwrite = false;
        }
    }

}
