/// <summary>
/// Generic Mono singleton.
/// </summary>
using UnityEngine;
using System;

/// <summary>
/// シングルトン
/// Sceneのヒエラルキに最初から生成してあるのが前提のシングルトン。
/// ヒエラルキ内にない場合は、instanceはnullを返す。
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>{

	/// <summary>
	/// インスタンスを返す。
	/// </summary>
	public static T Instance { get; private set;}

	public static bool HasInstance { get { return Instance != null; } }

	protected virtual void Awake() {
		if (Instance == this) {
			DestroyImmediate (gameObject);
			return;
		}
		Instance = (T)this;
		if (transform.parent != null) {
			DontDestroyOnLoad (gameObject);
		}
	}
}
