using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldLandmassBuilderOctaveProperties
{
    public WorldLandmassBuilderOctaveProperties(float frequency, float amplitude, float seedX, float seedY, int smoothCount, Keyframe[] curveKeyframes, int scalingCount)
    {
        this.frequency = frequency;
        this.amplitude = amplitude;
        this.seedX = seedX;
        this.seedY = seedY;
        this.smoothCount = smoothCount;
        this.curveKeyframes = curveKeyframes;
        this.scalingCount = scalingCount;
    }

    public Keyframe[] curveKeyframes { get; private set; }
    public float frequency { get; private set; }
    public float amplitude { get; private set; }
    public float seedX { get; private set; }
    public float seedY { get; private set; }
    public int smoothCount { get; private set; }
    /// <summary>
    /// this value indicates how many times the egde-length of the arry will get divided by 2 before the perlin-noise-heightmap will get created. After the heightmap has been created, the array will get upscaled again
    /// </summary>
    public int scalingCount { get; private set; }

}
