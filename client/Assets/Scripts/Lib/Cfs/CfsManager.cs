#if UNITY_5
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Cfs;

public class CfsManager : MonoBehaviour {
	public enum CfsState {
		None = 0,
		DownloadingIndex,
		DownloadedIndex,
		DownloadingContents,
		Ready,
	}

	public static CfsManager Instance;
	public static ILogger Logger = Debug.logger;

	public CfsState State { get; private set; }
	public Cfs.Cfs Cfs { get; private set; }

	public void Awake(){
		if (Instance != null) {
			Destroy (this);
			return;
		}

		Instance = this;
	}

	public void Init(Cfs.Cfs cfs){
		Cfs = cfs;
	}

	public void DownloadIndex(){
		StartCoroutine (downloadIndexCoroutine());
	}

	IEnumerator downloadIndexCoroutine(){
		Logger.Log ("Cfs", "Cfs: start download index");
		if (State != CfsState.None) {
			throw new InvalidOperationException ("Invalid state " + State);
		}
		State = CfsState.DownloadingIndex;

		string hash;
		if (Cfs.BucketPath.Length != 32) {
			var indexWww = new WWW ((Cfs.BaseUri + Cfs.BucketPath).ToString ());
			yield return indexWww;
			if (indexWww.error != null) {
				Logger.LogError ("Cfs", "error" + indexWww.error);
				yield break;
			}
			hash = indexWww.text.Trim();
		} else {
			hash = Cfs.BucketPath;
		}

		var www = new WWW (Cfs.UrlFromHash (hash).ToString());
		yield return www;
		if (www.error != null) {
			Logger.LogError ("Cfs", "error" + www.error);
		}
		Cfs.WriteBucket (hash, www.bytes);
		State = CfsState.DownloadedIndex;
	}

	/// <summary>
	/// ファイルをダウンロードする
	/// </summary>
	/// <param name="files">Files.</param>
	public void Download( IEnumerable<string> files){
		Logger.Log ("Cfs", "start downloading ");

		float start = Time.time;
		int all = 0;
		int cached = 0;
		int downloaded = 0;

		foreach( var file_ in files ){
			var file = file_;
			all++;
			if (Cfs.Exists (file)) {
				cached++;
				continue;
			}
			var url = Cfs.UrlFromFile(file).ToString();
			queue_.Add( new DownloadInfo(){
				Url = url, 
				Callback = (info)=>{
					if( info.www.error != null ){
						Logger.LogError ("Cfs", "error downloading" + file + " from " + url + " ," + info.www.error);
					}else{
						Logger.Log ("Cfs", "finish downloading " + file + " from " + url);
						Cfs.WriteFile (file, info.www.bytes);
					}
			}});
			downloaded++;
		}
		queue_.Add( new DownloadInfo(){ 
			Callback = (info)=>{
				Logger.Log ("Cfs", string.Format("download finished {3:0.0} sec, all={0}, cached={1}, downloaded={2}", all, cached, downloaded, Time.time-start));
			}
		});
	}

	public class DownloadInfo {
		public string Url;
		public Action<DownloadInfo> Callback;
		public WWW www;
	}

	List<DownloadInfo> queue_ = new List<DownloadInfo>();
	IEnumerator Start(){
		for (;;) {
			if (queue_.Count <= 0) {
				yield return null;
				continue;
			}

			var info = queue_ [0];
			queue_.RemoveAt (0);

			// コールバックを呼ぶだけ
			if (info.Url == null) {
				info.Callback (info);
				continue;
			}

			WWW www;
			while (true) {
				int retryCount = 0;
				www = new WWW (info.Url);
				yield return www;
				if (www.error != null) {
					if (retryCount >= 3) {
						break;
					} else {
						yield return new WaitForSeconds (1.0f);
						www.Dispose ();
						continue;
					}
				} else {
					break;
				}
			}

			if (info.Callback != null) {
				info.www = www;
				info.Callback (info);
			}

			www.Dispose ();
		}
	}

}

#endif
