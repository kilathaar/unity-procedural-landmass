using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {
	public enum DrawMode { NoiseMap, Mesh, FalloffMap };
	public DrawMode drawMode;

	public TerrainData terrainData;
	public NoiseData noiseData;
	public TextureData textureData;

	public Material terrainMaterial;

	[Range(0, 6)]
	public int editorLevelOfDetailPreview;


	public bool autoUpdate;

	public float[,] falloffMap;

	Queue<MapThreadInformation<MapData>> mapDataThreadInformationQueue = new Queue<MapThreadInformation<MapData>>();
	Queue<MapThreadInformation<MeshData>> meshDataThreadInformationQueue = new Queue<MapThreadInformation<MeshData>>();

	void OnValuesUpdated() {
		if(!Application.isPlaying) {
			DrawMapInEditor();
		}
	}

	void OnTextureValuesUpdated() {
		textureData.ApplyToMaterial(terrainMaterial);
	}

	public int mapChunkSize {
		get {
			if(terrainData.useFlatShading) {
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
		} else if(drawMode == DrawMode.Mesh) {
			display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorLevelOfDetailPreview, terrainData.useFlatShading));
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

		if(terrainData.useFalloffMap) {

			if(falloffMap == null) {
				falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
			}

			for(int y = 0; y < mapChunkSize + 2; y++) {
				for(int x = 0; x < mapChunkSize + 2; x++) {
					if(terrainData.useFalloffMap) {
						noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
					}
				}
			}
		}

		textureData.UpdateMeshHeights(terrainMaterial, terrainData.minimumHeight, terrainData.maximumHeight);

		return new MapData(noiseMap);
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
		if (textureData != null) {
			textureData.OnValuesUpdated -= OnTextureValuesUpdated;
			textureData.OnValuesUpdated += OnTextureValuesUpdated;
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

public struct MapData {
	public readonly float[,] heightMap;

	public MapData(float[,] heightMap) {
		this.heightMap = heightMap;
	}
}