using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float squareViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

	public int colliderLevelOfDetailIndex;
	public LevelOfDetailInformation[] detailLevels;

	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	public TextureData textureSettings;

	public Transform viewer;
	public Material mapMaterial;

	Vector2 viewerPosition;
	Vector2 previousViewerPosition;
	float meshWorldSize;
	int chunksVisibleInViewDistance;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

	void Start() {
		textureSettings.ApplyToMaterial(mapMaterial);
		textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minimumHeight, heightMapSettings.maximumHeight);

		float maximumViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
		meshWorldSize = meshSettings.meshWorldSize;
		chunksVisibleInViewDistance = Mathf.RoundToInt(maximumViewDistance / meshWorldSize);

		UpdateVisibleChunks();
	}

	void Update() {
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

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

		int currentChunkCoordinateX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
		int currentChunkCoordinateY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

		for(int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
			for(int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
				Vector2 ViewedChunkCoordinate = new Vector2(currentChunkCoordinateX + xOffset, currentChunkCoordinateY + yOffset);

				if(!alreadyUpdatedChunkCoordinates.Contains(ViewedChunkCoordinate)) {
					if(terrainChunkDictionary.ContainsKey(ViewedChunkCoordinate)) {
						terrainChunkDictionary[ViewedChunkCoordinate].UpdateTerrainChunk();
					} else {
						TerrainChunk newChunk = new TerrainChunk(ViewedChunkCoordinate, heightMapSettings, meshSettings, detailLevels, colliderLevelOfDetailIndex, transform, viewer, mapMaterial);
						terrainChunkDictionary.Add(ViewedChunkCoordinate, newChunk);
						newChunk.onVilisbilityChanged += OnTerrainChunkVisibilityChanged;
						newChunk.Load();
					}
				}
			}
		}
	}

	void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible) {
		if(isVisible) {
			visibleTerrainChunks.Add(chunk);
		} else {
			visibleTerrainChunks.Remove(chunk);
		}
	}
}

[System.Serializable]
public struct LevelOfDetailInformation {
	[Range(0, MeshSettings.numberOfSupportedLevelOfDetails - 1)]
	public int levelOfDetail;
	public float visibleDistanceThreshold;

	public float squareVisibleDistanceThreshold {
		get {
			return visibleDistanceThreshold * visibleDistanceThreshold;
		}
	}
}
