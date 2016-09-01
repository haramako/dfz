using UnityEngine;
using System.Collections.Generic;
using System;

public class GameObjectPool : IDisposable {
	bool disposed = false;
	GameObject prefab;
	Stack<GameObject> pool = new Stack<GameObject>();
	List<GameObject> usingObjects = new List<GameObject>();

	public GameObjectPool(GameObject _prefab){
		prefab = _prefab;
	}

	public GameObject Create(){
		if (disposed) throw new Exception ("Already disposed");
		GameObject obj;
		if( pool.Count > 0){
			obj = pool.Pop();
		}else{
			obj = GameObject.Instantiate(prefab) as GameObject;
		}
		usingObjects.Add (obj);
		obj.transform.SetParent (prefab.transform.parent, false);
		obj.SetActive (true);
		return obj;
	}

	public void Release(GameObject obj){
		if (disposed) throw new Exception ("Already disposed");
		usingObjects.Remove (obj);
		pool.Push (obj);
		obj.transform.SetParent (prefab.transform, false);
	}

	public void ReleaseAll(){
		if (disposed) throw new Exception ("Already disposed");
		foreach (var obj in usingObjects.ToArray()) {
			Release (obj);
		}
	}

	public void Dispose(){
		if (disposed) return;
		disposed = true;
		foreach (var obj in pool) {
			GameObject.Destroy (obj);
		}
	}
}

public class PoolBehavior : MonoBehaviour {
	GameObjectPool pool;
	public bool disableOnAwake = true;

	void Awake(){
		pool = new GameObjectPool (gameObject);
		if (disableOnAwake) {
			gameObject.SetActive (false);
			disableOnAwake = false;
		}
	}

	public GameObject Create(){
		return pool.Create ();
	}

	public void Release(GameObject obj){
		pool.Release (obj);
	}

	public void ReleaseAll(){
		pool.ReleaseAll ();
	}

	public void OnDestroy(){
		if( pool != null ) pool.Dispose();
	}

}

