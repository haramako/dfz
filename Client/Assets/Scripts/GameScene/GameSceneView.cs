using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game;
using DG.Tweening;

public class GameSceneView : MonoBehaviour
{

	public GameScene GameScene;
	public PoolBehavior CursorBase;

	public List<GameObject> cursors_ = new List<GameObject>();

	// Use this for initialization
	void Start ()
	{

	}

	public void ShowCursor(IEnumerable<Point> path)
	{
		foreach (var p in path)
		{
			var cursor = CursorBase.Create ();
			var pos = GameScene.PointToVector (p);
			cursor.transform.localPosition = pos;
			cursors_.Add (cursor);
		}
	}

	public void SpendCurosr()
	{
		if (cursors_.Count > 0)
		{
			var cur = cursors_ [0];
			cursors_.RemoveAt (0);

			cur.FindByName<SpriteRenderer> ("New Sprite").material.DOFade (0, 1.0f);
			cur.transform.DOScale (1.7f, 1.1f).OnComplete(() =>
			{
				cur.FindByName<SpriteRenderer> ("New Sprite").material.color = Color.white;
				cur.transform.localScale = Vector3.one;
				CursorBase.Release (cur);
			});
		}
	}

	public void ResetCursor()
	{
		cursors_.Clear ();
		CursorBase.ReleaseAll ();
	}

}

