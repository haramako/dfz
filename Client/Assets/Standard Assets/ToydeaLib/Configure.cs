using UnityEngine;
using System.Collections.Generic;
using DebuggerNonUserCodeAttribute = System.Diagnostics.DebuggerNonUserCodeAttribute;
using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;
//using SLua;

//[CustomLuaClass]
public class Configure {
	static Dictionary<string,string> dict;

	static void init(){
		if( dict != null ) return;
		dict = new Dictionary<string,string>();

		LoadFromResource("Misc/config_production");
		#if UNITY_ANDROID
		LoadFromResource("Misc/config_android");
		#endif
		#if UNITY_IPHONE
		LoadFromResource("Misc/config_ios");
		#endif
		#if UNITY_N3DS
		LoadFromResource("Misc/config_n3ds");
		#endif
		if (Configure.GetBool("SmartPass")) LoadFromResource("Misc/config_smart_pass");
		if (Configure.GetBool("AppPass")) LoadFromResource("Misc/config_app_pass");
#if UNITY_EDITOR
		try{
			LoadFromResource("Misc/config_development");
		}catch( System.Exception err){
			Debug.Log (err);
		}
#endif
	}

	public static void LoadFromResource(string resourceName){
		var asset = Resources.Load<TextAsset>(resourceName);
		if (asset == null) return; //throw new System.Exception ("cannot load '" + resourceName +"'");
		Parse(asset.text);
	}

	public static void Parse(string text){
		init();
		foreach( string rawLine in text.Split (new char[]{'\n'}) ){
			string line = rawLine.Split(new string[]{" #"}, 2, System.StringSplitOptions.None)[0]; // #を文字列の中で使いたいため、スペース# を対象にいている
			line = line.Trim ();
			if( line == "" || line[0] == '#' ) continue;
			var keyValue = line.Split (new char[]{'='}, 2);
			if( keyValue.Length != 2 ){ 
				if (Debug.isDebugBuild) {
					Debug.Log ( "invalid config '"+line+"'" ); 
				}
				continue; 
			}
			dict[keyValue[0].Trim ().ToLower ()] = keyValue[1].Trim ();
		}
	}

	public static string GetRaw(string key, string _default = null){
		init();
		string r;
		if (dict.TryGetValue (key.ToLower (), out r)) {
			return r;
		}else{
			return _default;
		}
	}

	/// <summary>
	/// Platformによって違う可能性のある設定を取得する.
	/// GetWithPlatform("Hoge") なら "Hoge.Android" , "Hoge" の順に検索して
	/// 見つからなかった場合は、_defaultで指定された値を返す。
	/// </summary>
	/// <returns>取得した設定値（みつからなかった場合は, _default を返す）</returns>
	/// <param name="key">キー</param>
	/// <param name="_default">見つからなかった場合に使用されるデフォルト値</param>
	public static string Get(string key, string _default = null){
		var platform = GetRaw("platform");
		string r = GetRaw(key + "." + platform );
		if (r != null) return r;
		r = GetRaw (key);
		if (r != null) return r;
		return _default;
	}

	public static bool GetBool(string key, bool _default = false){
		string v = Get(key);
		return (v==null)?_default:(v=="true");
	}
		
	public static int GetInt(string key, int _default = 0 ){
		string v = Get(key);
		return (v==null)?_default:int.Parse (v);
	}

	public static float GetFloat(string key, float _default = 0 ){
		string v = Get(key);
		return (v==null)?_default:float.Parse (v);
	}

	/// コンディションによるActionの実行を行う
	//[Conditional("UNITY_EDITOR")]
	[DebuggerNonUserCode]
	public static void Condition(string key, System.Action action){
		if( GetBool(key) ) action();
	}

	/// コンディションによるログ出力を行う
	[Conditional("UNITY_EDITOR")]
	[DebuggerNonUserCode]
	public static void Log(string key, string message){
		if (GetBool (key)) {
			Debug.Log (message);
		}
	}

	/// コンディションによるログ出力を行う
	[Conditional("UNITY_EDITOR")]
	[DebuggerNonUserCode]
	public static void LogWarning(string key, string message){
		if (GetBool (key)) {
			Debug.LogWarning (message);
		}
	}

	/// コンディションによるログ出力を行う
	[Conditional("UNITY_EDITOR")]
	[DebuggerNonUserCode]
	public static void LogError(string key, string message){
		if (GetBool (key)) {
			Debug.LogError (message);
		}
	}
}
