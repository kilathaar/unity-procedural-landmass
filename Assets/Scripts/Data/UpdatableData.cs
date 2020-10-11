using UnityEngine;

public class UpdatableData : ScriptableObject {
	public event System.Action OnValuesUpdated;
	public bool automaticUpdate;

	protected virtual void OnValidate() {
		if(automaticUpdate) {
			NotifyOfUpdatedValues();
		}
	}

	public void NotifyOfUpdatedValues() {
		if(OnValuesUpdated != null) {
			OnValuesUpdated();
		}
	}
}
