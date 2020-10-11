using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

	public enum NormalizeMode {
		Local,
		Global // minNoiseHeight & maxNoiseHeight kan skilja sig mellan olika "chunks" och vi gör en kvalificerad gissning
	}
	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode) {
		float[,] noiseMap = new float[mapWidth, mapHeight];

		System.Random prng = new System.Random(seed);
		Vector2[] octaveOffsets = new Vector2[octaves];

		float maximumPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		for(int i = 0; i < octaves; i++) {
			float offsetX = prng.Next(-100000, 100000) + offset.x;
			float offsetY = prng.Next(-100000, 100000) - offset.y;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);

			maximumPossibleHeight += amplitude;
			amplitude *= persistance;
		}

		if(scale <= 0) {
			scale = 0.0001f;
		}

		float maximumLocalNoiseHeight = float.MinValue;
		float minimumLocalNoiseHeight = float.MaxValue;

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;

		for(int y = 0; y < mapHeight; y++) {
			for(int x = 0; x < mapWidth; x++) {

				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				for(int i = 0; i < octaves; i++) {
					float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
					float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
					noiseHeight += perlinValue * amplitude;

					amplitude *= persistance;
					frequency *= lacunarity;
				}

				if(noiseHeight > maximumLocalNoiseHeight) {
					maximumLocalNoiseHeight = noiseHeight;
				} else if(noiseHeight < minimumLocalNoiseHeight) {
					minimumLocalNoiseHeight = noiseHeight;
				}

				noiseMap[x, y] = noiseHeight;
			}
		}

		for(int y = 0; y < mapHeight; y++) {
			for(int x = 0; x < mapWidth; x++) {
				if(normalizeMode == NormalizeMode.Local) {
					noiseMap[x, y] = Mathf.InverseLerp(minimumLocalNoiseHeight, maximumLocalNoiseHeight, noiseMap[x, y]);
				} else {
					float normalizedHeight = (noiseMap[x, y] + 1) / maximumPossibleHeight;
					noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
				}
			}
		}
		return noiseMap;
	}
}
