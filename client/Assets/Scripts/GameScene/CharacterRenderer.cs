using UnityEngine;
using System.Collections;
using Rogue;

public enum CharacterRendererState {
	None,
	Active,
}

public enum CharacterRendererPose {
	Move,
	Attack,
	Damage,
	Special,
}

public class CharacterRenderer : MonoBehaviour {

	public string AtlasName;
	public Direction Dir;

	public CharacterRendererState State;
	public CharacterRendererPose Pose = CharacterRendererPose.Move;

	static Vector2 BasePos = new Vector2 (0, 0.85f);
	static int[] RotateTable = new int[]{0,4,3,2,1,0,1,2,3};
    static bool[] FlipTable = new bool[] { false, false, false, false, false, true, true, true, true};

	SpriteRenderer sprite;

	void Awake(){
		var spriteObj = new GameObject ();
		sprite = spriteObj.AddComponent<SpriteRenderer> ();
		spriteObj.AddComponent<SimpleBillboard> ();
		spriteObj.transform.SetParent (this.transform, false);
		spriteObj.transform.localPosition = BasePos;
		sprite.sortingLayerID = SortingLayer.NameToID ("Character");
		if (AtlasName != null) redraw ();
	}

	void Update(){
        var anim = Mathf.FloorToInt(Time.time * 4) % 2 + 1;
        var imgNum = RotateTable [(int)Dir.Rotate(5)];
		string spriteName = "";
		switch (Pose) {
		case CharacterRendererPose.Move:
			spriteName = "move_" + imgNum + "_" + anim;
			break;
		case CharacterRendererPose.Attack:
			spriteName = "attack_" + imgNum + "_" + anim;
			break;
		case CharacterRendererPose.Special:
			spriteName = "special_" + anim;
			break;
		case CharacterRendererPose.Damage:
			spriteName = "damage";
			break;
		default:
			throw new System.Exception ("Invalid pose " + Pose);
		}

		var spr = ResourceCache.LoadSync<Sprite> (AtlasName + "$" + spriteName);
		sprite.sprite = spr;
		sprite.flipX = FlipTable [(int)Dir.Rotate(5)];

		switch( State ){
		case CharacterRendererState.None:
			sprite.color = Color.white;
			break;
		case CharacterRendererState.Active:
			sprite.color = Color.Lerp (Color.white, Color.blue, Mathf.Repeat( Time.time, 1.0f));
			break;
		}
	}

	void redraw(){
		var layer = LayerMask.NameToLayer("MapObject");
		sprite.gameObject.layer = layer;
	}
}
