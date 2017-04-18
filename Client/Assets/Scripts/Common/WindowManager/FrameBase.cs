using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using RSG;


/// <summary>
/// GroundMainの下の各画面のベース
///
/// 各状態のライフサイクルとイベント(GameObjectのものを含む)は下記のようになっている
///
///   ↓ はじめて表示される
///   ↓
/// Initialized
///   ↓
/// (MonoBehaviour.Awake)
///   ↓
/// (OnStartFrame)
///   ↓
/// (OnLoadFrameコルーチン)
///   ↓
/// (OnActivateFrame)
///   ↓
/// (MonoBehaviour.OnEnable)
///   ↓
/// (MonoBehaviour.Start)
///   ↓
///   ↓ ←--(OnSaveState)--(OnChangeState)--+
///   ↓ ↑                                  ↓ PushState()で移動/バックする
///   ↓ ↑+---------------------------------+
///   ↓ ↑↓
/// Active ←--------------+
///   ↓ 別の画面に移動する  ↑
///   ↓                   ↑
/// (OnSaveState)         ↑
///   ↓                 (OnChangeState)
/// (OnDeactivateFrame)   ↑
///   ↓                 (OnActivateFrame)
///   ↓                   ↑
/// Deactive -------------+  次の画面からバックキーで戻ってくる
///   ↓
///   ↓ バックキーで前の画面にもどる/別の画面に移動する
///   ↓
/// (OnFinishFrame)
///   ↓
/// (OnDestory)
///   ↓
/// Destroyed
///
/// </summary>
abstract public class FrameBase : MonoBehaviour
{

	/// <summary>
	/// フレームが作られた時に呼ばれるコールバック
	/// </summary>
	/// <param name="opt"></param>
	public virtual void OnStartFrame(FrameOption opt)
	{
	}

	/// <summary>
	/// フレームが作られた時に呼ばれるコールバック
	/// </summary>
	/// <param name="opt"></param>
	public virtual IEnumerator OnLoadFrame(FrameOption opt)
	{
		yield return null;
	}

	/// <summary>
	/// フレームがアクティブになった時に呼ばれるコールバック
	/// </summary>
	public virtual void OnActivateFrame()
	{
	}

	/// <summary>
	/// フレームが非アクティブになった時に呼ばれるコールバック
	/// </summary>
	public virtual void OnDeactivateFrame()
	{
	}

	/// <summary>
	/// フレームが削除される時に呼ばれるコールバック
	/// </summary>
	public virtual void OnFinishFrame()
	{
	}

	/// <summary>
	/// フレームの状態変更の時に呼ばれるコールバック
	/// </summary>
	public virtual void OnChangeState(FrameOption opt)
	{
	}

	/// <summary>
	/// フレームの状態変更の時に呼ばれるコールバック
	/// </summary>
	public virtual void OnSaveState(FrameOption opt)
	{
	}


	/// <summary>
	/// フェードが開けた後に呼ばれるコールバック
	/// </summary>
	public virtual IEnumerator AfterActivate(FrameOption opt)
	{
		yield return null;
	}

}

/// <summary>
/// GroundMain.SetFrame() や FrameBase.OnStartFrame()などで指定されるオプション
///
/// フレーム遷移時の各種パラメータを指定する
/// </summary>
public class FrameOption
{
	/// <summary>
	/// オリジナルのURI
	/// </summary>
	public Uri Uri;

	/// <summary>
	/// URIのクエリの分解済みのもの（GroundMainが設定する)
	/// </summary>
	public Dictionary<string, string> UriParam;

	/// <summary>
	/// その他、複雑な処理用のメモリ
	/// </summary>
	public object Param;

	/// <summary>
	/// 戻るボタンのヒストリをクリアするかどうか（フッターメニューからの遷移など）
	/// </summary>
	public bool ClearHistory;

	/// <summary>
	/// フェードアウト/インしない
	/// </summary>
	public bool NoFade;

	/// <summary>
	/// フェードアウトのみしない
	/// </summary>
	public bool NoFadeout;

	/// <summary>
	/// 「戻る」ボタンで戻ったかどうか
	/// </summary>
	public bool IsBack;

	/// <summary>
	/// 「戻る」ボタンで戻った場合の帰り値
	/// </summary>
	public object BackResult;

	/// <summary>
	/// PushState()での移動かどうか
	/// </summary>
	public bool IsPushState;

	/// <summary>
	/// ワールドマップ画面かどうか
	/// </summary>
	public bool IsWorldMap;

	/// <summary>
	/// メニューボタンの全体が表示されているかどうか
	/// </summary>
	public bool MenuActivated;

	/// <summary>
	/// フッターのアクティブなタブ（０はまり）
	/// </summary>
	public int FooterTabNum;

	/// <summary>
	/// URIのパラメータを取得する便利関数
	/// </summary>
	/// <param name="key">キー</param>
	/// <param name="defaultValue">デフォルト値</param>
	/// <returns></returns>
	public float GetFloatParam(string key, float defaultValue = 0)
	{
		string val;
		if (UriParam.TryGetValue(key, out val))
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
		if (UriParam.TryGetValue(key, out val))
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
		if (UriParam.TryGetValue(key, out val))
		{
			return val;
		}
		return defaultValue;
	}
}
