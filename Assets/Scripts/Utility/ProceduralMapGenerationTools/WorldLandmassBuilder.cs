using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class WorldLandmassBuilder : ProceduralGenThreadingBase
{
    public WorldLandmassBuilder(WorldLandmassBuilderOctaveProperties[] octaves, int size)
    {
        m_octavesProperties = octaves;
        m_size = size;
    }

    private int m_size;
    private WorldLandmassBuilderOctaveProperties[] m_octavesProperties;
    private List<WorldLandmassBuilderOctave> m_octaveBuilders = new List<WorldLandmassBuilderOctave>();

    protected override void mainThreadProcedure()
    {
        for (int i = 0; i < m_octavesProperties.Length; i++)
        {
            WorldLandmassBuilderOctave temp_octaveBuilder = new WorldLandmassBuilderOctave(m_octavesProperties[i], m_size);
            temp_octaveBuilder.start();
            m_octaveBuilders.Add(temp_octaveBuilder);
            //Debug.Log("started octave builder " + i);
        }

        m_result = new float[m_size, m_size];

        while (m_octaveBuilders.Count > 0)
        {
            Thread.Sleep(10);

            for (int i = 0; i < m_octaveBuilders.Count; i++)
            {
                if (m_octaveBuilders[i].isDone)
                {
                    ArrayTools.additionArrayMembers(m_result, m_octaveBuilders[i].m_result);
                    m_octaveBuilders[i].dispose();
                    m_octaveBuilders.Remove(m_octaveBuilders[i]);
                    //Debug.Log("octave done");
                }
            }
        }

        setIsDoneState(true);
    }

    public override void dispose()
    {
        base.dispose();

        if (m_octaveBuilders != null)
        {
            for (int i = 0; i < m_octaveBuilders.Count; i++)
            {
                m_octaveBuilders[i].dispose();
            }

            m_octaveBuilders.Clear();
            m_octaveBuilders = null;
        }
    }
}
