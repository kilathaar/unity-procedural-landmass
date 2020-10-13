using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {
	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float squareViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
	const float colliderGenerationDistanceThreshold = 5;

	public int colliderLevelOfDetailIndex;
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
	static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

	void Start() {
		mapGenerator = FindObjectOfType<MapGenerator>();

		maximumViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
		chunkSize = mapGenerator.mapChunkSize - 1;
		chunksVisibleInViewDistance = Mathf.RoundToInt(maximumViewDistance / chunkSize);

		UpdateVisibleChunks();
	}

	void Update() {
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;

		if(viewerPosition != previousViewerPosition) {
			foreach(TerrainChunk chunk in visibleTerrainChunks) {
				chunk.UpdateCollisionMesh();
			}
		}

		if((previousViewerPosition - viewerPosition).sqrMagnitude > squareViewerMoveThresholdForChunkUpdate) {
			previousViewerPosition = viewerPosition;
			UpdateVisibleChunks();
		}
	}

	void UpdateVisibleChunks() {
		HashSet<Vector2> alreadyUpdatedChunkCoordinates = new HashSet<Vector2>();
		for(int i = visibleTerrainChunks.Count - 1; i >= 0; i--) {
			alreadyUpdatedChunkCoordinates.Add(visibleTerrainChunks[i].coordinate);
			visibleTerrainChunks[i].UpdateTerrainChunk();
		}

		int currentChunkCoordinateX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
		int currentChunkCoordinateY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

		for(int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
			for(int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
				Vector2 ViewedChunkCoordinate = new Vector2(currentChunkCoordinateX + xOffset, currentChunkCoordinateY + yOffset);

				if(!alreadyUpdatedChunkCoordinates.Contains(ViewedChunkCoordinate)) {
					if(terrainChunkDictionary.ContainsKey(ViewedChunkCoordinate)) {
						terrainChunkDictionary[ViewedChunkCoordinate].UpdateTerrainChunk();
					} else {
						terrainChunkDictionary.Add(ViewedChunkCoordinate, new TerrainChunk(ViewedChunkCoordinate, chunkSize, detailLevels, colliderLevelOfDetailIndex, transform, mapMaterial));
					}
				}
			}
		}
	}

	public class TerrainChunk {
		public Vector2 coordinate;

		GameObject meshObject;
		Vector2 position;
		Bounds bounds;

		MeshRenderer meshRenderer;
		MeshFilter meshFilter;
		MeshCollider meshCollider;

		LevelOfDetailInformation[] detailLevels;
		LevelOfDetailMesh[] levelOfDetailMeshes;
		int colliderLevelOfDetailIndex;

		MapData mapData;
		bool mapDataReceived;
		int previousLevelOfDetailIndex = -1;
		bool hasSetCollider;

		public TerrainChunk(Vector2 coordinate, int size, LevelOfDetailInformation[] detailLevels, int colliderLevelOfDetailIndex, Transform parent, Material material) {
			this.coordinate = coordinate;
			this.detailLevels = detailLevels;
			this.colliderLevelOfDetailIndex = colliderLevelOfDetailIndex;

			position = coordinate * size;
			bounds = new Bounds(position, Vector2.one * size);
			Vector3 positionV3 = new Vector3(position.x, 0, position.y);

			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshCollider = meshObject.AddComponent<MeshCollider>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshRenderer.material = material;

			meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
			meshObject.transform.parent = parent;
			meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
			SetVisible(false);

			levelOfDetailMeshes = new LevelOfDetailMesh[detailLevels.Length];
			for(int i = 0; i < detailLevels.Length; i++) {
				levelOfDetailMeshes[i] = new LevelOfDetailMesh(detailLevels[i].levelOfDetail);
				levelOfDetailMeshes[i].updateCallback += UpdateTerrainChunk;
				if(i == colliderLevelOfDetailIndex) {
					levelOfDetailMeshes[i].updateCallback += UpdateCollisionMesh;
				}
			}
			mapGenerator.RequestMapData(position, OnMapDataReceived);
		}

		void OnMapDataReceived(MapData mapData) {
			this.mapData = mapData;
			mapDataReceived = true;

			UpdateTerrainChunk();
		}

		public void UpdateTerrainChunk() {
			if(mapDataReceived) {
				float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

				bool wasVisible = IsVisible();
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

				if(wasVisible != visible) {
					if(visible) {
						visibleTerrainChunks.Add(this);
					} else {
						visibleTerrainChunks.Remove(this);
					}
					SetVisible(visible);
				}
			}
		}

		public void UpdateCollisionMesh() {
			if(!hasSetCollider) {
				float squareDistanceFromViewerToEdge = bounds.SqrDistance(viewerPosition);

				if(squareDistanceFromViewerToEdge < detailLevels[colliderLevelOfDetailIndex].squareVisibleDistanceThreshold) {
					if(!levelOfDetailMeshes[colliderLevelOfDetailIndex].hasRequetstedMesh) {
						levelOfDetailMeshes[colliderLevelOfDetailIndex].RequestMesh(mapData);
						hasSetCollider = true;
					}
				}

				if(squareDistanceFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
					if(levelOfDetailMeshes[colliderLevelOfDetailIndex].hasMesh) {
						meshCollider.sharedMesh = levelOfDetailMeshes[colliderLevelOfDetailIndex].mesh;
					}
				}
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
		public event System.Action updateCallback;

		public LevelOfDetailMesh(int levelOfDetail) {
			this.levelOfDetail = levelOfDetail;
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
		[Range(0, MeshGenerator.numberOfSupportedLevelOfDetails - 1)]
		public int levelOfDetail;
		public float visibleDistanceThreshold;

		public float squareVisibleDistanceThreshold {
			get {
				return visibleDistanceThreshold * visibleDistanceThreshold;
			}
		}
	}
}
