﻿using UnityEngine;

public class UpdatableData : ScriptableObject {
	public event System.Action OnValuesUpdated;
	public bool automaticUpdate;

	protected virtual void OnValidate() {
		if(automaticUpdate) {
			UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
		}
	}

	public void NotifyOfUpdatedValues() {
		UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
		if(OnValuesUpdated != null) {
			OnValuesUpdated();
		}
	}
}
