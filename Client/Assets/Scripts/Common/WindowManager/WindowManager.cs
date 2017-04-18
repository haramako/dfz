using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using RSG;


/// <summary>
/// 地上の全体管理
/// </summary>
public partial class WindowManager : MonoSingleton<WindowManager>
{

	public interface IWindowManagerListener
	{
		void BeforeChange ();
		IEnumerator Fadeout();
		IEnumerator Fadein();
		IEnumerator BeforeLoadFrame();
		IEnumerator AfterLoadFrame();
		IEnumerator AfterActivate();
	}

	IWindowManagerListener Listener;
	public GameObject WindowRoot;

	List<FrameOption> frameHistory_ = new List<FrameOption>();
	GameObject currentWindow_;

	/// <summary>
	/// 新規画面に移行
	/// </summary>
	/// <param name="path">移行先path</param>
	/// <param name="isBack">戻る為の遷移か</param>
	/// <returns></returns>
	void DoGoTo(FrameOption opt)
	{
		Debug.Log("SetFrame(): " + opt.Uri.OriginalString);
		FrameOption prevFrame = null;
		if (frameHistory_.Count >= 1)
		{
			prevFrame = frameHistory_[frameHistory_.Count - 1];
		}

		StartCoroutine(goToCoroutine(prevFrame, opt, false, null));
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="prev">スタック上の前のフレーム</param>
	/// <param name="post">スタック上のあとのフレーム</param>
	/// <param name="isBack">バックキーでの移動かどうか</param>
	/// <param name="isBack">バックの場合のリザルト</param>
	/// <returns></returns>
	IEnumerator goToCoroutine(FrameOption prev, FrameOption post, bool isBack, object result)
	{
		//Debug.Log("Begin GrondMain.changeFrame()");

		ScreenLocker.Lock();

		FrameOption after; // 移動後のフレームオプション
		FrameOption before; // 移動前のフレームオプション
		string path;
		if (isBack)
		{
			after = prev;
			before = post;
			path = prev.Uri.LocalPath.Substring(1); // "/"を排除
			after.IsBack = true;
			after.BackResult = result;
		}
		else
		{
			after = post;
			before = prev;
			path = post.Uri.LocalPath.Substring(1); // "/"を排除
			after.IsBack = false;
			after.BackResult = null;
		}

		// フェードアウト
		if (!(post.NoFade || post.NoFadeout))
		{
			yield return Listener.Fadeout();
		}

		ScreenLocker.UnlockAll();
		ScreenLocker.Lock();

		yield return null;

		if (!post.IsPushState)
		{
			if (currentWindow_ != null)
			{
				Destroy(currentWindow_);
			}
		}

		// ヒストリの変更
		if (!isBack)
		{
			if (after.ClearHistory)
			{
				frameHistory_.Clear();
			}
			frameHistory_.Add(post);
		}
		else
		{
			frameHistory_.RemoveAt(frameHistory_.Count - 1);
		}

		Listener.BeforeChange ();

		// SaveStateを行う
		if (currentWindow_ != null)
		{
			var afterFrame = currentWindow_.GetComponent<FrameBase>();
			afterFrame.OnSaveState(before);
		}

		ResourceCache.ReleaseAll(0);

		if (!post.IsPushState)
		{
			// PushState() でない場合、新しいフレームを作成
			var path2 = path.Split('/');
			GameObject obj = null;

			yield return ResourceCache
						 .Create<GameObject>("Menu/" + path2.Last())
						 .Then(o =>
			{
				obj = o;
				obj.transform.SetParent(WindowRoot.transform, false);
			}).AsCoroutine();

			currentWindow_ = obj;

			var newFrame = obj.GetComponent<FrameBase>();
			if (newFrame != null)
			{
				newFrame.OnStartFrame(after);

				yield return Listener.BeforeLoadFrame();

				yield return newFrame.OnLoadFrame(after);

				newFrame.OnChangeState(after);

				yield return Listener.AfterLoadFrame();

				newFrame.OnActivateFrame();
			}

			obj.SetActive(true);
		}
		else
		{
			yield return Listener.BeforeLoadFrame();

			// PushState() の場合
			var afterFrame = currentWindow_.GetComponent<FrameBase>();
			afterFrame.OnChangeState(after);

			yield return Listener.AfterLoadFrame();
		}

		// フェードイン
		if (!post.NoFade)
		{
			yield return Listener.Fadeout();
		}

		ScreenLocker.Unlock();

		yield return currentWindow_.GetComponent<FrameBase>().AfterActivate(post);

		yield return Listener.AfterActivate();

		//Debug.Log("Finish GrondMain.changeFrame()");
	}

	public void GoTo(string path)
	{
		GoTo(path);
	}

	/// <summary>
	/// 1個前のフレームに戻る
	/// </summary>
	/// <returns>戻ったか</returns>
	public bool DoBack(object result)
	{
		int count = frameHistory_.Count;
		if (frameHistory_.Count > 1)
		{
			StartCoroutine(goToCoroutine(frameHistory_[count - 2], frameHistory_[count - 1], true, result));
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// URIのQueryStringを分解する
	/// </summary>
	/// <param name="query"></param>
	/// <returns></returns>
	public static Dictionary<string, string> ParseQuery(string query)
	{
		return query
			   .Split('?', '&')
			   .Where(i => string.IsNullOrEmpty(i) == false)
			   .Select(i => i.Split('='))
			   .ToDictionary(
				   i => Uri.UnescapeDataString(i[0]),
				   i => ((i.Length > 1) ? Uri.UnescapeDataString(i[1]) : ""));
	}

	public static void PushState(string path, FrameOption option = null)
	{
		if (option == null)
		{
			option = new FrameOption();
		}
		option.IsPushState = true;
		option.NoFade = true;
		GoTo(path, option);
	}

	public static void GoTo(string path, FrameOption option = null)
	{
		var uri = new Uri(new Uri("ddp:///"), path);
		var query = ParseQuery(uri.Query);
		if (option == null)
		{
			option = new FrameOption();
		}
		option.Uri = uri;
		option.UriParam = query;
		Instance.DoGoTo(option);
		#if UNITY_EDITOR
		// 現在のURLをStartupWindow用に保存する
		PlayerPrefs.SetString("StartupWindow.CurrentUrl", uri.ToString());
		PlayerPrefs.Save();
		#endif
	}

	//
	/// <summary>
	/// 特定のURLまでのヒストリーを削除する
	/// 現在のFrameを除いて、末尾から探し、最初の指定されたURLを含まないヒストリーを削除する。
	/// また、指定されたURLが存在しない場合はすべてのヒストリーを削除する。
	///
	/// A => B => C => D => E
	/// から path に B を指定した場合、ヒストリーは
	/// A => B => E
	/// の状態になる。
	/// その直後に Back() を呼ぶことで、特定のURLまで戻る挙動となる
	///
	/// </summary>
	/// <param name="path"></param>
	public void RemoveHistorySincePath(string path)
	{
		if (path[0] != '/') path = "/" + path; // 先頭の/を足す

		while (true)
		{
			// すべて削除したら終わり
			if (frameHistory_.Count <= 2)
			{
				break;
			}

			var frame = frameHistory_[frameHistory_.Count - 2];
			Debug.Log(frame.Uri.LocalPath);
			if ( frame.Uri.LocalPath == path )
			{
				// 指定されたURLまで戻ったら終わり
				break;
			}
			frameHistory_.RemoveAt(frameHistory_.Count - 2);
		}
	}

	public static void BackToFrame(string path)
	{
		Instance.RemoveHistorySincePath(path);
		Back();
	}

	/// <summary>
	/// ひとつ前の画面に戻る
	/// </summary>
	public static void Back(object result = null)
	{
		Instance.DoBack(result);
	}

}
