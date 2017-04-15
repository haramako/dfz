using UnityEngine;
using System;
using System.Collections;

namespace RSG
{

	/// <summary>
	/// コルーチン実行のためのシングルトン
	/// </summary>
	class Worker : MonoBehaviour
	{
		static Worker instance_;

		public static Worker Instance
		{
			get
			{
				if (instance_ == null)
				{
					var obj = new GameObject ("<<PromiseWorker>>");
					GameObject.DontDestroyOnLoad (obj);
					instance_ = obj.AddComponent<Worker> ();
				}
				return instance_;
			}
		}
	}

	public static class PromiseEx
	{
		public static IPromise Delay(float sec)
		{
			var promise = new Promise();
			Worker.Instance.StartCoroutine (delayCoroutine(promise, sec));
			return promise;
		}

		static IEnumerator delayCoroutine(Promise promise, float sec)
		{
			yield return new WaitForSeconds (sec);
			promise.Resolve ();
		}

		public static IPromise<WWW> StartWWW(string url)
		{
			var promise = new Promise<WWW> ();
			Worker.Instance.StartCoroutine (WWWToPromiseCoroutine(promise, new WWW(url)));
			return promise;
		}

		public static IPromise<WWW> StartWWW(WWW www)
		{
			var promise = new Promise<WWW> ();
			Worker.Instance.StartCoroutine (WWWToPromiseCoroutine(promise, www));
			return promise;
		}

		static IEnumerator WWWToPromiseCoroutine(Promise<WWW> promise, WWW www)
		{
			yield return www;
			if (www.error != null)
			{
				promise.Reject (new Exception (www.error));
			}
			else
			{
				promise.Resolve (www);
			}
		}

		/// <summary>
		/// 対象のYieldInstruction(コルーチン)をPromise<object>に変換する.
		/// 
		/// 使用例:
		///    var wait = new WaitForSeconds(3.0f);
		///    wait.AsPromise().Done(_=>{ Debug.Log("finish!"); });
		/// </summary>
		public static IPromise<WWW> AsPromise(this WWW www)
		{
			var promise = new Promise<WWW>();
			Worker.Instance.StartCoroutine(asPromiseCoroutineWWW(promise, www));
			return promise;
		}

		static IEnumerator asPromiseCoroutineWWW(Promise<WWW> promise, WWW www)
		{
			while (www.isDone) {
				yield return null;
			}
			promise.Resolve(www);
		}

		/// <summary>
		/// 対象のYieldInstruction(コルーチン)をPromise<object>に変換する.
		/// 
		/// 使用例:
		///    var wait = new WaitForSeconds(3.0f);
		///    wait.AsPromise().Done(_=>{ Debug.Log("finish!"); });
		/// </summary>
		public static Promise<object> AsPromise(this YieldInstruction coro)
		{
			var promise = new Promise<object>();
			Worker.Instance.StartCoroutine(asPromiseCoroutine(promise, coro));
			return promise;
		}

		static IEnumerator asPromiseCoroutine(Promise<object> promise, YieldInstruction coro)
		{
			yield return coro;
			promise.Resolve(null);
		}

		public static IPromise<T> Resolved<T>(T val)
		{
			var promise = new Promise<T>();
			promise.Resolve(val);
			return promise;
		}
	}
}
