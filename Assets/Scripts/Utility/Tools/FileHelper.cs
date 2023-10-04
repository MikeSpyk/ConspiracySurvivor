using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public static class FileHelper 
{
    public static void writeFileToDisk(string path, string fileName, bool overwrite, byte[] data)
    {
        string[] pathSplitted = path.Split('\\');

        string pathBuilder = "";

        for(int i = 0;i < pathSplitted.Length; i++)
        {
            pathBuilder += pathSplitted[i] + "\\";

            if(!Directory.Exists(pathBuilder))
            {
                Directory.CreateDirectory(pathBuilder);
            }
        }

        pathBuilder += fileName;

        if (File.Exists(pathBuilder))
        {
            if(overwrite)
            {
                File.WriteAllBytes(pathBuilder, data);
            }
        }
        else
        {
            File.WriteAllBytes(pathBuilder, data);
        }
    }

    public static void writeFileToDisk(string fullPath, string data, bool overwrite)
    {
        try
        {
            string[] pathSplitted = fullPath.Split('\\');

            for (int i = 1; i < pathSplitted.Length; i++) // i=1: 0 is rootDir(C:,D:....)
            {
                string pathPiece = "";

                for (int j = 0; j <= i; j++)
                {
                    pathPiece += pathSplitted[j] + "\\";
                }

                if (i < pathSplitted.Length - 1) // Directory
                {
                    if (!Directory.Exists(pathPiece))
                    {
                        Directory.CreateDirectory(pathPiece);
                        Debug.LogWarning("FileHelper: Created non-existent directory \"" + pathPiece + "\"");
                    }
                }
                else // file
                {
                    pathPiece = pathPiece.Remove(pathPiece.Length - 1, 1);

                    if (File.Exists(pathPiece) && !overwrite)
                    {

                    }
                    else
                    {
                        File.WriteAllText(pathPiece, data);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("FileHelper: Error creating file: " + ex);
        }
    }
}
