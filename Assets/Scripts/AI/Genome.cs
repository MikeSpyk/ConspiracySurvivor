using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Text;

public class Genome
{
    public Genome()
    {

    }
    public Genome(Genome original)
    {
        cloneFrom(original);
    }

    public float m_diversity = 0;
    public float m_fitness = 0;
    public float[,] m_inputsWeights;
    public float[,,] m_neuronsWeights;
    public float[,] m_outputsWeights;
    private bool m_changedGenomeSum = true;
    private float m_GenomeSum = float.MinValue;

    private float getRandomZeroToOne(int input)
    {
        //return Mathf.PerlinNoise(2000.13f * input * Time.realtimeSinceStartup, 2000.13f * input * Time.realtimeSinceStartup);
        return UnityEngine.Random.value;
    }

    private float getRandomMinusOneToOne(int input)
    {
        return (getRandomZeroToOne(input) - 0.5f) * 2;
    }

    private bool getRandomBool(int input)
    {
        if (getRandomZeroToOne(input) > .5f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void createRandom(int inputsCount, int neuronsCountX, int neuronsCountY, int outputsCount, int randomFactor)
    {
        int counter1 = (int)(inputsCount * neuronsCountX * neuronsCountY * outputsCount * randomFactor * Time.realtimeSinceStartup);

        m_inputsWeights = new float[inputsCount, neuronsCountY];
        for (int i = 0; i < inputsCount; i++)
        {
            for (int j = 0; j < neuronsCountY; j++)
            {
                m_inputsWeights[i, j] = getRandomMinusOneToOne(counter1);
                counter1++;
            }
        }

        m_neuronsWeights = new float[neuronsCountX, neuronsCountY, neuronsCountY];
        for (int i = 0; i < neuronsCountX; i++)
        {
            for (int j = 0; j < neuronsCountY; j++)
            {
                for (int k = 0; k < neuronsCountY; k++)
                {
                    m_neuronsWeights[i, j, k] = getRandomMinusOneToOne(counter1);
                    counter1++;
                }
            }
        }
        m_outputsWeights = new float[neuronsCountY, outputsCount];
        for (int i = 0; i < neuronsCountY; i++)
        {
            for (int j = 0; j < outputsCount; j++)
            {
                m_outputsWeights[i, j] = getRandomMinusOneToOne(counter1);
                counter1++;
            }
        }
    }

    public void mixBothEvenly(Genome genomeB)
    {
        // Attention: genomes are always reference, that means genomeB can get altered! 
        for (int i = 0; i < m_inputsWeights.GetLength(0); i++)
        {
            for (int j = 0; j < m_inputsWeights.GetLength(1); j++)
            {
                if (getRandomBool(i + j))
                {
                    this.m_inputsWeights[i, j] = genomeB.m_inputsWeights[i, j];
                }
                else
                {
                    genomeB.m_inputsWeights[i, j] = this.m_inputsWeights[i, j];
                }
            }
        }

        for (int i = 0; i < m_neuronsWeights.GetLength(0); i++)
        {
            for (int j = 0; j < m_neuronsWeights.GetLength(1); j++)
            {
                for (int k = 0; k < m_neuronsWeights.GetLength(2); k++)
                {
                    if (getRandomBool(i + j + k))
                    {
                        this.m_neuronsWeights[i, j, k] = genomeB.m_neuronsWeights[i, j, k];
                    }
                    else
                    {
                        genomeB.m_neuronsWeights[i, j, k] = this.m_neuronsWeights[i, j, k];
                    }
                }
            }
        }

        for (int i = 0; i < m_outputsWeights.GetLength(0); i++)
        {
            for (int j = 0; j < m_outputsWeights.GetLength(1); j++)
            {
                if (getRandomBool(i + j))
                {
                    this.m_outputsWeights[i, j] = genomeB.m_outputsWeights[i, j];
                }
                else
                {
                    genomeB.m_outputsWeights[i, j] = this.m_outputsWeights[i, j];
                }
            }
        }
    }

    public void mixWith(Genome InputGenome, int count)
    {
        float randomValue = 0;
        for (int i = 0; i < count; i++)
        {
            randomValue = getRandomZeroToOne(i + 1);

            if (randomValue < .40f)
            {
                int indexX = (int)(getRandomZeroToOne(i + 2) * (m_neuronsWeights.GetLength(0) - 1));
                int indexY = (int)(getRandomZeroToOne(i + 3) * (m_neuronsWeights.GetLength(1) - 1));
                int indexZ = (int)(getRandomZeroToOne(i + 4) * (m_neuronsWeights.GetLength(2) - 1));
                this.m_neuronsWeights[indexX, indexY, indexZ] = InputGenome.m_neuronsWeights[indexX, indexY, indexZ];
            }
            else if (randomValue < .53f)
            {
                int indexX = (int)(getRandomZeroToOne(i + 2) * (m_inputsWeights.GetLength(0) - 1));
                int indexY = (int)(getRandomZeroToOne(i + 3) * (m_inputsWeights.GetLength(1) - 1));
                this.m_inputsWeights[indexX, indexY] = InputGenome.m_inputsWeights[indexX, indexY];
            }
            else
            {
                int indexX = (int)(getRandomZeroToOne(i + 2) * (m_outputsWeights.GetLength(0) - 1));
                int indexY = (int)(getRandomZeroToOne(i + 3) * (m_outputsWeights.GetLength(1) - 1));
                this.m_outputsWeights[indexX, indexY] = InputGenome.m_outputsWeights[indexX, indexY];
            }
        }
        m_changedGenomeSum = true;
    }

    public void cloneFrom(Genome inputGenome)
    {
        m_inputsWeights = new float[inputGenome.m_inputsWeights.GetLength(0), inputGenome.m_inputsWeights.GetLength(1)];
        for (int i = 0; i < inputGenome.m_inputsWeights.GetLength(0); i++)
        {
            for (int j = 0; j < inputGenome.m_inputsWeights.GetLength(1); j++)
            {
                this.m_inputsWeights[i, j] = inputGenome.m_inputsWeights[i, j];
            }
        }

        m_neuronsWeights = new float[inputGenome.m_neuronsWeights.GetLength(0), inputGenome.m_neuronsWeights.GetLength(1), inputGenome.m_neuronsWeights.GetLength(2)];
        for (int i = 0; i < inputGenome.m_neuronsWeights.GetLength(0); i++)
        {
            for (int j = 0; j < inputGenome.m_neuronsWeights.GetLength(1); j++)
            {
                for (int k = 0; k < inputGenome.m_neuronsWeights.GetLength(2); k++)
                {
                    this.m_neuronsWeights[i, j, k] = inputGenome.m_neuronsWeights[i, j, k];
                }
            }
        }

        m_outputsWeights = new float[inputGenome.m_outputsWeights.GetLength(0), inputGenome.m_outputsWeights.GetLength(1)];
        for (int i = 0; i < inputGenome.m_outputsWeights.GetLength(0); i++)
        {
            for (int j = 0; j < inputGenome.m_outputsWeights.GetLength(1); j++)
            {
                this.m_outputsWeights[i, j] = inputGenome.m_outputsWeights[i, j];
            }
        }

        m_fitness = inputGenome.m_fitness;
        m_diversity = inputGenome.m_diversity;
    }

    public float getGenomeID()
    {
        if (m_changedGenomeSum)
        {
            calcGenomeSum();
            m_changedGenomeSum = false;
        }
        return m_GenomeSum;
    }

    private void calcGenomeSum()
    {
        m_GenomeSum = 0;
        int counter = 0;
        for (int i = 0; i < this.m_inputsWeights.GetLength(0); i++)
        {
            for (int j = 0; j < this.m_inputsWeights.GetLength(1); j++)
            {
                m_GenomeSum += this.m_inputsWeights[i, j];
                counter++;
            }
        }

        for (int i = 0; i < this.m_neuronsWeights.GetLength(0); i++)
        {
            for (int j = 0; j < this.m_neuronsWeights.GetLength(1); j++)
            {
                for (int k = 0; k < this.m_neuronsWeights.GetLength(2); k++)
                {
                    m_GenomeSum += this.m_neuronsWeights[i, j, k];
                    counter++;
                }
            }
        }

        for (int i = 0; i < this.m_outputsWeights.GetLength(0); i++)
        {
            for (int j = 0; j < this.m_outputsWeights.GetLength(1); j++)
            {
                m_GenomeSum += this.m_outputsWeights[i, j];
                counter++;
            }
        }
        m_GenomeSum /= counter;
    }

    public void saveToDisk(string path, string name, bool overwrite)
    {
        StringBuilder strBuilder = new StringBuilder();

        strBuilder.AppendLine(name);

        strBuilder.AppendLine("" + m_inputsWeights.GetLength(0));
        strBuilder.AppendLine("" + m_inputsWeights.GetLength(1));

        strBuilder.AppendLine("" + m_neuronsWeights.GetLength(0));
        strBuilder.AppendLine("" + m_neuronsWeights.GetLength(1));
        strBuilder.AppendLine("" + m_neuronsWeights.GetLength(2));

        strBuilder.AppendLine("" + m_outputsWeights.GetLength(0));
        strBuilder.AppendLine("" + m_outputsWeights.GetLength(1));

        for (int i = 0; i < m_inputsWeights.GetLength(0); i++)
        {
            for (int j = 0; j < m_inputsWeights.GetLength(1); j++)
            {
                strBuilder.AppendLine("" + m_inputsWeights[i, j]);
            }
        }

        for (int i = 0; i < m_neuronsWeights.GetLength(0); i++)
        {
            for (int j = 0; j < m_neuronsWeights.GetLength(1); j++)
            {
                for (int k = 0; k < m_neuronsWeights.GetLength(2); k++)
                {
                    strBuilder.AppendLine("" + m_neuronsWeights[i, j, k]);
                }
            }
        }

        for (int i = 0; i < m_outputsWeights.GetLength(0); i++)
        {
            for (int j = 0; j < m_outputsWeights.GetLength(1); j++)
            {
                strBuilder.AppendLine("" + m_outputsWeights[i, j]);
            }
        }

        FileHelper.writeFileToDisk(path + "\\" + name + ".genome", strBuilder.ToString(), overwrite);
    }

    public bool tryLoadFromDisk(int inputsCount, int neuronsCountX, int neuronsCountY, int outputsCount, string fullPath)
    {
        StreamReader stream = null;

        try
        {
            stream = new StreamReader(fullPath);

            string name = stream.ReadLine();
            int inputsDiskCountX = int.Parse(stream.ReadLine());
            int inputsDiskCountY = int.Parse(stream.ReadLine());

            int neuronsDiskCountX = int.Parse(stream.ReadLine());
            int neuronsDiskCountY = int.Parse(stream.ReadLine());
            int neuronsDiskCountZ = int.Parse(stream.ReadLine());

            int outputsDiskCountX = int.Parse(stream.ReadLine());
            int outputsDiskCountY = int.Parse(stream.ReadLine());

            if (inputsDiskCountX != inputsCount || neuronsDiskCountX != neuronsCountX || neuronsDiskCountY != neuronsCountY || outputsDiskCountY != outputsCount)
            {
                Debug.LogWarning(string.Format("Genome:tryLoadFromDisk:weights-length differ: inputsDiskCountX:{0},inputsCount:{1},neuronsDiskCountX:{2},neuronsCountX:{3},neuronsDiskCountY:{4},neuronsCountY:{5},outputsDiskCountY:{6},outputsCount:{7}"
                                                , inputsDiskCountX, inputsCount, neuronsDiskCountX, neuronsCountX, neuronsDiskCountY, neuronsCountY, outputsDiskCountY, outputsCount));
                stream.Close();
                stream.Dispose();
                return false;
            }

            m_inputsWeights = new float[inputsDiskCountX, inputsDiskCountY];
            for (int i = 0; i < m_inputsWeights.GetLength(0); i++)
            {
                for (int j = 0; j < m_inputsWeights.GetLength(1); j++)
                {
                    m_inputsWeights[i, j] = float.Parse(stream.ReadLine());
                }
            }

            m_neuronsWeights = new float[neuronsDiskCountX, neuronsDiskCountY, neuronsDiskCountZ];
            for (int i = 0; i < m_neuronsWeights.GetLength(0); i++)
            {
                for (int j = 0; j < m_neuronsWeights.GetLength(1); j++)
                {
                    for (int k = 0; k < m_neuronsWeights.GetLength(2); k++)
                    {
                        m_neuronsWeights[i, j, k] = float.Parse(stream.ReadLine());
                    }
                }
            }

            m_outputsWeights = new float[outputsDiskCountX, outputsDiskCountY];
            for (int i = 0; i < m_outputsWeights.GetLength(0); i++)
            {
                for (int j = 0; j < m_outputsWeights.GetLength(1); j++)
                {
                    m_outputsWeights[i, j] = float.Parse(stream.ReadLine());
                }
            }

            stream.Close();
            stream.Dispose();

            return true;
        }
        catch (Exception ex)
        {
            if (stream != null)
            {
                stream.Close();
                stream.Dispose();
            }

            Debug.LogWarning("Genome:tryLoadFromDisk:exception:" + ex);

            return false;
        }
    }

    public void doRandomChangesWeights(int count)
    {
        float randomValue;
        for (int i = 0; i < count; i++)
        {
            randomValue = getRandomZeroToOne(i + 1);

            if (randomValue < .33f)
            {
                m_neuronsWeights[Mathf.Max((int)(getRandomZeroToOne(i + 2) * (m_neuronsWeights.GetLength(0) - 1)), 0), Mathf.Max((int)(getRandomZeroToOne(i + 3) * (m_neuronsWeights.GetLength(1) - 1)), 0), Mathf.Max((int)(getRandomZeroToOne(i + 4) * (m_neuronsWeights.GetLength(2) - 1)), 0)] = getRandomMinusOneToOne(i);
            }
            else if (randomValue < .66f)
            {
                m_inputsWeights[Mathf.Max((int)(getRandomZeroToOne(i + 2) * (m_inputsWeights.GetLength(0) - 1)), 0), Mathf.Max((int)(getRandomZeroToOne(i + 3) * (m_inputsWeights.GetLength(1) - 1)), 0)] = getRandomMinusOneToOne(i);
            }
            else
            {
                m_outputsWeights[Mathf.Max((int)(getRandomZeroToOne(i + 2) * (m_outputsWeights.GetLength(0) - 1)), 0), Mathf.Max((int)(getRandomZeroToOne(i + 3) * (m_outputsWeights.GetLength(1) - 1)), 0)] = getRandomMinusOneToOne(i);
            }
        }
    }

}
