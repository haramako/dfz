using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class StartupWindow : EditorWindow
{

	public class Setting : ScriptableObject
	{
		public bool Enable;
		public string Url = "";
	}

	Setting setting_;

	void OnEnable()
	{
		setting_ = AssetDatabase.LoadAssetAtPath<Setting> ("Assets/StartupWindow.asset");
		if (setting_ == null)
		{
			Debug.Log ("create!");
			setting_ = ScriptableObject.CreateInstance<Setting> ();
			AssetDatabase.CreateAsset (setting_, "Assets/StartupWindow.asset");
		}
	}

	void OnGUI()
	{
		Setting s = setting_;

		GUILayout.BeginVertical ();

		s.Enable = GUILayout.Toggle (s.Enable, "有効");
		s.Url = GUILayout.TextField (s.Url);

		if (s.Enable)
		{
			UrlSchemeReceiver.SetUrlInEditor (s.Url);
		}
		else
		{
			UrlSchemeReceiver.SetUrlInEditor ("");
		}

		GUILayout.EndVertical ();
	}

	[MenuItem("Tools/スタートアップウィンドウ")]
	public static void Open()
	{
		EditorWindow.GetWindow<StartupWindow> ();
	}

}
