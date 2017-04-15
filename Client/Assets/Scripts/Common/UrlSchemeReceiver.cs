using UnityEngine;
using System;
using System.Collections;
using System.Linq;

public class UrlSchemeReceiver
{
	static bool consumed;
	static string savedScheme;

	static UrlSchemeReceiver()
	{
		#if UNITY_EDITOR
		if (PlayerPrefs.GetInt("StartupWindow.Enable") == 1)
		{
			savedScheme = PlayerPrefs.GetString("StartupWindow.Url", null);
			if (savedScheme == "") savedScheme = null; // ""はnull扱い
		}
		#elif UNITY_ANDROID
		savedScheme = PlayerPrefs.GetString("BootScheme", null);
		PlayerPrefs.DeleteKey("BootScheme");
		PlayerPrefs.Save();
		#elif UNITY_IOS
		// TODO: これから
		#else
		// コマンドラインのうち、ddp://で始まるものがあれば、それを使用する
		savedScheme = System.Environment.GetCommandLineArgs().FirstOrDefault(x => x.StartsWith("ddp://"));
		#endif
	}

	public static bool HasUrlScheme()
	{
		return !consumed && !string.IsNullOrEmpty(savedScheme);
	}

	/// <summary>
	/// URLスキーマを取得する。
	/// </summary>
	/// <returns>URLスキーマの文字列。ない場合はnull</returns>
	public static string PeekUrlScheme()
	{
		if( consumed || string.IsNullOrEmpty(savedScheme))
		{
			return null;
		}
		else
		{
			return savedScheme;
		}
	}

	/// <summary>
	/// URLスキーマを取得し、同時にクリアする。
	/// </summary>
	/// <returns>>URLスキーマの文字列。ない場合はnull</returns>
	public static string GetUrlScheme()
	{
		if (consumed)
		{
			return null;
		}
		else
		{
			consumed = true;
			return savedScheme;
		}
	}

	/// <summary>
	/// URLスキーマを指定する
	/// デバッグ時のみ使用する
	/// </summary>
	/// <param name="val">URLスキーマの値</param>
	public static void SetUrlScheme(string val)
	{
		consumed = false;
		savedScheme = val;
	}

}
