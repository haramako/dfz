using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

/// <summary>
/// アセットバンドルを作成するスクリプト
/// </summary>
public class AssetBundleCreator {

	static readonly string[] SrcDirectories = new string[]{ "Assets/Gardens" };
	static readonly string DestDirectory = "../AssetBundles";

	public static string GetDestDirectory(){
		var path = DestDirectory.Replace('/', Path.DirectorySeparatorChar);
		return Path.Combine (Application.dataPath, path);
	}

	/// <summary>
	/// すべてのアセットバンドルを作成する（バッチ用)
	/// </summary>
	public static void CreateAssetBundleBatch(){
		SetAssetBundleName ();
		CreateAssetBundleAll (true);
	}

	/// <summary>
	/// すべてのアセットバンドルを作成する（メニュー用）
	/// </summary>
	[MenuItem("Tools/アセットバンドル/Asset Bundle をまとめて作成する")]
	public static void CreateAssetBundleAllMenu(){
		CreateAssetBundleAll (false);
	}

	/// <summary>
	/// すべてのアセットバンドルを作成する
	/// </summary>
	public static void CreateAssetBundleAll(bool noConfirm = false){
		if (noConfirm || EditorUtility.DisplayDialog ("アセットバンドル作成", "すべてのアセットバンドルを作成します。よろしいですか？", "OK", "Cencel")) {
			var target = EditorUserBuildSettings.activeBuildTarget;
			var oldTarget = target;
			Debug.Log ("ビルドターゲット: " + target);
			if (target.ToString().StartsWith ("Standalone")) target = BuildTarget.StandaloneWindows;
			var outputPath = Path.Combine (GetDestDirectory(), target.ToString());
			Directory.CreateDirectory (outputPath);
			try {
				BuildPipeline.BuildAssetBundles (outputPath, BuildAssetBundleOptions.IgnoreTypeTreeChanges, target);
			}finally{
				if( target != oldTarget ){
					EditorUserBuildSettings.SwitchActiveBuildTarget(oldTarget);
				}
			}
		}
	}

	/// <summary>
	/// すべてのアセットバンドルの名前を自動でつける
	/// </summary>
	[MenuItem("Tools/アセットバンドル/Asset Bundle の名前をつける")]
	public static void SetAssetBundleName(){
		foreach (var src in SrcDirectories) {
			var guids = AssetDatabase.FindAssets ("", new string[]{ src });
			foreach (var guid in guids) {
				var path = AssetDatabase.GUIDToAssetPath (guid);
				if (System.IO.Directory.Exists (path)) {
					var relativePath = path.Substring (src.Length + 1);
					if (relativePath.Split ('/').Length != 2)
						continue;
					var ai = AssetImporter.GetAtPath (path);
					var name = System.IO.Path.GetFileName (path).ToLowerInvariant () + ".ab";
					if (ai.assetBundleName != name) {
						ai.assetBundleName = name;
						Debug.Log ("名前をつけました。 " + path + " => " + ai.assetBundleName);
					}
				}
			}
		}
	}
}

