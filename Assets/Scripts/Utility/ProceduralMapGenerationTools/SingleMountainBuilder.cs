using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleMountainBuilder : ProceduralGenThreadingBase
{
	public SingleMountainBuilder(float seedX, float seedY, int sizeExp, Keyframe[] displacementCurveKeys, int displacementAmplitude, Keyframe[] roughnessStagesKeys)
	{
		m_seedX = seedX;
		m_seedY = seedY;
		m_sizeExp = sizeExp;
        m_displacementCurveKeys = displacementCurveKeys;
        m_displacementAmplitude = displacementAmplitude;
        m_roughnessStagesKeys = roughnessStagesKeys;
    }

	private float m_seedX;
	private float m_seedY;
	private int m_sizeExp;
    private Keyframe[] m_displacementCurveKeys;
    private Keyframe[] m_roughnessStagesKeys;
    private int m_displacementAmplitude;

    protected override void mainThreadProcedure()
	{
		m_result = getMountain(m_sizeExp, m_seedX,m_seedY, m_displacementCurveKeys, m_displacementAmplitude);
		setIsDoneState(true);
	}

	private float[,] getMountain(int sizeExp, float seedX , float seedY, Keyframe[] displacementCurveKeys, int displacementAmplitude)
	{
		float[,] latestMountains = ProceduralMappingTools.getDiamondSquareMountain( sizeExp, m_roughnessStagesKeys, 0.6f, seedX, seedY, 0.3f, false, true);

		int arrayEdgeLengthHalf = latestMountains.GetLength(0)/2;
		Vector2Int middlePos = new Vector2Int(arrayEdgeLengthHalf,arrayEdgeLengthHalf);
		float distanceToMiddle;

        ArrayTools.raiseAboveZeroArray(latestMountains);
		ArrayTools.normalizeArray(latestMountains);

		latestMountains = ArrayTools.turbulenceDisplacement(latestMountains, displacementCurveKeys, displacementAmplitude);

		ArrayTools.applyGaussianBlur(latestMountains, 1,3);


			AnimationCurve falloffCurve = new AnimationCurve(WorldManager.singleton.mountainExtendFalloff.keys);

			for(int i = 0 ; i < latestMountains.GetLength(0); i++)
			{
				for(int j = 0 ; j < latestMountains.GetLength(0); j++)
				{
					//distanceToMiddle = Mathf.Max( Mathf.Abs( arrayEdgeLengthHalf -i), Mathf.Abs( arrayEdgeLengthHalf -j));
					distanceToMiddle = Mathf.Min( Vector2Int.Distance(new Vector2Int(i,j), middlePos) / arrayEdgeLengthHalf, 1);
					//latestMountains[i,j] *= WorldManager.singleton.mountainExtendFalloff.Evaluate(1f- distanceToMiddle);
					latestMountains[i,j] *= falloffCurve.Evaluate(1f- distanceToMiddle);
				}
			}

		ArrayTools.normalizeArray(latestMountains);

		AnimationCurve heightCurve = new AnimationCurve(WorldManager.singleton.diamondHeightCurve.keys);

		for(int k = 0; k < 2; k++)
		{
			for(int i = 0 ; i < latestMountains.GetLength(0); i++)
			{
				for(int j = 0 ; j < latestMountains.GetLength(1); j++)
				{
					latestMountains[i,j] = heightCurve.Evaluate(latestMountains[i,j]);
					//latestMountains[i,j] = WorldManager.singleton.diamondHeightCurve.Evaluate(latestMountains[i,j]);
				}
			}
		}

		return latestMountains;
	}
}
