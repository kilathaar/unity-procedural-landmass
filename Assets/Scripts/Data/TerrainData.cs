using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData {
	public float uniformScale = 2.5f;

	public bool useFlatShading;
	public bool useFalloffMap;

	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;

	public float minimumHeight {
		get {
			return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0);
		}
	}
	
	public float maximumHeight {
		get {
			return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1);
		}
	}
}
