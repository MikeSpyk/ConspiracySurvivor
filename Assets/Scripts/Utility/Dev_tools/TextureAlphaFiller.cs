using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureAlphaFiller : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Texture2D m_alphaMap;
    [SerializeField] private Texture2D m_baseTexture;
    [SerializeField] private string m_saveAssetPath = "Assets/Textures/TEST/Alpha.png";
    [SerializeField] private bool m_invertAlpha = false;
    [Header("Interface")]
    [SerializeField] private bool m_execute = false;

    // Update is called once per frame
    void Update()
    {
        if (m_execute)
        {
            m_execute = false;

            Texture2D output = new Texture2D(m_baseTexture.width, m_baseTexture.height);

            Color[] baseColors = m_baseTexture.GetPixels();
            Color[] alphaColors = m_alphaMap.GetPixels();

            if (m_invertAlpha)
            {
                for (int i = 0; i < baseColors.Length; i++)
                {
                    baseColors[i].a = 1f- alphaColors[i].r;
                }
            }
            else
            {
                for (int i = 0; i < baseColors.Length; i++)
                {
                    baseColors[i].a = alphaColors[i].r;
                }
            }

            output.SetPixels(baseColors);
            output.Apply();

            byte[] data = output.EncodeToPNG();

            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, m_saveAssetPath), data);
            Debug.Log("Successfully created \"" + m_saveAssetPath + "\"");

        }
    }
}
