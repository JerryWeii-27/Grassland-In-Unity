using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    FastNoiseLite baseNoise = new FastNoiseLite();
    FastNoiseLite mountainNoise1 = new FastNoiseLite();
    FastNoiseLite mountainNoise2 = new FastNoiseLite();
    FastNoiseLite mountainNoise3 = new FastNoiseLite();

    private int size;

    public bool enableBase = true;
    public float scale = 1.01f;
    public int seed = 1831;
    public int octaves = 0; // 6
    public float persistance = 0,
        lacunarity = 0; // 2.1 and 0.45
    public float ridgeLevel = 1;

    public bool enableMountain = true;
    private float mountainMapScale;
    public float mountainMapRatio,
        mountainMapInfluence;
    public float terrainSharpness = 1;
    public int moutainOctaves = 0;
    public float mountainPersistance = 0,
        moutainLacunarity = 0;
    public Vector2 mountainMapOffset = new Vector2(0, 0);

    public Vector2 worldOffsets = new Vector2(0, 0);

    public float seaLevel; // Currently unused.

    public float biomeScale = 1000;

    public float noiseSum = 0;

    private Queue<MapDataThread> mapQueue = new Queue<MapDataThread>();

    public Dictionary<Vector2, float> HeightDict = new Dictionary<Vector2, float>();

    private void NoiseInit(int octaves, float persistance, float lacunarity)
    {
        // Base Noise
        baseNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        baseNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        baseNoise.SetFractalOctaves(octaves);
        baseNoise.SetFractalGain(persistance);
        baseNoise.SetFractalLacunarity(lacunarity);
        baseNoise.SetFractalWeightedStrength(0);
        baseNoise.SetFrequency(scale);
        baseNoise.SetSeed(seed);

        // Mountain Noise
        mountainNoise1.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        mountainNoise1.SetFractalType(FastNoiseLite.FractalType.FBm);
        mountainNoise1.SetFractalOctaves(moutainOctaves);
        mountainNoise1.SetFractalGain(mountainPersistance);
        mountainNoise1.SetFractalLacunarity(moutainLacunarity);
        mountainNoise1.SetFractalWeightedStrength(0);
        mountainNoise1.SetFrequency(mountainMapScale);
        // mountainNoise1.SetSeed(114514);
        mountainNoise1.SetSeed(seed + 1);

        mountainNoise2.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        mountainNoise2.SetFractalType(FastNoiseLite.FractalType.None);
        mountainNoise2.SetFrequency(mountainMapScale * 2);
        // mountainNoise2.SetSeed(13);
        mountainNoise2.SetSeed(seed + 2);

        mountainNoise3.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        mountainNoise3.SetFractalType(FastNoiseLite.FractalType.None);
        mountainNoise3.SetFrequency(mountainMapScale);
        // mountainNoise3.SetSeed(150);
        mountainNoise3.SetSeed(seed + 3);
    }

    public void Start()
    {
        size = FindObjectOfType<EndlessWorld>().size;
        //worldScale = FindObjectOfType<EndlessWorld>().worldScale;
        //base noise + continentalness

        mountainMapScale = scale / mountainMapRatio;

        NoiseInit(octaves, persistance, lacunarity);
    }

    public void RequestMapData(Action<MapData> callback, float[] offset)
    {
        NoiseMapGeneration(callback, offset);
    }

    private async void NoiseMapGeneration(Action<MapData> callback, float[] offset)
    {
        System.Random prng = new System.Random(seed);
        //Debug.Log(new Vector2(offset[0], offset[1]));

        Dictionary<Vector2, float> borderHeight = new Dictionary<Vector2, float>();

        float[,] noiseMap = new float[size, size];

        var result = await Task.Run(() =>
        {
            Vector2[] octaveOffsets = new Vector2[octaves];

            for (int i = 0; i < octaves; i++)
            {
                float offsetX = offset[0] * (size - 1) + (i + 1) * 50.13f - 11911,
                    offsetY = offset[1] * (size - 1) + i * 100.27f - 11911;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            // Base Noise.
            // Can use compute shader here.
            for (int x = -1; x <= size; x++)
            {
                for (int y = -1; y <= size; y++)
                {
                    float noiseHeight = 0;

                    float sampleX = x + offset[0] * (size - 1) + worldOffsets.x;
                    float sampleY = y + offset[1] * (size - 1) + worldOffsets.y;

                    if (enableBase)
                    {
                        noiseHeight = baseNoise.GetNoise(sampleX, sampleY);
                    }

                    if (enableMountain)
                    {
                        noiseHeight +=
                            NoiseSmoothStep(
                                mountainNoise1.GetNoise(
                                    sampleX + mountainMapOffset.x,
                                    sampleY + mountainMapOffset.y
                                )
                            )
                            * mountainMapInfluence
                            / 2;
                        noiseHeight +=
                            NoiseSmoothStep(
                                mountainNoise2.GetNoise(
                                    sampleX + mountainMapOffset.x,
                                    sampleY + mountainMapOffset.y
                                )
                            )
                            * mountainMapInfluence
                            / 2;
                        // noiseHeight += mountainNoise3.GetNoise(sampleX, sampleY) * mountainMapInfluence;

                        noiseHeight /= 2;
                    }

                    // For mesh normals and other stuff.
                    if (x < 0 || x == size || y < 0 || y == size)
                    {
                        borderHeight.TryAdd(new Vector2(x, y), noiseHeight);
                    }
                    else
                    {
                        noiseMap[x, y] = noiseHeight;
                        // noiseSum += noiseHeight / 100f;
                    }
                }
            }
            print("Done with generating map.");

            //string s = "";
            //for (int i = 0; i < size; i++)
            //{
            //	for (int j = 0; j < size; j++)
            //	{
            //		s += noiseMap[i, j];
            //	}
            //	s += '\n';
            //}
            //Debug.Log("noisemap: " + s);
            //Debug.Log(maxNoise.ToString() + "   " + minNoise.ToString());

            lock (mapQueue)
            {
                mapQueue.Enqueue(
                    new MapDataThread(new MapData(noiseMap, borderHeight, offset), callback)
                );
            }

            return 0;
        });
        return;
    }

    private void Update()
    {
        if (mapQueue.Count > 0)
        {
            MapDataThread now = mapQueue.Dequeue();
            now.CallBack();
        }
    }

    float NoiseSmoothStep(float value)
    {
        return Mathf.SmoothStep(
            -1f,
            1f,
            (value + 1) / 2 * (terrainSharpness * 2 + 1) - terrainSharpness
        );
    }
}

public class MapData
{
    public float[,] heightMap;
    public Dictionary<Vector2, float> borderMap;
    public Vector2Int position;

    public MapData(float[,] noiseMap, Dictionary<Vector2, float> _borderMap, float[] offset)
    {
        position.x = (int)offset[0];
        position.y = (int)offset[1];
        heightMap = noiseMap;
        borderMap = _borderMap;
    }
}

public class MapDataThread
{
    MapData mapData;
    Action<MapData> callback;

    public MapDataThread(MapData _mapData, Action<MapData> _callback)
    {
        mapData = _mapData;
        callback = _callback;
    }

    public void CallBack()
    {
        callback(mapData);
    }
}
