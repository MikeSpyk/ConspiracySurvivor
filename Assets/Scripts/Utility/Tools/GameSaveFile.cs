using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;

public class GameSaveFile
{
    private const char PARAMETER_SEPERATOR = (char)17;
    private const char PARAMETER_SEPERATOR_END = (char)18;

    private StringBuilder m_currentSectionStrBuilder = new StringBuilder();
    private List<byte> m_currentData = new List<byte>();
    private bool m_isStringSectionOpen = false;

    public void createStringSection()
    {
        if (m_isStringSectionOpen)
        {
            Debug.LogWarning("GameSaveFile: createNewSection: created a new section before closing the last one !");
            closeStringSection();
        }

        m_isStringSectionOpen = true;
    }

    public void closeStringSection()
    {
        byte[] data = getStringBytes(m_currentSectionStrBuilder.ToString());

        m_currentData.AddRange(BitConverter.GetBytes(data.Length));
        m_currentData.AddRange(data);

        m_currentSectionStrBuilder.Clear();
        m_isStringSectionOpen = false;
    }

    public void addSectionParameter(string key, string value)
    {
        m_currentSectionStrBuilder.Append(key);
        m_currentSectionStrBuilder.Append(PARAMETER_SEPERATOR);
        m_currentSectionStrBuilder.Append(value);
        m_currentSectionStrBuilder.Append(PARAMETER_SEPERATOR_END);
    }

    public void addBytes(byte[] bytes)
    {
        m_currentData.AddRange(bytes);
    }
    public void addBytes(List<byte> bytes)
    {
        m_currentData.AddRange(bytes);
    }

    public void writeToDisk(string path, string fileName)
    {
        string basePath = System.IO.Directory.GetCurrentDirectory();
        FileHelper.writeFileToDisk(basePath + path, fileName, true, m_currentData.ToArray());
    }

    private static byte[] getStringBytes(string text)
    {
        return Encoding.Unicode.GetBytes(text);
    }
}
