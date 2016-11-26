using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RSG;
using Game;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public class GameScene : MonoBehaviour {

	public enum CameraMode {
		None,
		Normal,
	}

    public enum Mode
    {
        None,
        QMove,
        Walking,
    }

	public GameObject CameraTarget;
	public Camera MainCamera;
	public CharacterContainer CharacterBase;

	public GameSceneView View;

	public GameObject ActionButtons;
	public PoolBehavior ActionButtonBase;
	public PoolBehavior AttackMarkBase;

	public GameObject SkillCutinBase;

	public GameObject[] FieldObjects;

    Vector3 FocusToPoint; // カメラが向かうべきポイント
    float CameraDistanceTo; // カメラの距離
    int CurZoom = 1;
	CameraMode cameraMode = CameraMode.None;

    public Dictionary<int, CharacterContainer> Characters = new Dictionary<int, CharacterContainer>();

    public Field Field { get; private set; }

    public Mode mode;

	void Start(){
		var stage = G.Stages [0];

        mode = Mode.None;
        FocusToPoint = CameraTarget.transform.localPosition;
        CameraDistanceTo = -20f;
		ActionButtons.SetActive (false);

        Field = new Field();
		Field.Init (stage);

		initField ();

		Field.StartThread ();
    }

	public void OnSpeedupDown(){
		Time.timeScale = 5.0f;
	}

	public void OnSpeedupUp(){
		Time.timeScale = 1.0f;
	}

	public void initField(){
		var map = Field.Map;
		for (int z = 0; z < map.Height; z++) {
			for (int x = 0; x < map.Width; x++) {
				var cell = map [x, z];
				GameObject obj;
				if (cell.Val == 1) {
					obj = Instantiate (FieldObjects [0]);
				}else{
					obj = Instantiate (FieldObjects [1]);
				}
				obj.transform.localPosition = PointToVector (new Point (x, z));
			}
		}

		foreach (var ch in Field.Map.Characters) {
			ch.Dir = Direction.South;
			UpdateCharacter(ch);
		}

		var mainch = Field.FindCharacter ("P1");
		curCharacter = mainch;
		MoveCameraTo (PointToVector( mainch.Position));
		CameraDistanceTo = -40.0f;
		cameraMode = CameraMode.Normal;
	}

    void Update(){
		Application.targetFrameRate = 60;

		messageLoop ();
		updateWalking ();

        var curPos = CameraTarget.transform.localPosition;
        curPos = Vector3.Lerp(FocusToPoint, curPos, Mathf.Pow(0.05f, Time.deltaTime));
        CameraTarget.transform.localPosition = curPos;

		switch (cameraMode) {
		case CameraMode.Normal:
			var curDistance = MainCamera.transform.localPosition;
			curDistance.z = Mathf.Lerp (-Mathf.Abs(CameraDistanceTo), curDistance.z, Mathf.Pow (0.1f, Time.deltaTime));
			MainCamera.transform.localPosition = curDistance;
			break;
		default:
			break;
		}

		if (Input.GetKey (KeyCode.F)) {
			Time.timeScale = 10.0f;
		} else {
			Time.timeScale = 1.0f;
		}

	}

	void messageLoop(){
		while (true) {
			GameLog.ICommand log;
			if (Field.SendQueue.TryDequeue (out log)) {
				log.Process (this);
			} else {
				break;
			}
		}
	}

    void Send(GameLog.IRequest request)
    {
		if (request == null) {
			throw new System.ArgumentNullException ("Request must not be null");
		}
		Field.RecvQueue.Enqueue (request);
		System.Threading.Thread.Sleep(4); // ちょっとだけまつ
		messageLoop ();
    }

	void MoveCameraTo(Vector3 pos){
        FocusToPoint = pos;
		CameraTarget.transform.localPosition = pos;
	}

	public void OnBeginDrag(PointerEventData ev){
        if (ev.button != PointerEventData.InputButton.Left) return;
        //Debug.Log("Begin Drag");
    }

    public void OnEndDrag(PointerEventData ev){
        if (ev.button != PointerEventData.InputButton.Left) return;
        //Debug.Log("End Drag");
    }

    public void OnDrag(PointerEventData ev){
        if (ev.button != PointerEventData.InputButton.Left) return;
        var delta = new Vector3 (ev.delta.x / Screen.width, 0, ev.delta.y / Screen.height);
        MoveCameraTo( CameraTarget.transform.localPosition - delta * 10f);
	}

	public void OnPointerClick (PointerEventData ev){
        if (ev.button != PointerEventData.InputButton.Left) return;
        if (ev.dragging == true) return;

		Game.Point hit;
		if (LaycastByScreenPosInt (ev.position, out hit)) {
            Debug.Log(hit);
            switch (mode)
            {
            case Mode.QMove:
				OnFieldClick (hit);
                break;
            default:
                // DO NOTHING;
                break;
            }
		}
	}

	public void OnFieldClick(Point pos){
		Debug.Log ("from" + curCharacter.Position + " to " + pos);
		List<Point> path = null;
		//Field.Map.TemporaryOpen (curCharacter.Position, () => {
			path = Field.Map.PathFinder.FindPath (curCharacter.Position, pos, Field.Map.StepWalkableNow (), 10);
		//});
		if (path != null) {
			View.ShowCursor (path);
			var req = GameLog.WalkRequest.CreateInstance ();
			req.Path = path.Select (p => new GameLog.Point (p)).ToList ();
			Send (req);
			mode = Mode.None;
		}
	}

    public void OnZoomButtonClick()
    {
        CurZoom = (CurZoom + 1) % 3;
        float[] zooms = { 15, 20, 30 };
        CameraDistanceTo = -zooms[CurZoom];
    }

    public void FocusTo(Vector3 pos)
    {
        Plane plane = new Plane(new Vector3(0,1,0), 0);
        Ray ray = MainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        ray.origin = pos;
        float enter;
        if (plane.Raycast(ray, out enter)) {
            FocusToPoint = ray.GetPoint(enter);
        }
    }

	public Vector3 PointToVector(Point p){
		return new Vector3 (p.X + 0.5f, 0, p.Y + 0.5f);
	}

	// スクリーン座標から、マス目の位置を取得する
	public bool LaycastByScreenPos(Vector2 screenPos, out Vector2 hitPos){
		RaycastHit hit;
		if (!Physics.Raycast (MainCamera.ScreenPointToRay (screenPos), out hit, 1000, 1 << LayerMask.NameToLayer("MapGrid"))) {
			hitPos = new Vector2 ();
			return false;
		} else {
			hitPos = new Vector2 (hit.point.x, hit.point.z);
			return true;
		}
	}

    public bool LaycastByScreenPosInt(Vector2 screenPos, out Point hitPos)
    {
        Vector2 hit;
        if( LaycastByScreenPos(screenPos, out hit))
        {
            hitPos = new Point(Mathf.FloorToInt(hit.x), Mathf.FloorToInt(hit.y));
            return true;
        }
        else
        {
            hitPos = new Point();
            return false;
        }
    }

	//===================================================================
	// キャラクタの描画
	//===================================================================

    public void RedrawAll()
    {
        foreach( var ch in Field.Map.Characters)
        {
			if (ch.Active) {
				UpdateCharacter (ch);
			}
        }
    }

    public CharacterContainer GetCharacterRenderer(Character ch)
    {
        CharacterContainer cc;
        if (!Characters.TryGetValue(ch.Id, out cc))
        {
            cc = Instantiate(CharacterBase);
            cc.name = ch.Name + ":" + ch.Id;
			ResourceCache.Load<SpriteSet> (string.Format ("Enemy{0:0000}", ch.AtlasId)).Done (ss => {
				cc.Chara.SpriteSet = ss;
			});
            Characters[ch.Id] = cc;
        }
        return cc;
    }

    public void UpdateCharacter(Character ch)
    {
        var cr = GetCharacterRenderer(ch);
        var pos = new Vector3(ch.Position.X + 0.5f, 0, ch.Position.Y + 0.5f);
        cr.transform.localPosition = pos;
    }

	//===================================================================
	// アクションボタン
	//===================================================================

	Action<string> OnActionButtonClick;

	public void OpenAction(Action<string> onClick){
		ActionButtons.SetActive (true);
		ActionButtonBase.ReleaseAll ();
		OnActionButtonClick = onClick;
	}

	public void AddAction(string action, string name){
		var buttonObj = ActionButtonBase.Create ();
		buttonObj.FindByName<Text> ("Text").text = name;
		buttonObj.name = "ActionButton:" + action;
	}

	public void CloseAction(){
		ActionButtonBase.ReleaseAll ();
		ActionButtons.SetActive (false);
	}

	public void OnActionClick(GameObject btn){
		var id = btn.GetStringId ();
		OnActionButtonClick (id);
	}

	//===================================================================
	// 2Dエフェクト
	//===================================================================

	bool fadeEnabled;
	public SpriteRenderer Fade;

	public void SetFade(bool val){
		if (val == fadeEnabled)	return;
		fadeEnabled = val;
		if (val) {
			Fade.gameObject.SetActive (true);
		} else {
			Fade.gameObject.SetActive (false);
		}
	}

	public Camera ScreenEffectCamera;

	public IPromise<GameObject> ShowScreenEffect(string name){
		return ResourceCache.Create<GameObject> (name);
	}

	//===================================================================
    // メッセージハンドラ
    //===================================================================

    Character curCharacter;
	Point[] curAttackRange;

	/*
    public void QMove(Message mes)
    {
        var ch = (Character)mes.Param[0];
        var cr = GetCharacterRenderer(ch);
        Point[] range = null;

		cr.Chara.State = CharacterRendererState.Active;
        mode = Mode.QMove;
        curCharacter = ch;

		OpenAction (act=>{
			switch(act){
			case "cancel":
				CloseAction();
				break;
			case "move":
				CloseAction();
				break;
			case "attack":
				CloseAction();
				break;
			case "skill":
				CloseAction();
				break;
			default:
				throw new Exception("invalid action" + act);
			}
		});
		AddAction ("cancel", "キャンセル");
		AddAction ("move", "完了");
		AddAction ("attack", "攻撃");
		AddAction ("skill", "スキル");

        FocusTo(cr.transform.position);
    }
    */

	public void StartWalking(CharacterContainer cc, Point to){
		var pos3 = PointToVector (to);
		walkings.Add (new Walking {
			CharacterContainer = cc,
			From = cc.transform.localPosition,
			To = pos3,
			Duration = 0.2f,
			CurrentTime = walkingRest,
			OnFinished = ()=>{ 
				Send(GameLog.AckRequest.CreateInstance()); 
				messageLoop(); 
			}
		});
	}

	public class Walking {
		public CharacterContainer CharacterContainer;
		public Vector3 From;
		public Vector3 To;
		public float Duration;
		public float CurrentTime;
		public Action OnFinished;
	}

	List<Walking> walkings = new List<Walking> ();
	float walkingRest;

	// 歩きのupdateごとの処理
	void updateWalking(){
		walkingRest = 0;

		float deltaTime = Time.deltaTime;
		foreach (var w in walkings) {
			w.CurrentTime += deltaTime;
			if (w.CurrentTime >= w.Duration) {
				w.CurrentTime = w.Duration;
			}
			w.CharacterContainer.transform.localPosition = Vector3.Lerp (w.From, w.To, w.CurrentTime / w.Duration);
			FocusToPoint = w.CharacterContainer.transform.localPosition;
		}
		Action onfinished = null;
		foreach (var w in walkings) {
			if (w.OnFinished != null && w.CurrentTime >= w.Duration) {
				walkingRest = w.CurrentTime - w.Duration;
				onfinished = w.OnFinished;
			}
		}
		walkings.RemoveAll (w => (w.CurrentTime >= w.Duration));
		if( onfinished != null ) onfinished ();
	}

	#if false
	public void RedrawChar(Message mes){
		var ch = (Character)mes.Param[0];
		var cr = GetCharacterRenderer(ch);
		UpdateCharacter (ch);
		cr.transform.localRotation = ch.Dir.ToWorldQuaternion ();
		Send(GameLog.AckRequest.CreateInstance()); 
	}

	IEnumerator Attack(Message mes)
	{
		var ch = (Character)mes.Param[0];
		var target = (Character)mes.Param [1];
		var dir = (Direction)mes.Param [2];
		var damage = (int)mes.Param [3];

		var cr = GetCharacterRenderer(ch);
		var targetCr = GetCharacterRenderer (target);

		cr.transform.localRotation = dir.ToWorldQuaternion ();

		CameraDistanceTo = 15f;
		yield return new WaitForSeconds(0.2f);

		cr.Animate ("EnemyAttack01");
		yield return new WaitForSeconds(1.0f);

		targetCr.transform.localRotation = dir.Rotate(4).ToWorldQuaternion ();
		targetCr.Animate ("EnemyDamage01");

		FocusTo(cr.transform.position);

		yield return new WaitForSeconds(1.0f);

		CameraDistanceTo = 20f;

		cr.Chara.Pose = CharacterRendererPose.Move;
		targetCr.Chara.Pose = CharacterRendererPose.Move;

		RedrawAll();
		Send(GameLog.AckRequest.CreateInstance()); 
	}

	public GameObject SkFire;
	IEnumerator Skill(Message mes)
	{
		var ch = (Character)mes.Param[0];
		var targets = (Character[])mes.Param [1];
		var dir = (Direction)mes.Param [2];
		var path = (Point[])mes.Param [3];

		var cr = GetCharacterRenderer(ch);
		SetFade (true);
		var cutin = Instantiate(SkillCutinBase);
		cutin.SetActive(true);
		cr.Chara.Unfade = true;
		//var targetCr = GetCharacterRenderer (target);

		cr.transform.localRotation = dir.ToWorldQuaternion ();

		yield return new WaitForSeconds(4.0f);

		var skillPos = PointToVector (path [path.Length / 2]);
		FocusTo (skillPos);

		cr.Animate ("EnemyAttack01");
		yield return new WaitForSeconds(1.0f);

		foreach (var pos in path) {
			var obj = Instantiate (SkFire);
			obj.SetActive (true);
			obj.transform.position = PointToVector (pos);
			yield return new WaitForSeconds (0.35f);
		}

		//targetCr.transform.localRotation = dir.Rotate(4).ToWorldQuaternion ();
		//targetCr.Animate ("EnemyDamage01");

		FocusTo(cr.transform.position);

		yield return new WaitForSeconds(1.0f);

		cr.Chara.Pose = CharacterRendererPose.Move;
		//targetCr.Chara.Pose = CharacterRendererPose.Move;

		cr.Chara.Unfade = false;
		SetFade (false);

		RedrawAll();
		Send(GameLog.AckRequest.CreateInstance()); 
	}
	#endif
}
