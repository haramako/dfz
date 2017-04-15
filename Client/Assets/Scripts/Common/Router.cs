using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using UnityEngine;

public class Router : Singleton<Router>
{

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

}
