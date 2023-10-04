using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class EvolutionaryAlgorithm
{
    public EvolutionaryAlgorithm(int inputsCount, int neuronsCountX, int neuronsCountY, int outputsCount)
    {
        m_genomeInputsCount = inputsCount;
        m_genomeNeuronsCountX = neuronsCountX;
        m_genomeNeuronsCountY = neuronsCountY;
        m_genomeOutputsCount = outputsCount;
    }

    public Genome m_bestGenomeLastGen = null;
    private List<Genome> m_disposedGenomes = new List<Genome>();
    private List<Vector2> m_disposedGenomesDiversityDistance = new List<Vector2>(); // distance to use to calculate diversity
    private List<Genome> m_preparedGenomes = new List<Genome>();
    private int m_genomeInputsCount = -1;
    private int m_genomeNeuronsCountX = -1;
    private int m_genomeNeuronsCountY = -1;
    private int m_genomeOutputsCount = -1;
    private int m_generationCount = 0;
    private int m_randomGenomesCounter = 1;
    private float m_bestFitness = 0;
    private float m_averageFitness = 0; // last generation
    private int m_writeBestToDiskCount = 0;
    private string m_writePath = "";
    public int m_mutationsCount = 1;
    public float m_mutatedMemberShare = .5f; // [0-0.5] how many will mutate next gen und how many will just get cloned. lower numbers means less mutations
    public int m_desiredMemberCount = 10;
    public bool m_mix = false;
    public bool m_useDiversity = false;
    public float m_diversityWeight = 1;
    public List<Genome> m_lastGenBest = new List<Genome>();

    public int generationCount
    {
        get
        {
            return m_generationCount;
        }
    }

    public int preparedGenomesCount
    {
        get
        {
            return m_preparedGenomes.Count;
        }
    }

    public float bestFitness
    {
        get
        {
            return m_bestFitness;
        }
    }

    public float averageFitness
    {
        get
        {
            return m_averageFitness;
        }
    }

    public void disposeGenomes(Genome[] genome)
    {
        m_disposedGenomes.AddRange(genome);
    }

    public void disposeGenome(Genome genome)
    {
        m_disposedGenomes.Add(genome);
    }

    public void disposeGenome(Genome genome, Vector2 diversityDistance)
    {
        m_disposedGenomes.Add(genome);
        m_disposedGenomesDiversityDistance.Add(diversityDistance);
    }

    public Genome[] getAllPreparedGenomes()
    {
        Genome[] retrunValue = new Genome[m_desiredMemberCount];

        if(m_preparedGenomes.Count < m_desiredMemberCount)
        {
            Debug.LogWarning("EvolutionaryAlgorithm: getAllPreparedGenomes: not enought prepared genomes. difference: " + (m_desiredMemberCount - m_preparedGenomes.Count));
        }

        // get prepared
        for(int i = 0; i < m_preparedGenomes.Count && i < m_desiredMemberCount; i++)
        {
            retrunValue[i] = m_preparedGenomes[i];
        }

        if(m_bestGenomeLastGen == null)
        {
            if (m_preparedGenomes != null && m_preparedGenomes.Count > 0)
            {
                m_bestGenomeLastGen = m_preparedGenomes[0];
            }
            else
            {
                m_bestGenomeLastGen = getNewRandomGenome();
            }
        }

        // not enough prepared ? --> create new one from best
        for(int i = m_preparedGenomes.Count; i < m_desiredMemberCount; i++)
        {
            retrunValue[i] = new Genome(m_bestGenomeLastGen);
        }

        m_preparedGenomes.Clear();

        return retrunValue;
    }

    public Genome getRandomPreparedGenome()
    {
        if (m_preparedGenomes.Count > 0)
        {
            int randomIndex = (int)(Random.value * (m_preparedGenomes.Count - 1));

            Genome returnValue = m_preparedGenomes[randomIndex];
            m_preparedGenomes.RemoveAt(randomIndex);

            return returnValue;
        }
        else
        {
            if (m_generationCount > 0)
            {
                //Debug.LogWarning("EvolutionaryAlgorithm: not enough inherited genomes available for this generation. creating random genome");
            }

            if(m_bestGenomeLastGen == null)
            {
                return getNewRandomGenome();
            }
            else
            {
                return new Genome(m_bestGenomeLastGen);
            }
     
        }
    }

    private Genome getNewRandomGenome()
    {
        Genome returnValue = new Genome();
        returnValue.createRandom(m_genomeInputsCount, m_genomeNeuronsCountX, m_genomeNeuronsCountY, m_genomeOutputsCount, m_randomGenomesCounter);
        m_randomGenomesCounter++;
        return returnValue;
    }

    public void nextGenWriteBestGenomesToDisk(int count, string folderPath)
    {
        m_writeBestToDiskCount = count;
        m_writePath = folderPath;
    }

    public void loadGenomesFromDisk(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            string[] files = Directory.GetFiles(folderPath);
            {
                for (int i = 0; i < files.Length; i++)
                {
                    Genome temp = new Genome();
                    if (temp.tryLoadFromDisk(m_genomeInputsCount, m_genomeNeuronsCountX, m_genomeNeuronsCountY, m_genomeOutputsCount, files[i]))
                    {
                        m_preparedGenomes.Add(temp);
                    }
                    else
                    {
                        Debug.LogWarning("EvolutionaryAlgorithm: failed to load genome \"" + files[i] + "\"");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("EvolutionaryAlgorithm: folder to load from not found: "+ folderPath);
        }
    }

    public void createNextGeneration()
    {
        if (m_preparedGenomes.Count > 0)
        {
            Debug.LogWarning("EvolutionaryAlgorithm: not all genomes from last generation have been used. remaining genomes: " + m_preparedGenomes.Count);
            m_preparedGenomes.Clear();
        }

        m_generationCount++;

        // find best half

        List<Genome> bestGenomes = new List<Genome>(); //[0] = very best, [n] least better

        int mutateGenomesCount = (int)(m_desiredMemberCount * m_mutatedMemberShare);
        int copyGenomesCount = m_desiredMemberCount - mutateGenomesCount;

        Debug.Log("EvolutionaryAlgorithm: new generation ("+m_generationCount +"): disposed: "+ m_disposedGenomes.Count + ", keeping: " + Mathf.Min(copyGenomesCount, m_disposedGenomes.Count)+", mutating: "+ mutateGenomesCount +", discarding: "+ (m_desiredMemberCount- Mathf.Min(copyGenomesCount, m_disposedGenomes.Count)));

        if(m_useDiversity)
        {
            for (int i = 0; i < m_disposedGenomes.Count; i++)
            {
                float avgDistance = 0;

                for (int j = 0; j < m_disposedGenomes.Count; j++)
                {
                    if(i == j)
                    {
                        continue;
                    }
                    avgDistance += Vector2.Distance(m_disposedGenomesDiversityDistance[i], m_disposedGenomesDiversityDistance[j]);
                }

                avgDistance /= m_disposedGenomes.Count;

                m_disposedGenomes[i].m_diversity = avgDistance * m_diversityWeight;
            }
        }

        m_averageFitness = 0;
        m_bestFitness = 0;
        
        m_disposedGenomesDiversityDistance.Clear();

        for (int i = 0; i < Mathf.Max(mutateGenomesCount, copyGenomesCount); i++)
        {
            if (m_disposedGenomes.Count < 1)
            {
                int additionalNeed = Mathf.Max(mutateGenomesCount, copyGenomesCount) - bestGenomes.Count;
                Debug.LogWarning("EvolutionaryAlgorithm: not enough genomes available to create new generation. difference: " + additionalNeed + ", needed: " + Mathf.Max(mutateGenomesCount, copyGenomesCount) + ", available: " + bestGenomes.Count);
                int addCounter = 0;

                for(int j = 0; j < additionalNeed; j++)
                {
                    bestGenomes.Add(new Genome( bestGenomes[addCounter]));

                    addCounter++;

                    if(addCounter >= bestGenomes.Count)
                    {
                        addCounter = 0;
                    }
                }

                break;
            }

            Genome bestGenome = null;
            float bestFitness = float.MinValue;

            for (int j = 0; j < m_disposedGenomes.Count; j++)
            {
                if (m_useDiversity)
                {
                    if (m_disposedGenomes[j].m_fitness + m_disposedGenomes[j].m_diversity > bestFitness)
                    {
                        bestFitness = m_disposedGenomes[j].m_fitness + m_disposedGenomes[j].m_diversity;
                        bestGenome = m_disposedGenomes[j];
                    }
                }
                else
                {
                    if (m_disposedGenomes[j].m_fitness > bestFitness)
                    {
                        bestFitness = m_disposedGenomes[j].m_fitness;
                        bestGenome = m_disposedGenomes[j];
                    }
                }
            }

            m_bestFitness = Mathf.Max(m_bestFitness, bestFitness);
            m_averageFitness += bestFitness;
            m_disposedGenomes.Remove(bestGenome);
            bestGenomes.Add(bestGenome);
        }

        Debug.Log("EvolutionaryAlgorithm: new generation: bestFitness: " + m_bestFitness);

        m_bestGenomeLastGen = bestGenomes[0];

        for (int i = 0; i < Mathf.Min( m_writeBestToDiskCount, bestGenomes.Count); i++)
        {
            bestGenomes[i].saveToDisk(m_writePath + "\\", "testGenome"+i, true);
        }

        m_disposedGenomes.Clear();

        m_averageFitness /= bestGenomes.Count;

        // create next generation genomes

        // copy best part
        for (int i = 0; i < Mathf.Min( copyGenomesCount, bestGenomes.Count); i++)
        {
            m_preparedGenomes.Add(new Genome(bestGenomes[i]));
        }

        // copy and mutate other part
        for (int i = 0; i < Mathf.Min(mutateGenomesCount, bestGenomes.Count); i++)
        {
            Genome newGenome = new Genome(bestGenomes[i]);
            if(m_mix && i < m_lastGenBest.Count)
            {
                newGenome.mixWith(m_lastGenBest[(int)Mathf.Max(0, Random.value * (m_lastGenBest.Count-1))], m_mutationsCount);
            }
            newGenome.doRandomChangesWeights(m_mutationsCount);
            m_preparedGenomes.Add(newGenome);
        }

        m_lastGenBest.Clear();
        for (int i = 0; i < mutateGenomesCount && i < bestGenomes.Count; i++)
        {
            m_lastGenBest.Add(new Genome(bestGenomes[i]));
        }
    }

}
