using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {

	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float squareViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

	public LevelOfDetailInformation[] detailLevels;
	public static float maximumViewDistance;

	public Transform viewer;
	public Material mapMaterial;

	public static Vector2 viewerPosition;
	Vector2 previousViewerPosition;
	static MapGenerator mapGenerator;
	int chunkSize;
	int chunksVisibleInViewDistance;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	void Start() {
		mapGenerator = FindObjectOfType<MapGenerator>();

		maximumViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
		chunkSize = MapGenerator.mapChunkSize - 1;
		chunksVisibleInViewDistance = Mathf.RoundToInt(maximumViewDistance / chunkSize);

		UpdateVisibleChunks();
	}

	void Update() {
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
		if((previousViewerPosition - viewerPosition).sqrMagnitude > squareViewerMoveThresholdForChunkUpdate) {
			previousViewerPosition = viewerPosition;
			UpdateVisibleChunks();
		}
	}

	void UpdateVisibleChunks() {
		for(int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++) {
			terrainChunksVisibleLastUpdate[i].SetVisible(false);
		}
		terrainChunksVisibleLastUpdate.Clear();

		int currentChunkCoordinateX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
		int currentChunkCoordinateY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

		for(int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
			for(int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
				Vector2 ViewedChunkCoordinate = new Vector2(currentChunkCoordinateX + xOffset, currentChunkCoordinateY + yOffset);

				if(terrainChunkDictionary.ContainsKey(ViewedChunkCoordinate)) {
					terrainChunkDictionary[ViewedChunkCoordinate].UpdateTerrainChunk();
					if(terrainChunkDictionary[ViewedChunkCoordinate].IsVisible()) {
						terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[ViewedChunkCoordinate]);
					}
				} else {
					terrainChunkDictionary.Add(ViewedChunkCoordinate, new TerrainChunk(ViewedChunkCoordinate, chunkSize, detailLevels, transform, mapMaterial));
				}
			}
		}
	}

	public class TerrainChunk {

		GameObject meshObject;
		Vector2 position;
		Bounds bounds;

		MeshRenderer meshRenderer;
		MeshFilter meshFilter;

		LevelOfDetailInformation[] detailLevels;
		LevelOfDetailMesh[] levelOfDetailMeshes;

		MapData mapData;
		bool mapDataReceived;
		int previousLevelOfDetailIndex = -1;

		public TerrainChunk(Vector2 coordinate, int size, LevelOfDetailInformation[] detailLevels, Transform parent, Material material) {
			this.detailLevels = detailLevels;

			position = coordinate * size;
			bounds = new Bounds(position, Vector2.one * size);
			Vector3 positionV3 = new Vector3(position.x, 0, position.y);

			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshRenderer.material = material;
			meshObject.transform.position = positionV3;
			meshObject.transform.parent = parent;
			SetVisible(false);

			levelOfDetailMeshes = new LevelOfDetailMesh[detailLevels.Length];
			for(int i = 0; i < detailLevels.Length; i++) {
				levelOfDetailMeshes[i] = new LevelOfDetailMesh(detailLevels[i].levelOfDetail, UpdateTerrainChunk);
			}
			mapGenerator.RequestMapData(position, OnMapDataReceived);
		}

		void OnMapDataReceived(MapData mapData) {
			this.mapData = mapData;
			mapDataReceived = true;

			Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
			meshRenderer.material.mainTexture = texture;
			UpdateTerrainChunk();
		}

		public void UpdateTerrainChunk() {
			if(mapDataReceived) {
				float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
				bool visible = viewerDistanceFromNearestEdge <= maximumViewDistance;

				if(visible) {
					int levelOfDetailIndex = 0;

					for(int i = 0; i < detailLevels.Length - 1; i++) {
						if(viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreshold) {
							levelOfDetailIndex = i + 1;
						} else {
							break;
						}
					}
					if(levelOfDetailIndex != previousLevelOfDetailIndex) {
						LevelOfDetailMesh levelOfDetailMesh = levelOfDetailMeshes[levelOfDetailIndex];
						if(levelOfDetailMesh.hasMesh) {
							previousLevelOfDetailIndex = levelOfDetailIndex;
							meshFilter.mesh = levelOfDetailMesh.mesh;
						} else if(!levelOfDetailMesh.hasRequetstedMesh) {
							levelOfDetailMesh.RequestMesh(mapData);
						}
					}
				}
				SetVisible(visible);
			}
		}

		public void SetVisible(bool visible) {
			meshObject.SetActive(visible);
		}

		public bool IsVisible() {
			return meshObject.activeSelf;
		}
	}

	class LevelOfDetailMesh {
		public Mesh mesh;
		public bool hasRequetstedMesh;
		public bool hasMesh;
		int levelOfDetail;
		System.Action updateCallback;

		public LevelOfDetailMesh(int levelOfDetail, System.Action updateCallback) {
			this.levelOfDetail = levelOfDetail;
			this.updateCallback = updateCallback;
		}

		void OnMeshDataReceived(MeshData meshData) {
			mesh = meshData.CreateMesh();
			hasMesh = true;

			updateCallback();
		}

		public void RequestMesh(MapData mapData) {
			hasRequetstedMesh = true;
			mapGenerator.RequestMeshData(mapData, levelOfDetail, OnMeshDataReceived);
		}
	}

	[System.Serializable]
	public struct LevelOfDetailInformation {
		public int levelOfDetail;
		public float visibleDistanceThreshold;
	}
}
