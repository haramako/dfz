using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game;

public class GameSceneView : MonoBehaviour {

	public GameScene GameScene;
	public PoolBehavior CursorBase;

	public List<GameObject> cursors_ = new List<GameObject>();

	// Use this for initialization
	void Start () {
	
	}

	public void ShowCursor(IEnumerable<Point> path){
		CursorBase.ReleaseAll ();
		foreach (var p in path) {
			var cursor = CursorBase.Create ();
			var pos = GameScene.PointToVector (p);
			cursor.transform.localPosition = pos;
			cursors_.Add (cursor);
		}
	}

	public void SpendCurosr(){
		if (cursors_.Count > 0) {
			CursorBase.Release (cursors_ [0]);
			cursors_.RemoveAt (0);
		}
	}

	public void ResetCursor(){
		cursors_.Clear ();
		CursorBase.ReleaseAll ();
	}
	
}

