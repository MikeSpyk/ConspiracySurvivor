using UnityEngine;
using System.Collections;



public class NeuralNetwork
{
    public NeuralNetwork()
    {

    }

    public NeuralNetwork(Genome genome)
    {
        setGenome(genome);
    }

    private bool m_isInputCountSet = false;
    private bool m_areInputsWeightsSet = false;
    private bool m_areNeuronsWeightsSet = false;
    private bool m_isNeuronsCountSet = false;
    private bool m_isOutputsCountSet = false;
    private int m_inputsCount = -1;
    private Genome m_genome = null;
    private float[] m_outputs = null;
    private float[,] m_neurons = null;
    private float[,] m_inputsWeights = null;
    private float[,] m_outputsWeights = null;
    private float[,,] m_neuronsWeights = null;

    private float getRandomZeroToOne(int input)
    {
        return Mathf.PerlinNoise(2000.13f * input * Time.realtimeSinceStartup, 2000.13f * input * Time.realtimeSinceStartup);
    }

    private float getRandomMinusOneToOne(int input)
    {
        return (getRandomZeroToOne(input) - 0.5f) * 2;
    }

    private float sigmoid(float x)
    {
        return 2 / (1 + Mathf.Exp(-6 * x)) - 1;
    }

    private void showErrorMessage(string Message)
    {
        Debug.LogError("NeuralNetwork:" + Message);
    }

    public void setInputsCount(int count)
    {
        if (count > 0)
        {
            m_inputsCount = count;
        }
        else
        {
            showErrorMessage("Inputs-array length is <= 0");
            return;
        }
        m_isInputCountSet = true;
    }

    public void setNeuronsCount(int sizeX, int sizeY)
    {
        m_neurons = new float[sizeX, sizeY];

        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                m_neurons[i, j] = 0f;
            }
        }
        m_neuronsWeights = new float[sizeX - 1, sizeY, sizeY];

        m_isNeuronsCountSet = true;
    }

    public void setGenome(Genome genome)
    {
        if (genome == null)
        {
            Debug.LogWarning("NeuralNetwork: setGenome: genome = null");
        }
        else
        {
            m_genome = genome;

            setInputsCount(genome.m_inputsWeights.GetLength(0));
            setNeuronsCount(genome.m_neuronsWeights.GetLength(0), genome.m_neuronsWeights.GetLength(1));
            setOutputsCount(genome.m_outputsWeights.GetLength(1));
            setInputWeights(genome.m_inputsWeights);
            setNeuronWeights(genome.m_neuronsWeights);
            setOutputWeights(genome.m_outputsWeights);
        }
    }

    public void setOutputsCount(int outputscount)
    {
        m_outputs = new float[outputscount];

        for (int i = 0; i < m_outputs.Length; i++)
        {
            m_outputs[i] = 0f;
        }
        m_outputsWeights = new float[m_neurons.GetLength(1), outputscount];
        m_isOutputsCountSet = true;
    }

    public void setInputWeights(float[,] weights)
    {
        m_inputsWeights = new float[weights.GetLength(0), weights.GetLength(1)];

        for (int i = 0; i < weights.GetLength(0); i++)
        {
            for (int j = 0; j < weights.GetLength(1); j++)
            {
                m_inputsWeights[i, j] = weights[i, j];
            }
        }
        m_areInputsWeightsSet = true;
    }

    public void setNeuronWeights(float[,,] weights)
    {
        m_neuronsWeights = new float[weights.GetLength(0), weights.GetLength(1), weights.GetLength(2)];

        for (int i = 0; i < weights.GetLength(0); i++)
        {
            for (int j = 0; j < weights.GetLength(1); j++)
            {
                for (int k = 0; k < weights.GetLength(2); k++)
                {
                    m_neuronsWeights[i, j, k] = weights[i, j, k];
                }
            }
        }
        m_areNeuronsWeightsSet = true;
    }

    public void setOutputWeights(float[,] weights)
    {
        m_outputsWeights = new float[weights.GetLength(0), weights.GetLength(1)];
        for (int i = 0; i < weights.GetLength(0); i++)
        {
            for (int j = 0; j < weights.GetLength(1); j++)
            {
                m_outputsWeights[i, j] = weights[i, j];
            }
        }
    }

    public void setAllWeightsRandom() // all kinds of weights: neurons, inputs, outputs
    {
        if (!m_isNeuronsCountSet)
        {
            showErrorMessage("Error: neurons size not set");
        }
        else if (!m_isInputCountSet)
        {
            showErrorMessage("Error: input count not set");
        }
        else
        {
            m_inputsWeights = new float[m_inputsCount, m_neurons.GetLength(1)];
            for (int i = 0; i < m_inputsCount; i++)
            {
                for (int j = 0; j < m_neurons.GetLength(1); j++)
                {
                    m_inputsWeights[i, j] = getRandomMinusOneToOne(i + j);
                }
            }

            m_neuronsWeights = new float[m_neurons.GetLength(0), m_neurons.GetLength(1), m_neurons.GetLength(1)];
            for (int i = 0; i < m_neurons.GetLength(0); i++)
            {
                for (int j = 0; j < m_neurons.GetLength(1); j++)
                {
                    for (int k = 0; k < m_neurons.GetLength(1); k++)
                    {
                        m_neuronsWeights[i, j, k] = getRandomMinusOneToOne(i + j + k);
                    }
                }
            }
            for (int i = 0; i < m_neurons.GetLength(1); i++)
            {
                for (int j = 0; j < m_outputs.Length; j++)
                {
                    m_outputsWeights[i, j] = getRandomMinusOneToOne(i + j);
                }
            }
            m_areNeuronsWeightsSet = true;
            m_areInputsWeightsSet = true;
        }
    }

    public Genome getGenomeRef()
    {
        if (m_genome == null)
        {
            m_genome = new Genome();
        }
        m_genome.m_inputsWeights = m_inputsWeights;
        m_genome.m_neuronsWeights = m_neuronsWeights;
        m_genome.m_outputsWeights = m_outputsWeights;

        return m_genome;
    }

    public void doRandomChangesWeights(int count)
    {
        float randomValue;
        for (int i = 0; i < count; i++)
        {
            randomValue = getRandomZeroToOne(i + 1);

            if (randomValue < .40f)
            {
                m_neuronsWeights[Mathf.Max((int)(getRandomZeroToOne(i + 2) * (m_neuronsWeights.GetLength(0) - 1)), 0), Mathf.Max((int)(getRandomZeroToOne(i + 3) * (m_neuronsWeights.GetLength(1) - 1)), 0), Mathf.Max((int)(getRandomZeroToOne(i + 4) * (m_neuronsWeights.GetLength(2) - 1)), 0)] = getRandomMinusOneToOne(i);
            }
            else if (randomValue < .53f)
            {
                m_inputsWeights[Mathf.Max((int)(getRandomZeroToOne(i + 2) * (m_inputsWeights.GetLength(0) - 1)), 0), Mathf.Max((int)(getRandomZeroToOne(i + 3) * (m_inputsWeights.GetLength(1) - 1)), 0)] = getRandomMinusOneToOne(i);
            }
            else
            {
                m_outputsWeights[Mathf.Max((int)(getRandomZeroToOne(i + 2) * (m_outputsWeights.GetLength(0) - 1)), 0), Mathf.Max((int)(getRandomZeroToOne(i + 3) * (m_outputsWeights.GetLength(1) - 1)), 0)] = getRandomMinusOneToOne(i);
            }
        }
    }

    private void resetNeuronsAndOutput()
    {
        for (int i = 0; i < m_neurons.GetLength(0); i++)
        {
            for (int j = 0; j < m_neurons.GetLength(1); j++)
            {
                m_neurons[i, j] = 0f;
            }
        }
        for (int i = 0; i < m_outputs.Length; i++)
        {
            m_outputs[i] = 0f;
        }
    }

    /// <summary>
    /// returns outputs (solution to given inputs)
    /// </summary>
    /// <param name="Inputs"></param>
    /// <returns></returns>
	public float[] think(float[] Inputs)
    {
        // inputs zwischem 1 und 0
        if (!m_isInputCountSet || !m_areInputsWeightsSet || !m_areNeuronsWeightsSet || !m_isNeuronsCountSet || !m_isOutputsCountSet)
        {
            showErrorMessage("Error: not all preparations are done");
            return null;
        }
        if (Inputs.Length > m_inputsCount || Inputs.Length <= 0)
        {
            showErrorMessage("Error: wrong input-count");
            return null;
        }
        resetNeuronsAndOutput();

        for (int i = 0; i < m_inputsCount; i++) // input neurons
        {
            for (int j = 0; j < m_neurons.GetLength(1); j++)
            {
                m_neurons[0, j] += Inputs[i] * m_inputsWeights[i, j];
            }
        }

        for (int i = 0; i < m_neurons.GetLength(1); i++)
        {
            if (m_neurons[0, i] == 0)
            {
            }
            else
            {
                m_neurons[0, i] = sigmoid(m_neurons[0, i]);
            }
        }

        for (int i = 1; i < m_neurons.GetLength(0); i++) // deep
        {
            for (int j = 0; j < m_neurons.GetLength(1); j++)
            {
                for (int k = 0; k < m_neurons.GetLength(1); k++)
                {
                    m_neurons[i, j] += m_neurons[i - 1, k] * m_neuronsWeights[i - 1, k, j];
                }
            }
            for (int j = 0; j < m_neurons.GetLength(1); j++)
            {
                if (m_neurons[i, j] == 0)
                {
                }
                else
                {
                    m_neurons[i, j] = sigmoid(m_neurons[0, j]);
                }
            }
        }

        /*
        Debug.Log("m_outputs.length=" + m_outputs.Length);
        Debug.Log("m_neurons.length=" + m_neurons.GetLength(0) + "," + m_neurons.GetLength(1));
        Debug.Log("m_outputsWeights.length=" + m_outputsWeights.GetLength(0) + "," + m_outputsWeights.GetLength(1));
        */

        for (int i = 0; i < m_neurons.GetLength(1); i++) // output neurons
        {
            for (int j = 0; j < m_outputs.Length; j++)
            {
                //Debug.Log(string.Format("i={0}/{1}; j={2}/{3} ", i, m_neurons.GetLength(1), j, m_outputs.Length));
                m_outputs[j] += m_neurons[m_neurons.GetLength(0) - 1, i] * m_outputsWeights[i, j];
            }
        }
        for (int i = 0; i < m_outputs.Length; i++)
        {
            if (m_outputs[i] == 0)
            {
            }
            else
            {
                m_outputs[i] = sigmoid(m_outputs[i]);
            }
        }
        return m_outputs;
    }

}
