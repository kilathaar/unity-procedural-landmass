using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdatableData {
	const int textureSize = 512;
	const TextureFormat textureFormat = TextureFormat.RGB565;

	public Layer[] layers;

	float previousMinimumHeight;
	float previousMaximumHeight;

	public void ApplyToMaterial(Material material) {
		material.SetInt("layerCount", layers.Length);
		material.SetColorArray("baseColours", layers.Select(layer => layer.tint).ToArray());
		material.SetFloatArray("baseStartHeights", layers.Select(layer => layer.startHeight).ToArray());
		material.SetFloatArray("baseBlends", layers.Select(layer => layer.blendStrength).ToArray());
		material.SetFloatArray("baseColourStrength", layers.Select(layer => layer.tintStrength).ToArray());
		material.SetFloatArray("baseTextureScales", layers.Select(layer => layer.textureScale).ToArray());
		Texture2DArray texturesArray = GenerateTextureArray(layers.Select(layer => layer.texture).ToArray());
		material.SetTexture("baseTextures", texturesArray);

		UpdateMeshHeights(material, previousMinimumHeight, previousMaximumHeight);
	}

	public void UpdateMeshHeights(Material material, float minimumHeight, float maximumHeight) {
		previousMinimumHeight = minimumHeight;
		previousMaximumHeight = maximumHeight;

		material.SetFloat("minHeight", minimumHeight);
		material.SetFloat("maxHeight", maximumHeight);
	}

	Texture2DArray GenerateTextureArray(Texture2D[] textures) {
		Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
		for(int i = 0; i < textures.Length; i++) {
			textureArray.SetPixels(textures[i].GetPixels(), i);
		}
		textureArray.Apply();
		return textureArray;
	}

	[System.Serializable]
	public class Layer {
		public Texture2D texture;
		public Color tint;
		[Range(0, 1)]
		public float tintStrength;
		[Range(0, 1)]
		public float startHeight;
		[Range(0, 1)]
		public float blendStrength;
		public float textureScale;
	}
}
