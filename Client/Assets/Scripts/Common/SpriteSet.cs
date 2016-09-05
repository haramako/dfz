using UnityEngine;
using System.Collections;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpriteSet : ScriptableObject {
	public Sprite[] Sprites;

	public Sprite Find(string name){
		return Sprites.FirstOrDefault (s => s.name == name);
	}

#if UNITY_EDITOR
	[MenuItem("Tools/CreateSpriteSet")]
	public static void Create(){
		var objs = Selection.objects;
		foreach (var obj in Selection.objects) {
			var dir = obj as DefaultAsset;
			CreateFromDir (dir);
		}
	}

	public static void CreateFromDir(DefaultAsset dir){
		var dirPath = AssetDatabase.GetAssetPath (dir.GetInstanceID ());
		var spriteGuids = AssetDatabase.FindAssets ("t:sprite", new string[]{ dirPath });
		var sprites = spriteGuids
			.Select (guid => AssetDatabase.GUIDToAssetPath (guid))
			.Select (path => AssetDatabase.LoadAssetAtPath<Sprite> (path))
			.ToArray ();

		var obj = ScriptableObject.CreateInstance<SpriteSet> ();
		obj.Sprites = sprites;
		AssetDatabase.CreateAsset (obj, dirPath + "/" + dir.name + ".asset");

		EditorUtility.SetDirty (obj);

	}
#endif
}
