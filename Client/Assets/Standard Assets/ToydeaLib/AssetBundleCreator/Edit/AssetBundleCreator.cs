﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;

/// <summary>
/// ビルド用クラス
/// </summary>
[InitializeOnLoad]
public class AssetBundleCreator : AssetPostprocessor
{
    /// <summary>
    /// アセットバンドル名の命名ルールの基底クラス
    /// </summary>
    public abstract class NamingRule
    {
        /// <summary>
        /// ルールに基づき、PATHからアセットバンドル名を取得する
        /// </summary>
        /// <param name="path">対象のアセットのパス</param>
        /// <returns>アセットバンドル名、ない場合は null を返す</returns>
        public abstract string GetAssetBundleName(string path);
    }

    /// <summary>
    /// 固定でアセットバンドルの名前をつける
    /// </summary>
    public class StaticRule : NamingRule
    {
        public string Pattern { get; private set; }
        public string AssetBundleName { get; private set; }

        public StaticRule(string pattern, string assetBundleName)
        {
            Pattern = pattern;
            AssetBundleName = assetBundleName;
        }

        public override string GetAssetBundleName(string path)
        {
            if (path.Length > Pattern.Length && path.Substring(0, Pattern.Length) == Pattern)
            {
                return AssetBundleName + ".ab";
            }
            else
            {
                return null;
            }

        }
    }

    /// <summary>
    /// アセットバンドル名の命名ルール.
    /// 
    /// NamePositionの指定は、0なら、pattern直下のファイル名、1なら１段下のファイル名、-1ならpatternの最後のパス名が使用される。
    /// 
    /// 例:
    /// var path = "Assets/Hoge/Fuga/Piyo.png";
    /// (new AssetBundleRule("Assets/Hoge/", 0).GetAssetBundleName(path);  // => "Fuga"
    /// (new AssetBundleRule("Assets/Hoge/", 1).GetAssetBundleName(path);  // => "Piyo"
    /// (new AssetBundleRule("Assets/Hoge/", -1).GetAssetBundleName(path);  // => "Hoge"
    /// </summary>
    public class PathRule : NamingRule
    {
        /// <summary>
        /// DataPathからの相対パス
        /// </summary>
        public string Pattern { get; private set; }

        /// <summary>
        /// アセットバンドル名を指定するパスの位置.
        /// 0なら、pattern直下のファイル名、1なら１段下のファイル名、-1ならpatternの最後のパス名、となる。
        /// </summary>
        public int NamePosition { get; private set; }

        public PathRule(string pattern, int namePosition)
        {
            Pattern = pattern;
            NamePosition = namePosition;
        }

        // PATHからアセットバンドル名を取得する
        public override string GetAssetBundleName(string path)
        {
            if( path.Length > Pattern.Length && path.Substring(0, Pattern.Length) == Pattern)
            {
                if (NamePosition < 0)
                {
                    // NamePositionが-1の場合は、フォルダ名自体がアセバン名になる
                    var pathList = Pattern.Split('/');
                    return pathList[pathList.Length - 2].ToLowerInvariant() + ".ab";
                }
                else
                {
                    var rest = path.Substring(Pattern.Length);
                    var pathList = rest.Split('/');
                    if (pathList.Length > NamePosition)
                    {
                        // 合致!!
                        var filename = pathList[NamePosition];
                        var dotIndex = filename.IndexOf('.');
                        if (dotIndex >= 0)
                        {
                            // 拡張子を取る
                            return filename.Substring(0, dotIndex).ToLowerInvariant() + ".ab";
                        }
                        else
                        {
                            return filename.ToLowerInvariant() + ".ab";
                        }
                    }
                    else
                    {
                        // インデックスを超えている
                        return null;
                    }
                }
            }
            else
            {
                return null;
            }
        }

    }

    public static string[] RootPaths = new string[] {
        "Assets/Gardens",
    };
    public static List<NamingRule> Rules = new List<NamingRule>();

    static AssetBundleCreator() {
        Rules.Add(new PathRule("Assets/Gardens/", 1));
    }

    /// <summary>
    /// ビルド実行
    /// </summary>
    static void Build()
    {
        //string outPutPath = "";
        string buildTargetString = "";
        string[] args = System.Environment.GetCommandLineArgs();
        Debug.Log("args parse");
        for (int i = 0; i < args.Length ;++i )
        {
            switch (args[i])
            {
                case "/outputpath":
                    Debug.Log("/outputpath :" + args[i+1]);
                    //outPutPath = args[i+1];
                    break;

                case "/target":
                    Debug.Log("/target :" + args[i+1]);
                    buildTargetString = args[i+1];
                    break;
            }
        }

        var target = BuildTargetFromString(buildTargetString);
        var outputPath = Path.Combine(Application.dataPath, "..\\AssetBundles\\" + OutputPathFromBuildTarget(target));

        CreateAssetBundlesForTarget(target, outputPath);

    }

    /// <summary>
    /// ビルドターゲットから、出力フォルダを取得する
    /// </summary>
    static string OutputPathFromBuildTarget(BuildTarget target)
    {
        switch( target)
        {
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "iOS";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
            case BuildTarget.StandaloneOSXUniversal:
            case BuildTarget.StandaloneOSXIntel:
            case BuildTarget.StandaloneOSXIntel64:
                return "StandaloneWindows";
            default:
                throw new System.Exception("Unknown build target! " + target);
        }
    }

    /// <summary>
    /// 文字列から、BuildTargetに変換する
    /// </summary>
    static BuildTarget BuildTargetFromString(string val)
    {
        return (BuildTarget)System.Enum.Parse(typeof(BuildTarget), val);
    }

    [MenuItem("Window/AssetBundleCreator/アセットバンドルを作成")]
    public static void CreateAssetBundles()
    {
        NameAllAssets();

        var target = EditorUserBuildSettings.activeBuildTarget;
        var outputPath = Path.Combine(Application.dataPath, "..\\AssetBundles\\" + OutputPathFromBuildTarget(target));
        CreateAssetBundlesForTarget(target, outputPath);
    }


    public static void CreateAssetBundlesForTarget(BuildTarget target, string outputPath)
    {
        var opt = BuildAssetBundleOptions.IgnoreTypeTreeChanges;

        // Windowsの場合、32bit/64bitの区別はしないで現在のターゲットにあわせる
        if ((target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
            && (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows || EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64))
        {
            target = EditorUserBuildSettings.activeBuildTarget;
        }

        // ターゲットプラットフォームをスイッチする
        if (EditorUserBuildSettings.activeBuildTarget != target)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(target);
        }

        BuildPipeline.BuildAssetBundles( outputPath, opt, target);
    }

    [MenuItem("Window/AssetBundleCreator/名前をつける")]
    public static void NameAllAssets()
    {
        int num = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            var guids = AssetDatabase.FindAssets(null, RootPaths);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (SetAssetBundleNameForAsset(path))
                {
                    num++;
                }
            }

        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        Debug.Log("Finished. " + num + " assets modofied.");
    }

    [MenuItem("Window/AssetBundleCreator/名前をつける（すべてを対象、タグ削除も行う）")]
    public static void NameAllAssetsForce()
    {
        int num = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            var guids = AssetDatabase.FindAssets(null);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (SetAssetBundleNameForAsset(path, true))
                {
                    num++;
                }
            }

        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        Debug.Log("Finished. " + num + " assets modofied.");
    }

    /// <summary>
    /// アセットにアセットバンドル名をつける
    /// </summary>
    /// <param name="path">対象のアセットのパス</param>
    /// <param name="force">アセバン名がない場合に、アセバン名をクリアするか</param>
    /// <returns></returns>
    public static bool SetAssetBundleNameForAsset(string path, bool force = false)
    {
        // ディレクトリは無視
        FileAttributes attr = File.GetAttributes(path);
        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
        {
            return false;
        }

        foreach (var rule in Rules)
        {
            var assetBundleName = rule.GetAssetBundleName(path);
            if (assetBundleName != null)
            {
                var importer = AssetImporter.GetAtPath(path);
                if ( importer.assetBundleName != assetBundleName)
                {
                    Debug.Log("Modify asset bundle name to " + assetBundleName + " at " + path);
                    importer.assetBundleName = assetBundleName;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        if( force)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null && !string.IsNullOrEmpty(importer.assetBundleName))
            {
                Debug.Log("Modify asset bundle name to " + "" + " at " + path);
                importer.assetBundleName = "";
            }
        }
        return false;
    }


    public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
    {
        var assets = importedAssets.Concat(movedAssets).ToArray();

        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var asset in assets)
            {
                SetAssetBundleNameForAsset(asset, true);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
    }
}