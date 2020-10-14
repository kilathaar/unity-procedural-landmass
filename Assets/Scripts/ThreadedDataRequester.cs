using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class ThreadedDataRequester : MonoBehaviour {
	static ThreadedDataRequester instance;
	Queue<ThreadInformation> dataQueue = new Queue<ThreadInformation>();

	private void Awake() {
		instance = FindObjectOfType<ThreadedDataRequester>();
	}

	public static void RequestData(Func<object> generateData, Action<object> callback) {
		ThreadStart threadStart = delegate {
			instance.DataThread(generateData, callback);
		};

		new Thread(threadStart).Start();
	}

	void DataThread(Func<object> generateData, Action<object> callback) {
		object data = generateData();
		lock(dataQueue) {
			dataQueue.Enqueue(new ThreadInformation(callback, data));
		}
	}


	void Update() {
		if(dataQueue.Count > 0) {
			for(int i = 0; i < dataQueue.Count; i++) {
				ThreadInformation threadInformation = dataQueue.Dequeue();
				threadInformation.callback(threadInformation.parameter);
			}
		}
	}

	struct ThreadInformation {
		public readonly Action<object> callback;
		public readonly object parameter;

		public ThreadInformation(Action<object> callback, object parameter) {
			this.callback = callback;
			this.parameter = parameter;
		}
	}
}
