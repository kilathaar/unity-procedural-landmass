using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData {
	public Color[] baseColours;
	[Range(0, 1)]
	public float[] baseStartHeights;
	[Range(0, 1)]
	public float[] baseBlends;

	float previousMinimumHeight;
	float previousMaximumHeight;

	public void ApplyToMaterial(Material material) {
		material.SetInt("baseColourCount", baseColours.Length);
		material.SetColorArray("baseColours", baseColours);
		material.SetFloatArray("baseStartHeights", baseStartHeights);
		material.SetFloatArray("baseBlends", baseBlends);

		UpdateMeshHeights(material, previousMinimumHeight, previousMaximumHeight);
	}

	public void UpdateMeshHeights(Material material, float minimumHeight, float maximumHeight) {
		previousMinimumHeight = minimumHeight;
		previousMaximumHeight = maximumHeight;

		material.SetFloat("minHeight", minimumHeight);
		material.SetFloat("maxHeight", maximumHeight);
	}
}
