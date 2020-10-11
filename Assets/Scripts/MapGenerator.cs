using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {
	public enum DrawMode { NoiseMap, ColourMap, Mesh, FalloffMap };
	public DrawMode drawMode;

	public TerrainData terrainData;
	public NoiseData noiseData;

	[Range(0, 6)]
	public int editorLevelOfDetailPreview;


	public bool autoUpdate;

	public TerrainType[] regions;
	static MapGenerator instance;

	public float[,] falloffMap;

	Queue<MapThreadInformation<MapData>> mapDataThreadInformationQueue = new Queue<MapThreadInformation<MapData>>();
	Queue<MapThreadInformation<MeshData>> meshDataThreadInformationQueue = new Queue<MapThreadInformation<MeshData>>();


	void Awake() {
		falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
	}

	void OnValuesUpdated() {
		if (!Application.isPlaying) {
			DrawMapInEditor();
		}
	}

	public static int mapChunkSize {
		get {
			if(instance == null) {
				instance = FindObjectOfType<MapGenerator>();
			}
			if(instance.terrainData.useFlatShading) {
				return 95;
			} else {
				return 239;
			}
		}
	}
	public void DrawMapInEditor() {
		MapData mapData = GenerateMapData(Vector2.zero);
		MapDisplay display = FindObjectOfType<MapDisplay>();

		if(drawMode == DrawMode.NoiseMap) {
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
		} else if(drawMode == DrawMode.ColourMap) {
			display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
		} else if(drawMode == DrawMode.Mesh) {
			display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorLevelOfDetailPreview, terrainData.useFlatShading), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
		} else if(drawMode == DrawMode.FalloffMap) {
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
		}
	}

	public void RequestMapData(Vector2 centre, Action<MapData> callback) {
		ThreadStart threadStart = delegate {
			MapDataThread(centre, callback);
		};

		new Thread(threadStart).Start();
	}

	void MapDataThread(Vector2 centre, Action<MapData> callback) {
		MapData mapData = GenerateMapData(centre);
		lock(mapDataThreadInformationQueue) {
			mapDataThreadInformationQueue.Enqueue(new MapThreadInformation<MapData>(callback, mapData));
		}
	}

	public void RequestMeshData(MapData mapData, int levelOfDetail, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			MeshDataThread(mapData, levelOfDetail, callback);
		};

		new Thread(threadStart).Start();
	}

	void MeshDataThread(MapData mapData, int levelOfDetail, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, levelOfDetail, terrainData.useFlatShading);
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

	MapData GenerateMapData(Vector2 centre) {
		// +2 för att kompensera för "border" (episod 12)
		float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, centre + noiseData.offset, noiseData.normalizeMode);

		Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

		for(int y = 0; y < mapChunkSize; y++) {
			for(int x = 0; x < mapChunkSize; x++) {
				if(terrainData.useFalloffMap) {
					noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
				}
				float currentHeight = noiseMap[x, y];
				for(int i = 0; i < regions.Length; i++) {
					if(currentHeight >= regions[i].height) {
						colourMap[y * mapChunkSize + x] = regions[i].colour;
					} else {
						break;
					}
				}
			}
		}
		return new MapData(noiseMap, colourMap);
	}

	private void OnValidate() {
		// TODO: Denna kod genererar varningar, se https://forum.unity.com/threads/sendmessage-cannot-be-called-during-awake-checkconsistency-or-onvalidate-can-we-suppress.537265/
		if(terrainData != null) {
			terrainData.OnValuesUpdated -= OnValuesUpdated;
			terrainData.OnValuesUpdated += OnValuesUpdated;
		}
		if(noiseData != null) {
			noiseData.OnValuesUpdated -= OnValuesUpdated;
			noiseData.OnValuesUpdated += OnValuesUpdated;
		}
		falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
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