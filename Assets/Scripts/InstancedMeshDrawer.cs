using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancedMeshDrawer
{
    private const int INSTANCES_ARRAY_SIZE = 1023;

    private LODSpritesEntity[] m_prefabObjects = null;
    private Matrix4x4[][][] m_instancesData = null; // dim1: prefabs, dim2: arrays for prefab, dim3 array entry
    private bool[] m_nullPrefabs = null;
    private bool m_instancesReady = false;

    public void setPrefabs(LODSpritesEntity[] distantObject)
    {
        m_prefabObjects = distantObject;
        m_instancesData = new Matrix4x4[m_prefabObjects.Length][][];
        m_nullPrefabs = new bool[m_prefabObjects.Length];

        for(int i = 0; i < m_prefabObjects.Length; i++)
        {
            if(m_prefabObjects[i] == null)
            {
                m_nullPrefabs[i] = true;
            }
            else
            {
                m_nullPrefabs[i] = false;
            }
        }
    }

    public void setInstancesDataForPrefab(int prefabIndex, List<Vector3> positions, List<Quaternion> rotations,Vector3 observerPosition)
    {
        //Debug.Log("setInstancesDataForPrefab: " + prefabIndex + ", " + positions.Count);

        if (prefabIndex < 0 || prefabIndex >= m_prefabObjects.Length)
        {
            Debug.LogError("InstancedMeshDrawer: setInstancesData: prefabIndex out of bounds: " + prefabIndex + " [0-" + (m_prefabObjects.Length - 1) + "]");
            return;
        }

        if(m_prefabObjects[prefabIndex] == null)
        {
            return;
        }

        int instancesCounter = 0;
        int intArrayCounter = 0;

        m_instancesData[prefabIndex] = new Matrix4x4[divideRoundUp(positions.Count, INSTANCES_ARRAY_SIZE)][];

        for (int i = 0; i < positions.Count; i++)
        {
            if (instancesCounter == 0)
            {
                int size = positions.Count - i;
                if (size > INSTANCES_ARRAY_SIZE)
                {
                    size = INSTANCES_ARRAY_SIZE;
                }

                m_instancesData[prefabIndex][intArrayCounter] = new Matrix4x4[size];
            }

            Vector3 connectionVec = positions[i] - observerPosition;
            connectionVec.y = 0;
            Quaternion rotation = Quaternion.LookRotation(connectionVec) * Quaternion.Euler(0,90,0);

            m_instancesData[prefabIndex][intArrayCounter][instancesCounter] = Matrix4x4.TRS(positions[i] + Vector3.up * m_prefabObjects[prefabIndex].m_offsetY, rotation, m_prefabObjects[prefabIndex].m_size);

            instancesCounter++;

            if (instancesCounter >= INSTANCES_ARRAY_SIZE)
            {
                instancesCounter = 0;
                intArrayCounter++;
            }
        }

        m_instancesReady = true;
    }

    public void drawInstances()
    {
        if (m_instancesReady)
        {
            /*
            string arrayDimensions = "";
            for (int i = 0; i < m_instancesData.Length; i++)
            {
                arrayDimensions += i + ": " + m_instancesData[i].Length + " ; ";
                for (int j = 0; j < m_instancesData[i].Length; j++)
                {
                    arrayDimensions += m_instancesData[i][j].Length + " ; ";
                }
            }
            Debug.Log("drawInstances: " + arrayDimensions);
            */

            for (int i = 0; i < m_instancesData.Length; i++)
            {
                if(m_nullPrefabs[i])
                {
                    continue;
                }

                for (int j = 0; j < m_instancesData[i].Length; j++)
                {
                    Graphics.DrawMeshInstanced(m_prefabObjects[i].m_mesh, 0, m_prefabObjects[i].m_spritesMaterials[0], m_instancesData[i][j], m_instancesData[i][j].Length, null, m_prefabObjects[i].m_shadowCastingMode, m_prefabObjects[i].m_receiveShadows);
                }
            }
        }
        else
        {
            Debug.LogWarning("InstancedMeshDrawer: drawInstances: instances data is not set !");
        }
    }

    public bool instancesReady
    {
        get
        {
            return m_instancesReady;
        }
    }

    public static int divideRoundUp(int value, int divider)
    {
        float fResult = (float)value / divider;
        int iResult = (int)fResult;

        if (fResult > iResult)
        {
            iResult++;
        }

        return iResult;
    }
}
