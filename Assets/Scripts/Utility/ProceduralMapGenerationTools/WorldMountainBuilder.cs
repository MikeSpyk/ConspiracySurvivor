using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class WorldMountainBuilder : ProceduralGenThreadingBase
{
    public WorldMountainBuilder(float seedX, float seedY, int mountainCount, Keyframe[] turbulenceCurveKeys, int turbulenceAmplitude, int mountainSize2Exp, Keyframe[] roughnessStagesKeys)
    {
        m_seedX = seedX;
        m_seedY = seedY;
        //m_sizeExponent = size_TwoExponent;
        //m_sizeEdge = (int)Mathf.Pow(2, size_TwoExponent);
        //m_minMountains = minMountainsCount;
        //m_maxMountains = maxMountainsCount;
        //m_distanceFromEdge = distanceFromEdge;
        m_turbulenceCurveKeys = turbulenceCurveKeys;
        m_turbulenceAmplitude = turbulenceAmplitude;
        m_mountainSizeExp = mountainSize2Exp;
        m_mountaionCount = mountainCount;
        m_roughnessStagesKeys = roughnessStagesKeys;

        /*
        if (minMountainsCount > maxMountainsCount)
		{
			Debug.LogError("WorldMountainBuilder: maxMountains bigger than minMountains");
			m_minMountains = m_maxMountains;
		}
        */
    }

    private float m_maxDiffAdd;
    private int m_mountaionCount;
    private int m_mountainSizeExp;
    private float m_seedX;
    private float m_seedY;
    //private int m_sizeExponent;
    //private int m_sizeEdge;
    private int m_minMountains;
    private int m_maxMountains;
    //private int m_distanceFromEdge;
    private Keyframe[] m_turbulenceCurveKeys;
    private Keyframe[] m_roughnessStagesKeys;
    private int m_turbulenceAmplitude;
    public List<float[,]> m_mountains = new List<float[,]>();

    //private const int mountainSize = 11;

    protected override void mainThreadProcedure()
    {
        //int randomMountainCountRange = m_maxMountains - m_minMountains;
        //int mountainCount = m_minMountains + (int)(RandomValuesSeed.strictNoise(m_seedX, m_seedY) * randomMountainCountRange);
        int mountainCount = m_mountaionCount;

        List<SingleMountainBuilder> mountainCalculators = new List<SingleMountainBuilder>();
        for (int i = 0; i < mountainCount; i++)
        {
            SingleMountainBuilder tempInstance = new SingleMountainBuilder(m_seedX + i * 11, m_seedY + i * 11, m_mountainSizeExp, m_turbulenceCurveKeys, m_turbulenceAmplitude, m_roughnessStagesKeys);
            tempInstance.start();
            mountainCalculators.Add(tempInstance);
        }

        int readyCount = 0;

        while (readyCount != mountainCalculators.Count)
        {
            readyCount = 0;

            for (int i = 0; i < mountainCalculators.Count; i++)
            {
                if (mountainCalculators[i].isDone)
                {
                    readyCount++;
                }
            }

            Thread.Sleep(100);
        }

        for (int i = 0; i < mountainCalculators.Count; i++)
        {
            m_mountains.Add(mountainCalculators[i].result);
            mountainCalculators[i].dispose();
        }

        mountainCalculators.Clear();

        setIsDoneState(true);
    }

    /*
    protected void oldmainThreadProcedure()
	{
		int mountainArraysEgdeSize = (int)Mathf.Pow(2, mountainSize);
		int mountainPosRange = m_sizeEdge - m_distanceFromEdge*2 -mountainArraysEgdeSize ;

		if(mountainPosRange < 0)
		{
			Debug.LogError("WorldMountainBuilder: Size is too small to support mountains");
			setIsDoneState(true);
			return;
		}

		int randomMountainCount = m_maxMountains - m_minMountains;
		int mountainCount = m_minMountains + (int)(RandomValuesSeed.strictNoise(m_seedX, m_seedY) * randomMountainCount);

		List<SingleMountainBuilder> mountainCalculators= new List<SingleMountainBuilder>();
		SingleMountainBuilder tempInstance;
		for(int i = 0; i < mountainCount; i++)
		{
			tempInstance = new SingleMountainBuilder(m_seedX + i*10, m_seedY +i*10,mountainSize, m_turbulenceCurveKeys, m_turbulenceAmplitude);
			tempInstance.start();
			mountainCalculators.Add(tempInstance);
		}

		List<Vector2Int> mountainPositions = new List<Vector2Int>();
		for(int i = 0; i < mountainCount; i++)
		{
			mountainPositions.Add(new Vector2Int(	m_distanceFromEdge + (int)(mountainPosRange * RandomValuesSeed.strictNoise(m_seedX +i, m_seedY +i * 10)), 
				m_distanceFromEdge + (int)(mountainPosRange * RandomValuesSeed.strictNoise(m_seedX +i+mountainCount, m_seedY +(i+mountainCount) * 10))		));
		}

		m_result = new float[m_sizeEdge,m_sizeEdge];

		bool receivedSomething;
		while(mountainCalculators.Count > 0)
		{
			receivedSomething = false;
			for(int i = 0; i < mountainCalculators.Count; i++)
			{
				if(mountainCalculators[i].isDone)
				{
					ArrayTools.addToArrayMax(m_result, mountainCalculators[i].result, mountainPositions[i]);

					mountainCalculators[i].dispose();

					mountainCalculators.RemoveAt(i);
					mountainPositions.RemoveAt(i);

					receivedSomething = true;
				}
			}

			if(!receivedSomething)
			{
				Thread.Sleep(100);
			}
		}

		setIsDoneState(true);
	}
    */

}
