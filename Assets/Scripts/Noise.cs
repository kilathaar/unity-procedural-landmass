using UnityEngine;

public static class Noise {
	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale) {
		float[,] noiseMap = new float[mapWidth, mapHeight];

		if(scale <= 0) {
			scale = 0.0001f;
		}

		for(int y = 0; y < mapWidth; y++) {
			for(int x = 0; x < mapHeight; x++) {
				float sampleX = x / scale;
				float sampleY = y / scale;

				float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
				noiseMap[x, y] = perlinValue;
			}
		}
		return noiseMap;
	}
}