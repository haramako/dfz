using UnityEngine;
using System.Collections;
using RSG;

public class InitializeScene : MonoBehaviour {

	// Use this for initialization
	IEnumerator Start () {
		var cfsManager = CfsManager.Instance;
		var cfs = new Cfs.Cfs (
			Application.temporaryCachePath, 
			new System.Uri("http://27.90.199.90/"), 
			"94ab525f5329098a5207b4d669c0d335");
		cfs.Filter = (f) => {
			return !f.EndsWith(".ab") || f.Contains("WebPlayer");
		};
		cfsManager.Init (cfs);
		cfsManager.DownloadIndex ();
		yield return new WaitForSeconds (3.0f);
		cfsManager.Download (cfs.bucket.Files.Keys);

		G.Initialize (cfs);
		G.LoadMaster ();
		Debug.Log (G.FangTemplateDict.Count);

		ResourceCache.Load<GameObject>("Enemy0001Atlas").Then (x => {
			Debug.Log(x);
		});
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void OnButton1Click(){
		var d = new Promise<string> ();
		d.Then (x => {
			Debug.Log (x);
		});
		d.Resolve ("hoge");
	}
}
