using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using RSG;
using Object = UnityEngine.Object;

/**
 * オブジェクトキャッシュ.
 * 
 * アセットバンドル、もしくは、リソースから読み込んでGameObjectを生成する。
 * 
 * 
 * 使い方:
 * // オブジェクトのインスタンス化を行う
 * ResourceCache.Create<GameObject>("ResourceName"), 
 *   .Sibscribe(obj=>{
 *      (...objを使った処理)
 *   });
 *
 * // プリフェッチ（事前にロードのみを行う）
 * ResourceCache.Prefetch<GameObject>("ResourceName");
 * 
 */
public class ResourceCache: MonoSingleton<ResourceCache> {

	static public bool Logging = false; // trueなら詳細のログを表示する
	static public readonly string AssetRoot = "Assets/Gardens/Characters";

	public abstract class Resource : IDisposable {
		public string Name;
		public int RefCount { get; private set; }
		public float LastLoadTime;

		public IPromise<Resource> OnLoaded;
		public abstract Object FindObject (string obj, Type type);
		public abstract void Dispose ();

		// リファレンスカウントを増やす
		public void IncRef(){
			RefCount++;
			Configure.Log ("ResourceCache.Log", "Increment RefCount " + Name + " to " + RefCount);
		}

		// リファレンスカウントを減らす
		public void DecRef(){
			RefCount--;
			Configure.Log ("ResourceCache.Log", "Decrement RefCount " + Name + " to " + RefCount);
			if (RefCount < 0) {
				Debug.LogError ("Invalid RefCount " + Name);
			}
		}
	}

	/// <summary>
	/// リソースのリファレンスカウントを自動で増減させるBehaviour
	/// </summary>
	public class ResourceRefcountBehaviour : MonoBehaviour
	{
		Resource targetResource;

		public void SetTargetResource(Resource target){
			if (targetResource != null) {
				target.DecRef ();
			}
			targetResource = target;
			if (targetResource != null) {
				target.IncRef ();
			}
		}

		void OnDestroy(){
			if (targetResource != null) {
				targetResource.DecRef ();
			}
		}
	}

	public GameObject Container;
	public Dictionary<string,Resource> Bundles = new Dictionary<string,Resource>();

	void Start(){
		if (Container != null) {
			// すでにコンテナがある場合は、キャッシュに登録する
			for (int i = 0; i < Container.transform.childCount; i++) {
				var obj = Container.transform.GetChild (i).gameObject;
				Bundles[obj.name] = new PreloadResource (obj.name, obj);
				Bundles [obj.name].IncRef ();
				Configure.Log ("ResourceCache.Log", "Add preload object " + obj.name);
			}
		}
	}

	float nextCheckTime;
	public void Update(){
		if (nextCheckTime <= Time.time) {
			ReleaseAll ();
			nextCheckTime = Time.time + Configure.GetFloat("ResouceCache.CheckInterval", 120.0f);
		}
	}

	public static string PlatformDir(){
		var prefix = Configure.Get ("AssetBundlePrefix", null);
		if (prefix != null) {
			return prefix;
		} else {
			#if !UNITY_EDITOR && UNITY_ANDROID
			return "Android";
			#elif !UNITY_EDITOR && UNITY_IOS
			return "iOS";
			#elif UNITY_N3DS
			return "N3DS";
			#else
			return "WebPlayer";
			#endif
		}
	}

	public static string ExtOfType(Type type){
		if (type == typeof(Sprite)) {
			return ".png";
		}else if (type == typeof(Texture2D)) {
			return ".png";
		}else{
			return ".prefab";
		}
	}

	public IPromise<Object> LoadOrCreateObject(string name, Type type, bool isCreate, GameObject refCountOwner = null){
		var pair = name.Split (new char[]{ '#', '$' }, 2);
		var filename = pair [0];
		var objname = (pair.Length > 1) ? pair [1] : pair [0];
		return LoadResource (filename)
			.OnLoaded
			.Then( (res)=>{
				var obj = res.FindObject (objname, type);
				if( isCreate && type == typeof(GameObject) ){
					obj = Object.Instantiate (obj);
					var refcount = ((GameObject)obj).AddComponent<ResourceRefcountBehaviour>();
					refcount.SetTargetResource(res);
				}
				if( refCountOwner != null ){
					var refcount = refCountOwner.AddComponent<ResourceRefcountBehaviour>();
					refcount.SetTargetResource(res);
				}
				return obj;
			});
	}

	public static string[] SplitResourceName(string name){
		var pair = name.Split (new char[]{ '#', '$' }, 2);
		pair[1] = (pair.Length > 1) ? pair [1] : pair [0];	
		return pair;
	}

	public Resource LoadResource(string filename){

		Configure.Log ("ResourceCache.Log", "start LoadResource: name='" + filename + "'");

		// キャッシュor読み込み中の中にあるならそれを返す
		Resource res;
		if (Bundles.TryGetValue (filename, out res)) {
			res.LastLoadTime = Time.time;
			Configure.Log ("ResourceCache.Log", "Loadiresource from cache '"+ filename +"'");
			return res;
#if UNITY_EDITOR
		} else if (DirectResource.TryCreate (filename, out res)) {
#endif
		} else if (AssetBundleResource.TryCreate (filename, out res)) {
		} else {
			throw new ArgumentException ("Cannot find resource " + filename);
		}

		Configure.Log ("ResourceCache.Log", "end Loading resource '"+ filename + "' by " + res.GetType());

		Bundles [filename] = res;
		res.LastLoadTime = Time.time;

		return res;
	}

	public void DoReleaseForce(string filename){
		Configure.Log ("ResourceCache.Log", "RleaseForce resource '" + filename);
		Resource res;
		if (Bundles.TryGetValue (filename, out res)) {
			res.Dispose ();
			Bundles.Remove (filename);
		}
	}

	public void DoRelease(string filename){
		Configure.Log ("ResourceCache.Log", "Rlease resource '" + filename);
		Resource res;
		if (Bundles.TryGetValue (filename, out res)) {
			if (res.RefCount <= 0) {
				res.Dispose ();
				Bundles.Remove (filename);
			}
		}
	}

	public int DoReleaseAll(float forceDelay = -1f){
		var count = 0;
		Configure.Log ("ResourceCache.Log", "Rlease all");
		var delay = Configure.GetFloat ("ResourceCache.AutoReleaseDelay", 120.0f);
		if (forceDelay >= 0) {
			delay = forceDelay;
		}
		Bundles = Bundles.Where (kv => {
			if (kv.Value.RefCount <= 0 && Time.time >= kv.Value.LastLoadTime + delay) {
				Configure.Log("ResourceCache.Log", "Release "+kv.Value.Name);
				kv.Value.Dispose ();
				count++;
				return false;
			} else {
				return true;
			}
		}).ToDictionary(kv=>kv.Key, kv=>kv.Value);

		return count;
	}

	public void DoReleaseAllForce(){
		Configure.Log ("ResourceCache.Log", "Rlease all force");
		foreach (var b in Bundles.Values) {
			b.Dispose ();
		}
		Bundles.Clear ();
	}

	public void DoShowStatus(){
		Debug.Log ("ResourceCache.DoShowStatus()");
		foreach (var b in Bundles.Values) {
			Debug.Log ("Name: " + b.Name + " RefCount=" + b.RefCount);
		}
	}

	public static IPromise<Resource> LoadBundle(string name) {
		return Instance.LoadResource (name).OnLoaded;
	}

	public static IPromise<T> LoadAndRefCount<T>(string name, GameObject refCountOwner) where T : Object {
		return Instance.LoadOrCreateObject (name,typeof(T),false,refCountOwner).Then( obj => (T)obj);
	}

	public static IPromise<T> LoadAndRefCountAsComponent<T>(string name, GameObject refCountOwner) where T : Component {
		return Instance.LoadOrCreateObject (name,typeof(GameObject),false,refCountOwner).Then( obj => ((GameObject)obj).GetComponent<T>());
	}

	public static IPromise<T> Load<T>(string name) where T: Object {
		return Instance.LoadOrCreateObject (name,typeof(T),false).Then( obj => (T)obj);
	}

	public static T LoadSync<T>(string name) where T: Object {
		var pair = SplitResourceName (name);
		return (T)Instance.LoadResource (pair[0]).FindObject(pair[1], typeof(T));
	}

	public static IPromise<T> Create<T>(string name) where T: Object {
		return Instance.LoadOrCreateObject (name, typeof(T), true).Then (o => (T)o);
	}

	public static IPromise<T> CreateAsComponent<T>(string name) where T: Component {
		return Create<GameObject> (name).Then (obj => obj.GetComponent<T> ());
	}
		
	public static void ReleaseForce(string filename){
		Instance.DoReleaseForce (filename);
	}

	public static void ReleaseAllForce(){
		Instance.DoReleaseAllForce ();
	}

	/// <summary>
	/// リファレンスカウントが0ならリリースする
	/// </summary>
	/// <param name="filename">リソース名</param>
	public static void Release(string filename){
		Instance.DoRelease (filename);
	}

	/// <summary>
	/// リファレンスカウントがないものをすべて開放する
	/// </summary>
	public static void ReleaseAll(float forceDelay = -1f){
		var released = Instance.DoReleaseAll (forceDelay);
		if (released > 0) {
			Resources.UnloadUnusedAssets ();
		}
	}

	public static void ShowStatus(){
		Instance.DoShowStatus();
	}

	//===============================================
	// アセットローダークラス群
	//===============================================

	/// <summary>
	/// プリロード（最初からヒエラルキに置いてあるオブジェクト）
	/// </summary>
	public class PreloadResource : Resource {
		Object Obj;
		public PreloadResource(string name, Object obj){
			Name = name;
			Obj = obj;
			var promise = new Promise<Resource>();
			promise.Resolve(this);
			OnLoaded = promise;
		}
		public override Object FindObject (string obj, Type type){
			LastLoadTime = Time.time;
			return Obj;
		}
		public override void Dispose(){
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// エディタのみで有効な、プロジェクトからのダイレクト読み込み
	/// 実機ではアセットバンドルとして読み込むはずのもの
	/// </summary>
	public class DirectResource : Resource {
		string Path;
		Dictionary<string,Object> Objects = new Dictionary<string, Object>();

		static public bool TryCreate(string filename, out Resource res){
			if (true || Configure.GetBool ("ResourceCache.LoadDirect")) {
				// リソース名が "hoge#fuga" なら "hoge/fuga" か "hoge/Resources/fuga" が読み込みの対象になる
				var assetPathWithResources = AssetRoot+"/"+filename+"/Resources/";
				if (UnityEditor.AssetDatabase.AssetPathToGUID (assetPathWithResources).Length > 0) {
					res = new DirectResource(filename, assetPathWithResources);
					return true;
				}
				var assetPath = AssetRoot + "/" + filename;
				if (UnityEditor.AssetDatabase.AssetPathToGUID (assetPath).Length > 0) {
					res = new DirectResource (filename, assetPath);
					return true;
				}
			}
			res = null;
			return false;
		}

		public DirectResource(string name, string path){
			Name = name;
			Path = path;
			var promise = new Promise<Resource>();
			promise.Resolve(this);
			OnLoaded = promise;
		}

		public override Object FindObject (string objname, Type type){
			LastLoadTime = Time.time;
			Object obj;
			var key = objname + ":" + type;
			if (!Objects.TryGetValue (key, out obj)) {
				var path = Path + "/" + objname + ExtOfType (type);
				Configure.Log ("ResourceCache.Log", "Load from direct path" + Name + " at " + path);
				var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath(path, type);
				Objects [key] = obj = prefab;
			}
			return obj;
		}

		public override void Dispose(){
		}
	}
#endif

	/// <summary>
	/// アセットバンドルから読み込む
	/// </summary>
	public class AssetBundleResource : Resource {
		AssetBundle bundle;
		Promise<Resource> OnLoadedPromise;

		static public bool TryCreate(string filename, out Resource res){
			if (G.Cfs != null && G.Cfs.ExistsInBucket(PlatformDir() + "/" + filename.ToLowerInvariant() + ".ab") ) {
				res = new AssetBundleResource (filename);
				return true;
			} else {
				res = null;
				return false;
			}
		}

		public AssetBundleResource(string name){
			Name = name;
			OnLoadedPromise = new Promise<Resource>();
			OnLoaded = OnLoadedPromise;
			var promise = new Promise();

			var assetName = PlatformDir() + "/" + name.ToLowerInvariant() + ".ab";
			if( G.Cfs.Exists(assetName) ){
				promise.Resolve();
			}else{
				/*
				ob = G.Cfs.Download(new Cfs.FileInfo[]{ _G.Cfs.bucket.Files[assetName] }).Last()
					.Select(_=>_G.Cfs.LocalPathFromFile(assetName));
				*/
			}

			promise.Then(() => {
				var path = G.Cfs.LocalPathFromFile(assetName);
				Configure.Log ("ResourceCache.Log", "AssetBundleResourcefile://"+path);
				var www = WWW.LoadFromCacheOrDownload("file:///"+path.Replace(@"\","/"),0);
				return PromiseEx.StartWWW(www);
			}).Then( 
				www=>{
					bundle = www.assetBundle;
					OnLoadedPromise.Resolve(this);
				},
				ex => { OnLoadedPromise.Reject(ex); }
			);
		}

		public override Object FindObject (string objname, Type type){
			LastLoadTime = Time.time;
			var obj = bundle.LoadAsset (objname, type);
			if (obj == null) {
				throw new ArgumentException ("asset not " + objname + " found in " + Name);
			} else {
				return obj;
			}
		}
		
		public override void Dispose(){
			if (OnLoadedPromise != null && OnLoadedPromise.CurState != PromiseState.Pending) {
				OnLoadedPromise.Reject (new Exception ("Resource disposed"));
			}
			if (bundle != null) bundle.Unload (true);
		}
	}
}
