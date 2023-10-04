using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ByteCompressedFloat
{
    private float[,] m_inputArray;
    private byte[,] m_compressedArray;
    private float m_multiplier = -1;
    private float m_offset = float.MinValue;
    private bool m_isCompressed = false;

    public ByteCompressedFloat(int dimensionX, int dimensionY)
    {
        m_inputArray = new float[dimensionX, dimensionY];
    }

    public float this[int x, int y]
    {
        get
        {
            if (m_isCompressed)
            {
                return m_compressedArray[x, y] * m_multiplier + m_offset;
            }
            else
            {
                return m_inputArray[x, y];
            }
        }
        set
        {
            if (m_isCompressed)
            {
                throw new System.NotSupportedException("the input array has been compressed and released. it can no longer change values !");
            }
            else
            {
                m_inputArray[x, y] = value;
            }
        }
    }

    public void compress()
    {
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int i = 0; i < m_inputArray.GetLength(0); i++)
        {
            for (int j = 0; j < m_inputArray.GetLength(1); j++)
            {
                minValue = Mathf.Min(minValue, m_inputArray[i, j]);
                maxValue = Mathf.Max(maxValue, m_inputArray[i, j]);
            }
        }

        m_offset = minValue;

        if (minValue < 0)
        {
            maxValue += Mathf.Abs(minValue);
            minValue = 0;
        }
        else
        {
            maxValue -= minValue;
            minValue = 0;
        }

        m_multiplier = maxValue / byte.MaxValue;

        m_compressedArray = new byte[m_inputArray.GetLength(0), m_inputArray.GetLength(1)];

        for (int i = 0; i < m_inputArray.GetLength(0); i++)
        {
            for (int j = 0; j < m_inputArray.GetLength(1); j++)
            {
                m_compressedArray[i, j] = (byte)Mathf.Max(0, m_inputArray[i, j] / m_multiplier - m_offset);
            }
        }

        m_isCompressed = true;
        m_inputArray = null;
    }

    public int GetLength(int dimension)
    {
        if (m_isCompressed)
        {
            return m_compressedArray.GetLength(dimension);
        }
        else
        {
            return m_inputArray.GetLength(dimension);
        }
    }
}
