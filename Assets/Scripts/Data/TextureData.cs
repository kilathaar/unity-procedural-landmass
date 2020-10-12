using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData {
	public void ApplyToMaterial(Material material) {
		// TODO
	}

	public void UpdateMeshHeights(Material material, float minimumHeight, float maximumHeight) {
		material.SetFloat("minHeight", minimumHeight);
		material.SetFloat("maxHeight", maximumHeight);
	}
}
