using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdatableData {
	public const int numberOfSupportedLevelOfDetails = 5;
	public const int numberOfSupportedChunkSizes = 9;
	public const int numberOfSupportedFlatshadedChunkSizes = 3;
	public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

	public float meshScale = 2.5f;
	public bool useFlatShading;

	[Range(0, numberOfSupportedChunkSizes - 1)]
	public int chunkSizeIndex;
	[Range(0, numberOfSupportedFlatshadedChunkSizes - 1)]
	public int flatshadedChunkSizeIndex;

	// Number of vertices per line of a mesh rendered at its highest resolution (level of detail == 0). Includes the two extra vertices that are excluded from final mesh, but used for calculating normals.
	public int numberOfVerticesPerLine {
		get {
			return supportedChunkSizes[useFlatShading ? flatshadedChunkSizeIndex : chunkSizeIndex] + 5;
		}
	}

	public float meshWorldSize {
		get {
			return (numberOfVerticesPerLine - 3) * meshScale;
		}
	}
}
