using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using RSG;

public class InitializeScene : MonoBehaviour
{

	// Use this for initialization
	IEnumerator Start ()
	{
		var www = new WWW ("http://133.242.235.150:7000/tags/tb-dev");
		yield return www;
		if (!www.isDone)
		{
			throw new System.Exception ("cannot download hash");
		}

		var hash = www.text.Trim ();

		var cfsManager = CfsManager.Instance;
		var cfs = new Cfs.Cfs (
			Application.temporaryCachePath,
			new System.Uri("http://cfs.dragon-fang.com/"),
			hash);
		cfs.Filter = (f) =>
		{
			return !f.EndsWith(".ab") || f.Contains("WebPlayer");
		};
		cfsManager.Init (cfs);
		bool finish = false;
		cfsManager.DownloadIndex (() => {finish = true;});
		while (!finish)
		{
			yield return null;
		}
		finish = false;
		cfsManager.Download (cfs.bucket.Files.Keys, () => {finish = true;});
		while (!finish)
		{
			yield return null;
		}

		G.Initialize (cfs);
		G.LoadMaster ();


		var url = UrlSchemeReceiver.GetUrlScheme ();
		var uri = new Uri (new Uri ("dfz://"), url);
		Router.Route route;
		try
		{
			route = Router.Instance.Resolve (uri.AbsolutePath);
		}
		catch(Exception)
		{
			route = Router.Instance.Resolve ("/game");
		}

		Debug.Log ("URL" + url + " " + route.Scene);

		Router.LoadedCallback = new Action<Router.BaseScene>((scene) =>
		{
			scene.OnStartScene (new Router.SceneParam { Url = uri, Query = new Router.QueryParam (uri.Query) });
		});

		SceneManager.LoadScene (route.Scene);
	}

}
