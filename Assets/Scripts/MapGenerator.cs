using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {
	public enum DrawMode { NoiseMap, ColourMap, Mesh };
	public DrawMode drawMode;

	public const int mapChunkSize = 241;
	[Range(0, 6)]
	public int levelOfDetail;
	public float noiseScale;

	[Range(1, 8)]
	public int octaves;
	[Range(0, 1)]
	public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;

	public bool autoUpdate;

	public TerrainType[] regions;

	Queue<MapThreadInformation<MapData>> mapDataThreadInformationQueue = new Queue<MapThreadInformation<MapData>>();
	Queue<MapThreadInformation<MeshData>> meshDataThreadInformationQueue = new Queue<MapThreadInformation<MeshData>>();

	public void DrawMapInEditor() {
		MapData mapData = GenerateMapData();
		MapDisplay display = FindObjectOfType<MapDisplay>();

		if(drawMode == DrawMode.NoiseMap) {
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
		} else if(drawMode == DrawMode.ColourMap) {
			display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
		} else if(drawMode == DrawMode.Mesh) {
			display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
		}
	}

	public void RequestMapData(Action<MapData> callback) {
		ThreadStart threadStart = delegate {
			MapDataThread(callback);
		};

		new Thread(threadStart).Start();
	}

	void MapDataThread(Action<MapData> callback) {
		MapData mapData = GenerateMapData();
		lock(mapDataThreadInformationQueue) {
			mapDataThreadInformationQueue.Enqueue(new MapThreadInformation<MapData>(callback, mapData));
		}
	}

	public void RequestMeshData(MapData mapData, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			MeshDataThread(mapData, callback);
		};

		new Thread(threadStart).Start();
	}

	void MeshDataThread(MapData mapData, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
		lock(meshDataThreadInformationQueue) {
			meshDataThreadInformationQueue.Enqueue(new MapThreadInformation<MeshData>(callback, meshData));
		}
	}

	void Update() {
		if(mapDataThreadInformationQueue.Count > 0) {
			for(int i = 0; i < mapDataThreadInformationQueue.Count; i++) {
				MapThreadInformation<MapData> threadInformation = mapDataThreadInformationQueue.Dequeue();
				threadInformation.callback(threadInformation.parameter);
			}
		}

		if(meshDataThreadInformationQueue.Count > 0) {
			for(int i = 0; i < meshDataThreadInformationQueue.Count; i++) {
				MapThreadInformation<MeshData> threadInformation = meshDataThreadInformationQueue.Dequeue();
				threadInformation.callback(threadInformation.parameter);
			}
		}
	}

	MapData GenerateMapData() {
		float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

		Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

		for(int y = 0; y < mapChunkSize; y++) {
			for(int x = 0; x < mapChunkSize; x++) {
				float currentHeight = noiseMap[x, y];
				for(int i = 0; i < regions.Length; i++) {
					if(currentHeight <= regions[i].height) {
						colourMap[y * mapChunkSize + x] = regions[i].colour;
						break;
					}
				}
			}
		}
		return new MapData(noiseMap, colourMap);
	}

	private void OnValidate() {
		if(lacunarity < 1) {
			lacunarity = 1;
		}
		if(noiseScale <= 0.00085f) {
			noiseScale = 0.00085f;
		}
	}

	struct MapThreadInformation<T> {
		public readonly Action<T> callback;
		public readonly T parameter;

		public MapThreadInformation(Action<T> callback, T parameter) {
			this.callback = callback;
			this.parameter = parameter;
		}
	}
}

[System.Serializable]
public struct TerrainType {
	public string name;
	public float height;
	public Color colour;
}

public struct MapData {
	public readonly float[,] heightMap;
	public readonly Color[] colourMap;

	public MapData(float[,] heightMap, Color[] colourMap) {
		this.heightMap = heightMap;
		this.colourMap = colourMap;
	}
}