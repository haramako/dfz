using UnityEngine;
using System;
using System.Collections;

namespace RSG {

	/// <summary>
	/// コルーチン実行のためのシングルトン
	/// </summary>
	class Worker : MonoBehaviour {
		static Worker instance_;

		public static Worker Instance {
			get {
				if (instance_ == null) {
					var obj = new GameObject ("<<PromiseWorker>>");
					GameObject.DontDestroyOnLoad (obj);
					instance_ = obj.AddComponent<Worker> ();
				}
				return instance_;
			}
		}
	}

	public class PromiseEx {
		public static IPromise<WWW> StartWWW(string url){
			var promise = new Promise<WWW> ();
			Worker.Instance.StartCoroutine (WWWToPromiseCoroutine(promise, new WWW(url)));
			return promise;
		}

		public static IPromise<WWW> StartWWW(WWW www){
			var promise = new Promise<WWW> ();
			Worker.Instance.StartCoroutine (WWWToPromiseCoroutine(promise, www));
			return promise;
		}

		static IEnumerator WWWToPromiseCoroutine(Promise<WWW> promise, WWW www){
			yield return www;
			if (www.error != null) {
				promise.Reject (new Exception (www.error));
			} else {
				promise.Resolve (www);
			}
		}
	}
}
