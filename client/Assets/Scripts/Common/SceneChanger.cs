#define DONT_USE_MULTI_SCENE 
// TODO: 3DS版が5.3に追いつくまで、一時的にマルチシーンを使わない

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if !DONT_USE_MULTI_SCENE
using UnityEngine.SceneManagement;
#endif

#pragma warning disable 612, 618 // Unity5.3.2でwarningを抑制

public enum SceneChangerState {
	NONE,
	PROCESSING
}

public class SceneChangerOption {
	public string EffectName = "Effect228_SceneChangeShort";
	// 以下は、SceneChanger内で使用する
	public string PrevSceneName;
	public string NextSceneName;
	public GameObject EffectObject;
	public bool NoEyeCatch;
}

/*
/// <summary>
/// シーン変更の処理を行う。
/// 
/// 
/// 
/// </summary>
public class SceneChanger : MonoSingleton<SceneChanger> {

	public UIPanel Panel;
	public GameObject Camera;

	public SceneChangerState State { get; private set; }

	public IObservable<SceneChangerOption> OnUnloadScene { get { return unloadSceneSubject.AsObservable (); } }
	Subject<SceneChangerOption> unloadSceneSubject = new Subject<SceneChangerOption>();

	public IObservable<SceneChangerOption> OnFinishChangeScene { get { return finishChangeSceneSubject.AsObservable (); } }
	Subject<SceneChangerOption> finishChangeSceneSubject;

	Stack<IObservable<int>> sceneChangeWaitingList = new Stack<IObservable<int>> ();

	public SceneChangerOption Option { get; private set; }

	void Start(){
#if DONT_USE_MULTI_SCENE
		// DO NOTHING
#else
		State = SceneChangerState.NONE;
		SceneManager.LoadScene ("99_Dummy", LoadSceneMode.Additive);
#endif
	}

	public void AddSceneChangeWaiting(IObservable<int> waiting ){
		sceneChangeWaitingList.Push (waiting);
	}

	public IObservable<Unit> ChangeScene(string sceneName, SceneChangerOption sceneChangeOption = null){
		if (State != SceneChangerState.NONE) {
			return Observable.ReturnUnit ();
			// throw new InvalidOperationException ("Invalid schange changer status.");
		}
		finishChangeSceneSubject = new Subject<SceneChangerOption>();
		State = SceneChangerState.PROCESSING;
		if (sceneChangeOption == null) sceneChangeOption = new SceneChangerOption ();
		Option = sceneChangeOption;
		Option.NextSceneName = sceneName;
#if DONT_USE_MULTI_SCENE
		Option.PrevSceneName = Application.loadedLevelName;
#else
		Option.PrevSceneName = SceneManager.GetActiveScene ().name;
#endif
		return Observable.FromCoroutine (changeSceneInCoroutine);
	}

	IEnumerator changeSceneInCoroutine(){
#if	DONT_USE_MULTI_SCENE
		if( ScriptMachine.Instance != null ){
			ScriptMachine.Instance.IsUpdateReady = false;
		}

		// サブパネル（ローディング％表示と、アイキャッチが乗る）を作成する
		GameObject subPanelObj = new GameObject("SceneChangerSubPanel", typeof(UIWidget));
		UIWidget subPanel = subPanelObj.GetComponent<UIWidget>();
		subPanelObj.transform.SetParent(Panel.transform,false);
		subPanel.alpha = 0f;

		Configure.Log ("SceneChangerLog", "Start scene change to "+Option.NextSceneName);

		Configure.Log ("SceneChangerLog", "Start fadeout");
		yield return Fadeout(Option).StartAsCoroutine ();

		SceneLoadingManager loading = null;
		if( !Option.NoEyeCatch ){
			yield return ResourceCache.Create<GameObject>("SceneLoading").Do(obj=>{loading=obj.GetComponent<SceneLoadingManager>();}).StartAsCoroutine();
			loading.gameObject.transform.SetParent(subPanelObj.transform, false);
			TweenAlpha.Begin(subPanelObj, 0.3f, 1.0f);
		}

		unloadSceneSubject.OnNext (Option);

		Configure.Log ("SceneChangerLog", "Unload scene");

		SoundManager.ChangeScene (Option.NextSceneName);

		Application.LoadLevel("99_Dummy");
		#if UNITY_N3DS
		// N3DSでは色々破棄することにします
		System.GC.Collect();
		// 未使用のリソースを回収する
		if (ResourceCache.Instance != null)
		{
			ResourceCache.Instance.DoReleaseAllForce();
		}
		var resOperation = Resources.UnloadUnusedAssets();
		while (!resOperation.isDone)
		{
			yield return null;
		}
		#endif

		Camera.SetActive (true); // シーンチェンジ時用カメラをONにする

		yield return null; 
		ResourceCache.ReleaseAll(0);

		float startEyecatchTime = Time.time;
		if( !Option.NoEyeCatch ){
			yield return ResourceCache.Create<GameObject>("EyeCatchFang").Do( obj=>{
				obj.transform.SetParent(subPanel.transform, false);
			}).StartAsCoroutine();

		}

		Configure.Log ("SceneChangerLog", "Load scene "+Option.NextSceneName);
		
		#if UNITY_N3DS
		Application.LoadLevel(Option.NextSceneName);
		#else
		var operation = Application.LoadLevelAsync (Option.NextSceneName);
		while (!operation.isDone) {
			Configure.Log ("SceneChangerLog", "Scene changing ... " + (int)(operation.progress * 100));
			if( loading != null ) loading.Progress = Mathf.RoundToInt(operation.progress * 50f);
			yield return null;
		}
		#endif

		yield return null; // watingListを登録するために １フレーム待つ

		// シーンの準備が整うまで待つ
		while( sceneChangeWaitingList.Count > 0 ){
			Configure.Log("SceneChangerLog", "Pop wating list");
			var waiting = sceneChangeWaitingList.Pop();
			yield return waiting.Do(i=>{
				Configure.Log("SceneChangerLog", "Waiting scene ready "+i);
				if( loading != null ) loading.Progress = 50 + i / 2;
				})
				.StartAsCoroutine();
		}

		if( !Option.NoEyeCatch ){
			Configure.Log("SceneChangerLog", "Start fadeout eyecatch");
			loading.Progress = 100;
			yield return ScriptMachine.ExecuteThreadWithFunctionName("Hooks.EndSceneChange", this, Option).StartAsCoroutine();
			var wait = Configure.GetFloat("MinEyecatchWait",0);
			if( wait > 0 ){
				while( startEyecatchTime + wait > Time.time ){
					yield return null;
				}
			}
			TweenAlpha.Begin(subPanelObj, 0.3f, 0.0f);
			yield return new WaitForSeconds(0.3f);
			GameObject.Destroy(subPanelObj);
			Configure.Log("SceneChangerLog", "Finish fadeout eyecatch");
		}

		Camera.SetActive (false); // シーンチェンジ時用カメラをOFFにする

		Configure.Log ("SceneChangerLog", "Start fadein");
		yield return Fadein(Option).StartAsCoroutine ();

		finishChangeSceneSubject.OnNext (Option);
		finishChangeSceneSubject.OnCompleted();

		Configure.Log ("SceneChangerLog", "Finish scene schange to "+Option.NextSceneName);

		if( ScriptMachine.Instance != null ){
			ScriptMachine.Instance.IsUpdateReady = true;
		}
#else
		//WindowManager.LockScreen ();

		Configure.Log ("SceneChangerLog", "Start scene change to "+Option.NextSceneName);

		Configure.Log ("SceneChangerLog", "Start fadeout");
		yield return Fadeout(Option).StartAsCoroutine ();

		unloadSceneSubject.OnNext (Option);

		Configure.Log ("SceneChangerLog", "Unload scene");
		SceneManager.UnloadScene (SceneManager.GetActiveScene ().name);

		Camera.SetActive (true); // シーンチェンジ時用カメラをONにする

		Configure.Log ("SceneChangerLog", "Load scene "+Option.NextSceneName);
		var operation = SceneManager.LoadSceneAsync (Option.NextSceneName, LoadSceneMode.Additive);
		while (!operation.isDone) {
			Configure.Log ("SceneChangerLog", "Scene changing ... " + (int)(operation.progress * 100));
			yield return null;
		}

		yield return null; // watingListを登録するために １フレーム待つ

		// シーンの準備が整うまで待つ
		while( sceneChangeWaitingList.Count > 0 ){
			var waiting = sceneChangeWaitingList.Pop();
			yield return waiting.StartAsCoroutine();
		}

		Camera.SetActive (false); // シーンチェンジ時用カメラをOFFにする

		Configure.Log ("SceneChangerLog", "Start fadein");
		yield return Fadein(Option).StartAsCoroutine ();

		finishChangeSceneSubject.OnNext (Option);

		Configure.Log ("SceneChangerLog", "Finish scene schange to "+Option.NextSceneName);

		//WindowManager.UnlockScreen ();
#endif
		State = SceneChangerState.NONE;
	}

	public void Reboot(){
		SoundManager.ChangeScene ("00_Initialize");
		WebApiManager.Clear();
		SoundManager.StopBGM(0);
		Application.LoadLevel("99_Dummy");
		_G.Cfs.CancelDownload ();
		Observable.Return(Unit.Default).DelayFrame (3).Subscribe (_ => {
			Application.LoadLevel ("00_Initialize");
		});
	}

	IObservable<Unit> Fadeout(SceneChangerOption opt){
		return ResourceCache.Create<GameObject>(opt.EffectName)
			.SelectMany( obj=>{
				opt.EffectObject = obj;
				obj.transform.SetParent(Panel.gameObject.transform, false);
				obj.GetComponent<Animation>().Play("Fadeout");
				return obj.GetComponent<AnimationExtension>().Finished();
			})
			.Finally( ()=>{
				//Configure.Log ("Dev", "finish animation 1 (Fadeout)");
			})
			.Take(1);
	}

	IObservable<Unit> Fadein(SceneChangerOption opt){
		return Observable.Return(Unit.Default)
			.SelectMany(_=>{
				opt.EffectObject.GetComponent<Animation>().Play("Fadein");
				return opt.EffectObject.GetComponent<AnimationExtension>().Finished();
			})
			.Finally( ()=>{
				//Configure.Log ("Dev", "finish animation 2 (Fadein)");
				GameObject.DestroyObject(opt.EffectObject);
			})
			.Take(1);
	}


}
*/