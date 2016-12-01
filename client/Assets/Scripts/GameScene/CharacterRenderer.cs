using UnityEngine;
using System.Collections;
using Game;

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

[ExecuteInEditMode]
public class CharacterRenderer : MonoBehaviour {

	public SpriteRenderer Sprite;

	[SerializeField]
	public int DirInt { get { return (int)Dir; } set { Dir = (Direction)value; } }
	public float PoseNum;
	public float AnimNum;

	[System.NonSerialized]
	public Color OverColor = Color.white;

	public SpriteSet SpriteSet;
	//public string AtlasName;
	public Direction Dir;

	public CharacterRendererState State;
	public CharacterRendererPose Pose = CharacterRendererPose.Move;
	public bool Unfade;

	static Vector2 BasePos = new Vector2 (0, 0.85f);
	static int[] RotateTable = new int[]{0,0,1,2,3,4,3,2,1};
	static bool[] FlipTable = new bool[] { false, true, true, true, true, false, false, false, false, };

	void Awake(){
		if (Sprite == null) {
			var spriteObj = new GameObject ();
			spriteObj.hideFlags = HideFlags.HideAndDontSave;
			Sprite = spriteObj.AddComponent<SpriteRenderer> ();
			spriteObj.AddComponent<SimpleBillboard> ();
			spriteObj.transform.SetParent (this.transform, false);
			spriteObj.transform.localPosition = BasePos;
			Sprite.sortingLayerID = SortingLayer.NameToID ("Character");
		}
		redraw ();
	}

	void OnRenderObject(){
		#if UNITY_EDITOR
		if( !Application.isPlaying ){
			RecalcDir (UnityEditor.SceneView.lastActiveSceneView.camera);
		}else{
			if( Unfade ){
				Sprite.sortingLayerName = "OverFade";
			}else{
				Sprite.sortingLayerName = "Character";
			}
			RecalcDir(Camera.current);
		}
		#else
		RecalcDir(Camera.current);
		#endif
	}

	void RecalcDir(Camera camera){
		if (SpriteSet == null) {
			return;
		}

		if (camera == null) {
			return;
		}

		var cameraDir = GetCameraDirection (camera);

		var displayDir = Dir.Rotate (Mathf.RoundToInt(cameraDir / 45f));
        //var anim = Mathf.FloorToInt(Time.time * 4) % 2 + 1;
		var anim = Mathf.RoundToInt(AnimNum) % 2 + 1;
		var pose = (CharacterRendererPose)Mathf.RoundToInt (PoseNum);
		var imgNum = RotateTable [(int)displayDir];
		string spriteName = "";
		switch (pose) {
		case CharacterRendererPose.Move:
			spriteName = "move_" + imgNum + "_" + anim;
			break;
		case CharacterRendererPose.Attack:
			anim = (anim - 1) % 2 + 1;
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

		Sprite.sprite = SpriteSet.Find(spriteName);
		Sprite.flipX = FlipTable [(int)displayDir];

		switch( State ){
		case CharacterRendererState.None:
			Sprite.color = Color.white * OverColor;
			break;
		case CharacterRendererState.Active:
			Sprite.color = Color.Lerp (Color.white, Color.blue, Mathf.Repeat( Time.time, 1.0f)) * OverColor;
			break;
		}

	}

	float GetCameraDirection(Camera camera){
		var cameraForward = -horizontalize(camera.transform.forward);
		var cameraRight = horizontalize(camera.transform.right);
		var fwd = horizontalize(transform.forward);
		return angle360 (cameraForward, fwd, cameraRight);
	}

	static Vector3 horizontalize(Vector3 v){
		v.y = 0;
		return v.normalized;
	}

	static float angle360(Vector3 from, Vector3 to, Vector3 right)
	{
		float angle = Vector3.Angle(from, to);
		return (Vector3.Angle(right, to) > 90f) ? 360f - angle : angle;            
	}

	/*
	void OnPreRender(){
		var camera = Camera.current;
		var ray = camera.ViewportPointToRay (new Vector3 (0.5f, 0.5f));
		var cross = Vector3.Cross (ray.direction, transform.up);
		Debug.Log ("cross="+cross);
	}
	*/

	void redraw(){
		if (SpriteSet == null) {
			return;
		}
		var layer = LayerMask.NameToLayer("MapObject");
		Sprite.gameObject.layer = layer;
	}
}
