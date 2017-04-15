using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using Object = UnityEngine.Object;
using RSG;

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

    static public bool LogEnabled = false; // trueなら詳細のログを表示する

#if UNITY_ANDROID || UNITY_IOS
    static public bool DontUseAssetBundle = false;
#else
    static public bool DontUseAssetBundle = true; // アセバンを使わずにエディタ内のアセットを利用するオプション（エディタでのみ使用可能、それ以外では無視される）
#endif

    /// <summary>
    /// アセ版を使用しないモードが有効化どうかを確認する
    /// </summary>
    static public bool DontUseAssetBundleActual {
        get
        {
#if UNITY_EDITOR
            return DontUseAssetBundle;
#else
            return false; // エディタ以外では、アセ版を使うしか
#endif
        }
    }

    // アセットの名前とMD5ハッシュの対
    public Dictionary<string, Hash128> assetHashes;

    [System.Diagnostics.Conditional("DEBUG")]
    public static void Log(object obj)
    {
        if (LogEnabled)
        {
            Debug.Log(obj);
        }
    }

    public abstract class Resource : IDisposable {
        public string Name;
        public int RefCount { get; private set; }

		/// <summary>
		/// 常駐か
		/// </summary>
		public bool Resident { get; private set; }

        /// <summary>
        /// 読み込みが完了しているかどうか
        /// </summary>
        public bool IsLoaded { get; protected set; }

        /// <summary>
        /// 最後に使用された時刻(UnityEngine.Time.timeの値)
        /// </summary>
        public float LastLoadTime;

        public IPromise<Resource> OnLoaded;
        public abstract Object FindObject (string obj, Type type);
        public T FindObject<T>(string obj) where T: Object {
			return (T)FindObject(obj, typeof(T));
		}
        public virtual void LoadAllAssets() { }
        public virtual IPromise<int> LoadAllAssetsAsync() { return Promise<int>.Resolved(0); }
        public abstract void Dispose ();

		// 常駐モード
		public void EnableResident(){
			Resident = true;
			RefCount = 1;
		}

        // リファレンスカウントを増やす
        public void IncRef(){
			if (Resident)
				return;

            RefCount++;
            //ResourceCache.Log ("Increment RefCount " + Name + " to " + RefCount);
        }

        // リファレンスカウントを減らす
        public void DecRef(){
			if (Resident)
				return;

            RefCount--;
            //ResourceCache.Log ("Decrement RefCount " + Name + " to " + RefCount);
            if (RefCount < 0) {
                ResourceCache.Log ("Invalid RefCount " + Name);
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

    // float nextCheckTime;
    public void Update(){
#if false // TODO: 現時点では自動開放はしない
        if (nextCheckTime <= Time.time) {
            ReleaseAll ();
            nextCheckTime = Time.time + 120.0f;
        }
#endif
    }

    public Dictionary<string, string[]> Dependencies = new Dictionary<string, string[]>();

    public void Setup()
    {
        LoadDependencies();
    }

    public void LoadDependencies()
    {
        // TODO: 現時点では、仮にプレーンテキストから読んでいる
		#if false
        var lines = File.ReadAllLines(Path.Combine(Path.Combine(Application.temporaryCachePath, GameSystem.RuntimePlatform()), "dependency.txt"));
        foreach( var line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            var fileAndDeps = line.Split('=');
            var Deps = fileAndDeps[1].Split(',');
            Dependencies[fileAndDeps[0]] = Deps;
        }
		#endif
    }

    public static string PlatformDir(){
        //return GameSystem.RuntimePlatform();
		return null; // TODO
    }

    public IPromise<Object> LoadOrCreateObject(string name, Type type, bool isCreate, GameObject refCountOwner = null){
        var pair = SplitResourceName(name);
        return LoadResource(pair[0])
            .OnLoaded
            .Then((res) =>
                {
                    var obj = res.FindObject(pair[1], type);
                    if( obj == null)
                    {
                        Debug.LogError("ResourceCache: cannot find object " + pair[1] + " in " + pair[0]);
                        return null;
                    }
                    if (isCreate && type == typeof(GameObject))
                    {
                        obj = Object.Instantiate(obj);
                        var refcount = ((GameObject)obj).AddComponent<ResourceRefcountBehaviour>();
                        refcount.SetTargetResource(res);
                    }
                    if (refCountOwner != null)
                    {
                        var refcount = refCountOwner.AddComponent<ResourceRefcountBehaviour>();
                        refcount.SetTargetResource(res);
                    }
                    return obj;
                });
    }

    public static string[] SplitResourceName(string name){
        var pair = name.Split ('/');
        var result = new string[2];
        if (pair.Length > 1)
        {
            result[0] = pair[0];
            result[1] = pair[pair.Length-1];
        }
        else
        {
            result[0] = pair[0];
            result[1] = pair[0];
        }
        return result;
    }

    public Resource LoadResource(string filename){

        filename = filename.ToLowerInvariant();

        // キャッシュor読み込み中の中にあるならそれを返す
        Resource res;
        if (Bundles.TryGetValue (filename, out res)) {
            res.LastLoadTime = Time.time;
            return res;
#if UNITY_EDITOR
        } else if ( DontUseAssetBundle && DirectResource.TryCreate (filename, out res)) {
#endif
        }
		else if (AssetBundleResource.TryCreate (filename, out res)) {
        } else {
            throw new ArgumentException ("Cannot find resource " + filename);
        }

        ResourceCache.Log ( "Loading resource '"+ filename + "' by " + res.GetType());

        Bundles [filename] = res;
        res.LastLoadTime = Time.time;

        return res;
    }

    public void DoReleaseForce(string filename){
        ResourceCache.Log ( "RleaseForce resource '" + filename);
        Resource res;
        if (Bundles.TryGetValue (filename, out res)) {
            res.Dispose ();
            Bundles.Remove (filename);
        }
    }

    public void DoRelease(string filename){
        ResourceCache.Log ( "Rlease resource '" + filename);
        Resource res;
        if (Bundles.TryGetValue (filename, out res)) {
            if (res.RefCount <= 0) {
                res.Dispose ();
                Bundles.Remove (filename);
            }
        }
    }

    public void DoReleaseAll(float forceDelay = -1f){
        ResourceCache.Log ( "Rlease all");
        var delay = 120.0f;
        if (forceDelay >= 0) {
            delay = forceDelay;
        }
        Bundles = Bundles.Where (kv => {
            if (kv.Value.RefCount <= 0 && Time.time >= kv.Value.LastLoadTime + delay) {
                ResourceCache.Log( "Release "+kv.Value.Name);
                kv.Value.Dispose ();
                return false;
            } else {
                return true;
            }
        }).ToDictionary(kv=>kv.Key, kv=>kv.Value);
    }

    public void DoReleaseAllForce(){
        ResourceCache.Log ( "Rlease all force");
        foreach (var b in Bundles.Values) {
            b.Dispose ();
        }
        Bundles.Clear ();
    }

    public void DoShowStatus(){
        ResourceCache.Log ("ResourceCache.DoShowStatus()");
        foreach (var b in Bundles.Values) {
            ResourceCache.Log ("Name: " + b.Name + " RefCount=" + b.RefCount);
        }
    }

    public static Resource GetBundle(string name)
    {
        return Instance.LoadResource(name);
    }

    public static bool IsExistBundle(string name)
    {
		return Instance.IsResource(name.ToLower());
    }

    public bool IsResource(string name)
    {
		Resource res;

		if (Bundles.TryGetValue (name, out res)) {
			return res.IsLoaded;
		}

		return false;
    }

	
    public static IPromise<Resource> LoadBundleForBootstrap(string name) {

		
		AssetBundleResource.LoadCheckName = name.ToLowerInvariant();
		AssetBundleResource.LoadCheckNameCounter = 0;

		var r = Instance.LoadResource (name).OnLoaded;
		
		AssetBundleResource.LoadCheckName = "";
		AssetBundleResource.LoadCheckNameCounter = 0;

		return r;
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

    public static IPromise<T> Load<T>(string name ) where T:Object {
        return Instance.LoadOrCreateObject (name,typeof(T),false).Then( obj => (T)obj);
    }

    public static T LoadSync<T>(string assetBundleName, string name) where T : Object
    {
        return (T)Instance.LoadResource(assetBundleName).FindObject(name, typeof(T));
    }

    public static IPromise<T> Create<T>(string name) where T: Object {
        return Instance.LoadOrCreateObject (name, typeof(T), true).Then (o => (T)o);
    }

    public static IPromise<T> CreateAsComponent<T>(string name) where T: Component {
        return Create<GameObject> (name).Then (obj => obj.GetComponent<T> ());
    }

    public static GameObject CreateWithResourceEmulate(string path) {
        GameObject obj = null;
        var path2 = path;
        path2 = path2.Replace("Prefabs/", "");
        path2 = path2.Replace("Graphic/Monster/Model/", "");
        path2 = path2.Replace("Graphic/Original/SkillMotion/", "");
        path2 = path2.Replace("Graphic/Original/Model/", "");
        path2 = path2.Replace("Graphic/Original/CommonMotion/", "");
        path2 = path2.Replace("Graphic/Map/RandomMap/", "");
        if (!(path2.StartsWith("Graphic") || path2.StartsWith("Prefabs")))
        {
            ResourceCache.Create<GameObject>(path2).Then((o) =>
            {
                obj = o;
            });
            return obj;
        }
        else
        {
            return null;
        }
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
        Instance.DoReleaseAll (forceDelay);
		PromiseEx.Delay(0.1f).Done(() =>
        {
            Resources.UnloadUnusedAssets();
        });
    }

    public static void ShowStatus(){
        Instance.DoShowStatus();
    }

    //===============================================
    // アセットローダークラス群
    //===============================================

#if UNITY_EDITOR
    /// <summary>
    /// エディタのみで有効な、プロジェクトからのダイレクト読み込み
    /// 実機ではアセットバンドルとして読み込むはずのもの
    /// </summary>
    public class DirectResource : Resource {
        Dictionary<string,Object> Objects = new Dictionary<string, Object>();

        static public bool TryCreate(string filename, out Resource res){
            res = new DirectResource(filename, null);
            return true;
        }

        public DirectResource(string name, string path){
            Name = name;
            var def = new Promise<Resource>();
            OnLoaded = def;
			PromiseEx.Delay(0.001f).Done(()=> {
				IsLoaded = true;
	            def.Resolve(this);
			});
        }

        public override Object FindObject (string objname, Type type){
            LastLoadTime = Time.time;
            Object obj;
            var key = objname + ":" + type;

            if (Objects.TryGetValue(key, out obj))
            {
                return obj;
            }

            if (objname.Contains("$"))
            {
                // "$" で区切られている場合
                var assetSpritePair = objname.Split('$');
                foreach (var assetPath in UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(Name + ".ab", assetSpritePair[0]))
                {
                    foreach (var prefab in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath))
                    {
                        if (prefab.name == assetSpritePair[1] && prefab.GetType() == type)
                        {
                            Objects[key] = prefab;
                            return prefab;
                        }
                    }
                }
            }
            else
            {
                // "$" で区切られていない場合
                foreach (var assetPath in UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(Name + ".ab", objname))
                {
                    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, type);
                    if (prefab != null)
                    {
                        Objects[key] = prefab;
                        return prefab;
                    }
                }
            }
            return null;
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
        bool precached; // LoadAllAssets() ですべてのアセットを読み込み済みかどうか
        Dictionary<string,Object> cache = new Dictionary<string, Object>();
        List<Resource> dependencies = new List<Resource>();

        static public bool TryCreate(string filename, out Resource res){
            res = new AssetBundleResource (filename);
            return true;
        }

        string GetAssetPath()
        {
            return "file:///" + Application.temporaryCachePath.Replace(@"\", "/");
        }

		public static string LoadCheckName = "";
		public static int    LoadCheckNameCounter = 0;

        public AssetBundleResource(string name)
        {
            Name = name;

            var abname = name.ToLowerInvariant() + ".ab";
            var path = GetAssetPath() + "/" + PlatformDir() + "/" + abname;
            IncRef();

            // 依存したアセットバンドルを読み込む
            IPromise<int> dependenciesLoaded = null;
            string[] deps = null;
            if (ResourceCache.Instance.Dependencies.TryGetValue(name.ToLowerInvariant(), out deps)) {

				if (LoadCheckName != "") {
					if (LoadCheckNameCounter > 0) {
						int nId = -1;

						for (int i = 0; i < deps.Length; i++)
						{
							if (LoadCheckName == deps[i]) {
								nId = i;
								break;
							}
						}

						if (nId != -1) {
							for (int i = nId; i < deps.Length - 1; i++) {
								deps[i] = deps[i + 1];
							}

							Array.Resize(ref deps, deps.Length - 1);
						}
					}
					else {
						LoadCheckNameCounter++;
					}
				}

                foreach( var dep in deps)
                {

                    Log("Dependency load " + dep + " by " + abname);
                }

				if (deps.Length > 0) {
					var dependenciesLoading = deps.Select(dep =>
					{
					
						return ResourceCache.LoadBundle(dep).Then( r=>
						{
							// リファレンスカウントを増加させる
							r.IncRef();
							dependencies.Add(r);
						});
					}).ToArray();

					dependenciesLoaded = Promise<Resource>.All(dependenciesLoading).Then(_ => 0);
				}
				else {
					dependenciesLoaded = PromiseEx.Resolved(0);
				}
            }
            else
            {
                dependenciesLoaded = PromiseEx.Resolved(0);
            }

            OnLoaded = dependenciesLoaded.Then(_ =>
                {
                    var hashFilename = "/" + PlatformDir() + "/" + abname;
                    Hash128 hash;
                    if (ResourceCache.Instance.assetHashes.TryGetValue(hashFilename, out hash))
                    {
                        return WWW.LoadFromCacheOrDownload(path, hash).AsPromise();
                    }
                    else
                    {
                        return new WWW(path).AsPromise();
                    }
                })
                .Then(www =>
                {
                    IsLoaded = true;
                    bundle = www.assetBundle;
                    DecRef();
                    return (Resource)this;
                })
                .Catch(ex =>
                {
                    Debug.LogException(ex);
                });
        }

        public override Object FindObject (string objname, Type type){
            LastLoadTime = Time.time;

            if (objname.Contains("$"))
            {
                // "$"で区切られている場合
                objname = objname.Split('$')[1];
                LoadAllAssets();
            }

            var key = objname + ":" + type;
            Object found;
            if (cache.TryGetValue(key, out found))
            {
                return found;
            }

            if (!precached)
            {
                var asset = bundle.LoadAsset(objname, type);
                if (asset != null)
                {
                    cache[key] = asset;
                }
                return asset;
            }
            else
            {
                return null;
            }
        }

        public override void LoadAllAssets() {
            if (bundle != null && !precached)
            {
                foreach( var a in bundle.LoadAllAssets() ){
                    string name;
                    if (a.GetType() == typeof(Shader))
                    {
                        // シェーダーの場合は、 "/" で区切った最後を名前とする
                        var names = a.name.Split('/');
                        name = names[names.Length - 1];
                    }
                    else
                    {
                        name = a.name;
                    }
                    var key = name + ":" + a.GetType();
                    cache[key] = a;
                }
                precached = true;
            }
        }

        public override IPromise<int> LoadAllAssetsAsync()
        {
            var req = bundle.LoadAllAssetsAsync();
            return req.AsPromise().Then(_ =>
            {
                foreach (var a in req.allAssets)
                {
                    var key = a.name + ":" + a.GetType();
                    cache[key] = a;
                }
                return 0;
            });
        }

        public override void Dispose(){
            foreach( var dep in dependencies)
            {
                dep.DecRef();
            }
            if (bundle != null) bundle.Unload (true);
        }
    }
}
