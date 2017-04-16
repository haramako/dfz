using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using UnityEngine;

public class Router : Singleton<Router>
{
	public static Action<BaseScene> LoadedCallback;

	public class SceneParam
	{
		public Uri Url;
		public QueryParam Query;
	}

	public abstract class BaseScene : MonoBehaviour
	{
		public abstract void OnStartScene (SceneParam param);

		protected virtual void Awake()
		{
			if (Router.LoadedCallback != null)
			{
				Router.LoadedCallback (this);
			}
		}
	}

	public class Route
	{
		public string Path;
		public string Scene;
		public string Page;
	}

	List<Route> routes_ = new List<Route> ();

	public ReadOnlyCollection<Route> Routes { get { return routes_.AsReadOnly(); } }

	protected override void Initialize ()
	{
		AddRoute ("/game", "GameScene", "");
		AddRoute ("/title", "TitleScene", "");
	}

	public void AddRoute(string path_, string scene_, string page_)
	{
		routes_.Add (new Route () { Path = path_, Scene = scene_, Page = page_ });
	}

	public Route Resolve(string path)
	{
		foreach (var route in routes_)
		{
			if (path.StartsWith(route.Path))
			{
				return route;
			}
		}
		throw new Exception ("path '" + path + "' not match in routes");
	}

	public class QueryParam
	{
		public Dictionary<string, string> dict_ = new Dictionary<string, string>();
		public QueryParam(string query)
		{
			// ""と"?"はなにもしない
			if( query.Length <= 1 )
			{
				return;
			}

			var kvs = query.Substring(1).Split('&');
			foreach( var kv in kvs )
			{
				if( kv == "" )
				{
					continue;
				}
				var e = kv.Split('=');
				if( e.Length == 1 )
				{
					dict_[e[0]] = "";
				}
				else
				{
					dict_[e[0]] = e[1];
				}
			}
		}

		/// <summary>
		/// URIのパラメータを取得する便利関数
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="defaultValue">デフォルト値</param>
		/// <returns></returns>
		public bool GetBool(string key, bool defaultValue = false)
		{
			string val;
			if (dict_.TryGetValue(key, out val))
			{
				return val == "0" && val != "" && val.ToLowerInvariant () != "false";
			}
			return defaultValue;
		}

		/// <summary>
		/// URIのパラメータを取得する便利関数
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="defaultValue">デフォルト値</param>
		/// <returns></returns>
		public float GetFloatParam(string key, float defaultValue = 0)
		{
			string val;
			if (dict_.TryGetValue(key, out val))
			{
				float result;
				if (float.TryParse(val, out result))
				{
					return result;
				}
			}
			return defaultValue;
		}

		/// <summary>
		/// URIのパラメータを取得する便利関数
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="defaultValue">デフォルト値</param>
		/// <returns></returns>
		public int GetIntParam(string key, int defaultValue = 0)
		{
			string val;
			if (dict_.TryGetValue(key, out val))
			{
				int result;
				if (int.TryParse(val, out result))
				{
					return result;
				}
			}
			return defaultValue;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public string GetStringParam(string key, string defaultValue = "")
		{
			string val;
			if (dict_.TryGetValue(key, out val))
			{
				return val;
			}
			return defaultValue;
		}

	}

}
