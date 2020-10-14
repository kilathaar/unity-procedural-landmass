using UnityEngine;

public class MapPreview : MonoBehaviour {
	public Renderer textureRenderer;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	public enum DrawMode { NoiseMap, Mesh, FalloffMap };
	public DrawMode drawMode;

	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	public TextureData textureData;

	public Material terrainMaterial;

	[Range(0, MeshSettings.numberOfSupportedLevelOfDetails - 1)]
	public int editorLevelOfDetailPreview;
	public bool autoUpdate;

	public void DrawMapInEditor() {
		textureData.ApplyToMaterial(terrainMaterial);
		textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minimumHeight, heightMapSettings.maximumHeight);
		HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, heightMapSettings, Vector2.zero);

		if(drawMode == DrawMode.NoiseMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
		} else if(drawMode == DrawMode.Mesh) {
			DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorLevelOfDetailPreview));
		} else if(drawMode == DrawMode.FalloffMap) {
			DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numberOfVerticesPerLine), 0, 1)));
		}
	}

	public void DrawTexture(Texture2D texture) {
		textureRenderer.sharedMaterial.mainTexture = texture;
		textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;

		textureRenderer.gameObject.SetActive(true);
		meshFilter.gameObject.SetActive(false);
	}

	public void DrawMesh(MeshData meshData) {
		meshFilter.sharedMesh = meshData.CreateMesh();

		textureRenderer.gameObject.SetActive(false);
		meshFilter.gameObject.SetActive(true);
	}

	private void OnValuesUpdated() {
		if(!Application.isPlaying) {
			DrawMapInEditor();
		}
	}

	private void OnTextureValuesUpdated() {
		textureData.ApplyToMaterial(terrainMaterial);
	}

	private void OnValidate() {
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
}
