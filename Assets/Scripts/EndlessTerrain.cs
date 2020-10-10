using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {
	public const float maximumViewDistance = 450;
	public Transform viewer;

	public static Vector2 viewerPosition;
	int chunkSize;
	int chunksVisibleInViewDistance;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	void Start() {
		chunkSize = MapGenerator.mapChunkSize - 1;
		chunksVisibleInViewDistance = Mathf.RoundToInt(maximumViewDistance / chunkSize);
	}

	void Update() {
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
		UpdateVisibleChunks();
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
					terrainChunkDictionary.Add(ViewedChunkCoordinate, new TerrainChunk(ViewedChunkCoordinate, chunkSize, transform));
				}
			}

		}
	}

	public class TerrainChunk {

		GameObject meshObject;
		Vector2 position;
		Bounds bounds;

		public TerrainChunk(Vector2 coordinate, int size, Transform parent) {
			position = coordinate * size;
			bounds = new Bounds(position, Vector2.one * size);
			Vector3 positionV3 = new Vector3(position.x, 0, position.y);

			meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
			meshObject.transform.position = positionV3;
			meshObject.transform.localScale = Vector3.one * size / 10f;
			meshObject.transform.parent = parent;
			SetVisible(false);
		}

		public void UpdateTerrainChunk() {
			float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
			bool visible = viewerDistanceFromNearestEdge <= maximumViewDistance;
			SetVisible(visible);
		}

		public void SetVisible(bool visible) {
			meshObject.SetActive(visible);
		}

		public bool IsVisible() {
			return meshObject.activeSelf;
		}
	}
}
