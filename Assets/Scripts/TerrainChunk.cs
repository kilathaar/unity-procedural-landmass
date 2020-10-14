using UnityEngine;

public class TerrainChunk {
	const float colliderGenerationDistanceThreshold = 5;

	public event System.Action<TerrainChunk, bool> onVilisbilityChanged;
	public Vector2 coordinate;

	GameObject meshObject;
	Vector2 sampleCentre;
	Bounds bounds;

	MeshRenderer meshRenderer;
	MeshFilter meshFilter;
	MeshCollider meshCollider;

	LevelOfDetailInformation[] detailLevels;
	LevelOfDetailMesh[] levelOfDetailMeshes;
	int colliderLevelOfDetailIndex;

	HeightMap heightMap;
	bool heightMapReceived;
	int previousLevelOfDetailIndex = -1;
	bool hasSetCollider;
	float maximumViewDistance;

	HeightMapSettings heightMapSettings;
	MeshSettings meshSettings;
	Transform viewer;

	public TerrainChunk(Vector2 coordinate, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LevelOfDetailInformation[] detailLevels, int colliderLevelOfDetailIndex, Transform parent, Transform viewer, Material material) {
		this.coordinate = coordinate;
		this.detailLevels = detailLevels;
		this.colliderLevelOfDetailIndex = colliderLevelOfDetailIndex;
		this.heightMapSettings = heightMapSettings;
		this.meshSettings = meshSettings;
		this.viewer = viewer;

		sampleCentre = coordinate * meshSettings.meshWorldSize / meshSettings.meshScale;
		Vector2 position = coordinate * meshSettings.meshWorldSize;
		bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

		meshObject = new GameObject("Terrain Chunk");
		meshRenderer = meshObject.AddComponent<MeshRenderer>();
		meshFilter = meshObject.AddComponent<MeshFilter>();
		meshCollider = meshObject.AddComponent<MeshCollider>();
		meshRenderer.material = material;

		meshObject.transform.position = new Vector3(position.x, 0, position.y);
		meshObject.transform.parent = parent;
		SetVisible(false);

		levelOfDetailMeshes = new LevelOfDetailMesh[detailLevels.Length];
		for(int i = 0; i < detailLevels.Length; i++) {
			levelOfDetailMeshes[i] = new LevelOfDetailMesh(detailLevels[i].levelOfDetail);
			levelOfDetailMeshes[i].updateCallback += UpdateTerrainChunk;
			if(i == colliderLevelOfDetailIndex) {
				levelOfDetailMeshes[i].updateCallback += UpdateCollisionMesh;
			}
		}

		maximumViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
	}

	public void Load() {
		ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, heightMapSettings, sampleCentre), OnHeightMapReceived);
	}

	void OnHeightMapReceived(object heightMapObject) {
		this.heightMap = (HeightMap) heightMapObject;
		heightMapReceived = true;

		UpdateTerrainChunk();
	}

	Vector2 viewerPosition {
		get {
			return new Vector2(viewer.position.x, viewer.position.z);
		}
	}

	public void UpdateTerrainChunk() {
		if(heightMapReceived) {
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
					} else if(!levelOfDetailMesh.hasRequestedMesh) {
						levelOfDetailMesh.RequestMesh(heightMap, meshSettings);
					}
				}
			}

			if(wasVisible != visible) {
				SetVisible(visible);

				if(onVilisbilityChanged != null) {
					onVilisbilityChanged(this, visible);
				}
			}
		}
	}

	public void UpdateCollisionMesh() {
		if(!hasSetCollider) {
			float squareDistanceFromViewerToEdge = bounds.SqrDistance(viewerPosition);

			if(squareDistanceFromViewerToEdge < detailLevels[colliderLevelOfDetailIndex].squareVisibleDistanceThreshold) {
				if(!levelOfDetailMeshes[colliderLevelOfDetailIndex].hasRequestedMesh) {
					levelOfDetailMeshes[colliderLevelOfDetailIndex].RequestMesh(heightMap, meshSettings);
				}
			}

			if(squareDistanceFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
				if(levelOfDetailMeshes[colliderLevelOfDetailIndex].hasMesh) {
					meshCollider.sharedMesh = levelOfDetailMeshes[colliderLevelOfDetailIndex].mesh;
					hasSetCollider = true;
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
	public bool hasRequestedMesh;
	public bool hasMesh;
	int levelOfDetail;
	public event System.Action updateCallback;

	public LevelOfDetailMesh(int levelOfDetail) {
		this.levelOfDetail = levelOfDetail;
	}

	void OnMeshDataReceived(object meshDataObject) {
		mesh = ((MeshData) meshDataObject).CreateMesh();
		hasMesh = true;

		updateCallback();
	}

	public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings) {
		hasRequestedMesh = true;
		ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, levelOfDetail), OnMeshDataReceived);
	}
}
