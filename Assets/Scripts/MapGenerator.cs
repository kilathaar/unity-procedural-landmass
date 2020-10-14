using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {
	public enum DrawMode { NoiseMap, Mesh, FalloffMap };
	public DrawMode drawMode;

	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	public TextureData textureData;

	public Material terrainMaterial;


	[Range(0, MeshSettings.numberOfSupportedLevelOfDetails - 1)]
	public int editorLevelOfDetailPreview;


	public bool autoUpdate;

	public float[,] falloffMap;

	Queue<MapThreadInformation<HeightMap>> heightMapThreadInformationQueue = new Queue<MapThreadInformation<HeightMap>>();
	Queue<MapThreadInformation<MeshData>> meshDataThreadInformationQueue = new Queue<MapThreadInformation<MeshData>>();

	private void Start() {
		textureData.ApplyToMaterial(terrainMaterial);
		textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minimumHeight, heightMapSettings.maximumHeight);
	}

	void OnValuesUpdated() {
		if(!Application.isPlaying) {
			DrawMapInEditor();
		}
	}

	void OnTextureValuesUpdated() {
		textureData.ApplyToMaterial(terrainMaterial);
	}

	public void DrawMapInEditor() {
		textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minimumHeight, heightMapSettings.maximumHeight);
		HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, heightMapSettings, Vector2.zero);
		MapDisplay display = FindObjectOfType<MapDisplay>();

		if(drawMode == DrawMode.NoiseMap) {
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap.values));
		} else if(drawMode == DrawMode.Mesh) {
			display.DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorLevelOfDetailPreview));
		} else if(drawMode == DrawMode.FalloffMap) {
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numberOfVerticesPerLine)));
		}
	}

	public void RequestHeightMap(Vector2 centre, Action<HeightMap> callback) {
		ThreadStart threadStart = delegate {
			HeightMapThread(centre, callback);
		};

		new Thread(threadStart).Start();
	}

	void HeightMapThread(Vector2 centre, Action<HeightMap> callback) {
		HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, heightMapSettings, centre);
		lock(heightMapThreadInformationQueue) {
			heightMapThreadInformationQueue.Enqueue(new MapThreadInformation<HeightMap>(callback, heightMap));
		}
	}

	public void RequestMeshData(HeightMap heightMap, int levelOfDetail, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			MeshDataThread(heightMap, levelOfDetail, callback);
		};

		new Thread(threadStart).Start();
	}

	void MeshDataThread(HeightMap heightMap, int levelOfDetail, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, levelOfDetail);
		lock(meshDataThreadInformationQueue) {
			meshDataThreadInformationQueue.Enqueue(new MapThreadInformation<MeshData>(callback, meshData));
		}
	}

	void Update() {
		if(heightMapThreadInformationQueue.Count > 0) {
			for(int i = 0; i < heightMapThreadInformationQueue.Count; i++) {
				MapThreadInformation<HeightMap> threadInformation = heightMapThreadInformationQueue.Dequeue();
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

	private void OnValidate() {
		// TODO: Denna kod genererar varningar, se https://forum.unity.com/threads/sendmessage-cannot-be-called-during-awake-checkconsistency-or-onvalidate-can-we-suppress.537265/
		if(meshSettings != null) {
			meshSettings.OnValuesUpdated -= OnValuesUpdated;
			meshSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if(heightMapSettings != null) {
			heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
			heightMapSettings.OnValuesUpdated += OnValuesUpdated;
		}
		if(textureData != null) {
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

