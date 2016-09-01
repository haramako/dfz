using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System;
using SLua;
using System.Linq;

public static class Util {

	public static T FindByName<T>(this GameObject obj,string name) where T : Component
	{
		return obj
			.EachChildren ()
			.Where (o => (o.name == name))
			.Select (o => o.GetComponent<T>())
			.Where (c => (c != null))
			.FirstOrDefault ();
	}

	public static GameObject FindByName(this GameObject obj,string name)
	{
		return obj
			.EachChildren ()
			.Where (o => (o.name == name))
			.FirstOrDefault ();
	}

	public static IEnumerable<GameObject> EachChildren(this GameObject obj)
	{
		foreach (Transform child in obj.transform) {
			yield return child.gameObject;
			foreach (var x in child.gameObject.EachChildren()) {
				yield return x;
			}
		}
	}

	/// <summary>
	/// 自分の親を名前で検索する
	/// </summary>
	/// <returns>The ancestors.</returns>
	/// <param name="obj">Object.</param>
	public static T FindAncestorByName<T>(this GameObject obj, string name) where T: Component{
		return obj
			.EachAncestors ()
			.Where (o => (o.name == name))
			.Select (o => o.GetComponent<T>())
			.Where (c => (c != null))
			.FirstOrDefault ();
	}

	public static GameObject FindAncestorByName(this GameObject obj, string name) {
		return obj
			.EachAncestors ()
			.Where (o => (o.name == name))
			.FirstOrDefault ();
	}

	/// <summary>
	/// 自分の親をたどる
	/// </summary>
	/// <returns>先祖オブジェクトのリスト（自分、親、親の親、、、の順に列挙される）</returns>
	/// <param name="includeSelf">自分を含むか</param>
	public static IEnumerable<GameObject> EachAncestors(this GameObject obj, bool includeSelf = false)
	{
		Transform cur = (includeSelf ?  obj.transform : obj.transform.parent);
		while (cur != null) {
			yield return cur.gameObject;
			cur = cur.parent;
		}
	}

	/// <summary>
	/// 名前で指定されたIDを取得する
	/// </summary>
	/// <returns>ID</returns>
	public static int GetId(this GameObject obj)
	{
		return int.Parse (obj.GetStringId ());
	}
	public static int GetId(this Component c)
	{
		return c.gameObject.GetId ();
	}

	/// <summary>
	/// 名前で指定されたIDを取得する
	/// </summary>
	/// <returns>ID</returns>
	public static string GetStringId(this GameObject obj)
	{
		return obj.EachAncestors (true).Where (o => o.name.Contains(":")).Select (o => o.name.Split (':') [1]).FirstOrDefault ();
	}

	public static string GetStringId(this Component c)
	{
		return c.gameObject.GetStringId ();
	}
}

