using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator {
	public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre) {
		float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCentre);

		AnimationCurve heightCurve_threadsafe = new AnimationCurve(settings.heightCurve.keys);

		float minimumValue = float.MaxValue;
		float maximumValue = float.MinValue;

		for(int i = 0; i < width; i++) {
			for(int j = 0; j < height; j++) {
				values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j]) * settings.heightMultiplier;

				if(values[i, j] > maximumValue) {
					maximumValue = values[i, j];
				}

				if(values[i, j] < minimumValue) {
					minimumValue = values[i, j];
				}
			}
		}

		return new HeightMap(values, minimumValue, maximumValue);
	}
}

public struct HeightMap {
	public readonly float[,] values;
	public readonly float minimumValue;
	public readonly float maximumValue;

	public HeightMap(float[,] values, float minimumValue, float maximumValue) {
		this.values = values;
		this.minimumValue = minimumValue;
		this.maximumValue = maximumValue;
	}
}