using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RandomValuesSeed 
{
	/// <summary>
	/// like Mathf.perlinNoise but within range[0,1]. if perlinNoise creates values out of bounds, another random value is picked until the value is within range.
	/// </summary>
	/// <returns>The perlin noise.</returns>
	/// <param name="SeedX">Seed x.</param>
	/// <param name="SeedY">Seed y.</param>
	public static float strictNoise(float SeedX, float SeedY)
	{
		float randomValue;

		do
		{
			randomValue = Mathf.PerlinNoise(SeedX * 1.2f,SeedY * 1.2f);
			SeedX += 1.1f;
			SeedY += 1.1f;
		}
		while(randomValue < 0.0f || randomValue > 1.0f);

		return randomValue;
	}

	public static float perlinNoiseRanged(float minValue, float maxValue, int seedX, int seedY)
	{
		return perlinNoiseRanged( minValue,  maxValue, 1.2f * seedX, 1.2f * seedY);
	}
	public static float perlinNoiseRanged(float minValue, float maxValue, float seedX, float seedY)
	{
		return Mathf.Lerp(minValue,maxValue,Mathf.PerlinNoise(seedX, seedY));
	}

	public static float getRandomValueSeed(float SeedX, float SeedY, float minValue, float MaxValue)
	{
		return strictNoise(SeedX ,SeedY) * (MaxValue-minValue) + minValue;
	}
	/// <summary>
	/// Gets a random value between [0,1]
	/// </summary>
	/// <returns>The random value seed.</returns>
	/// <param name="SeedX">Seed x.</param>
	/// <param name="SeedY">Seed y.</param>
	public static float getRandomValueSeed(float SeedX, float SeedY)
	{
		return getRandomValueSeed(SeedX,SeedY, 0f, 1f);
	}
	/// <summary>
	/// like Mathf.perlinNoise but within range[0,1] and seed = x & y
	/// </summary>
	/// <returns>The perlin noise.</returns>
	/// <param name="SeedX">Seed x.</param>
	/// <param name="SeedY">Seed y.</param>
	public static float getRandomValueSeed(float Seed)
	{
		return getRandomValueSeed(Seed,Seed, 0f, 1f);
	}
	public static int getRandomValueSeed(float SeedX, float SeedY, int minValue, int MaxValue)
	{
		return (int) getRandomValueSeed(SeedX,SeedY,(float)minValue,(float)MaxValue);
	}
	public static int getRandomValueSeed(float SeedX, float SeedY, int MaxValue)
	{
		return (int) getRandomValueSeed(SeedX,SeedY, 0f, MaxValue);
	}
	public static int getRandomValueSeed(float Seed, int MaxValue)
	{
		return (int) getRandomValueSeed(Seed,Seed, 0f, MaxValue);
	}

	public static bool getRandomBool(float SeedX, float SeedY)
	{
		if( Mathf.PerlinNoise(SeedX * 1.2f ,SeedY * 1.2f) > 0.5f)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	public static bool getRandomBool(float Seed)
	{
		return getRandomBool(Seed,Seed);
	}
	public static bool getRandomBoolProbability(float SeedX, float SeedY, float probabilityPercentage)
	{
		if( strictNoise(SeedX ,SeedY) * 100f< probabilityPercentage)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public static bool getRandomBoolProbability(float Seed, float probabilityPercentage)
	{
		return getRandomBoolProbability(Seed,Seed,probabilityPercentage);
	}

    public static Quaternion getRandomRotationYAxis(float seedX, float seedY)
    {
        return Quaternion.Euler(new Vector3(0, getRandomValueSeed(seedX, seedY, 0f, 360f), 0));
    }
    public static Quaternion getRandomRotationYAxis(float seed)
    {
        return Quaternion.Euler(new Vector3(0, getRandomValueSeed(seed, seed, 0f, 360f), 0));
    }

    /// <summary>
    /// bad performance. don't use this !
    /// </summary>
    /// <param name="seed"></param>
    /// <returns></returns>
    public static double linearRandomValue(int seed)
    {
        Debug.LogWarning("bad performance. don't use this !");

        System.Random rand = new System.Random(seed);
        return rand.NextDouble();
    }

    public static void createTestTable(string fullPath, int count)
    {
        Debug.LogWarning("RandomValuesSeed.createTestTable");

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        int above9 = 0;
        int above8 = 0;
        int above7 = 0;
        int above6 = 0;
        int above5 = 0;
        int above4 = 0;
        int above3 = 0;
        int above2 = 0;
        int above1 = 0;
        int above0 = 0;

        float randomV = Random.value * 10000;

        for (int i = 0; i < count; i++)
        {
            float random = strictNoise(i, randomV);

            if (random > 0.9f)
                above9++;
            else if (random > 0.8f)
                above8++;
            else if (random > 0.7f)
                above7++;
            else if (random > 0.6f)
                above6++;
            else if (random > 0.5f)
                above5++;
            else if (random > 0.4f)
                above4++;
            else if (random > 0.3f)
                above3++;
            else if (random > 0.2f)
                above2++;
            else if (random > 0.1f)
                above1++;
            else
                above0++;
        }

        builder.Append("<0.1;");
        builder.Append(above0);
        builder.Append("\n");
        builder.Append(">0.1;");
        builder.Append(above1);
        builder.Append("\n");
        builder.Append(">0.2;");
        builder.Append(above2);
        builder.Append("\n");
        builder.Append(">0.3;");
        builder.Append(above3);
        builder.Append("\n");
        builder.Append(">0.4;");
        builder.Append(above4);
        builder.Append("\n");
        builder.Append(">0.5;");
        builder.Append(above5);
        builder.Append("\n");
        builder.Append(">0.6;");
        builder.Append(above6);
        builder.Append("\n");
        builder.Append(">0.7;");
        builder.Append(above7);
        builder.Append("\n");
        builder.Append(">0.8;");
        builder.Append(above8);
        builder.Append("\n");
        builder.Append(">0.9;");
        builder.Append(above9);

        System.IO.File.WriteAllText(fullPath, builder.ToString());
    }
}
