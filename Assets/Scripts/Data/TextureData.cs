using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData {
	float previousMinimumHeight;
	float previousMaximumHeight;

	public void ApplyToMaterial(Material material) {
		UpdateMeshHeights(material, previousMinimumHeight, previousMaximumHeight);
	}

	public void UpdateMeshHeights(Material material, float minimumHeight, float maximumHeight) {
		previousMinimumHeight = minimumHeight;
		previousMaximumHeight = maximumHeight;

		material.SetFloat("minHeight", minimumHeight);
		material.SetFloat("maxHeight", maximumHeight);
	}
}
