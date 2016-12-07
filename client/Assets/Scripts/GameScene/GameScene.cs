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
		Field.NoLog = true;
		Field.InitRandom (stage);

		initField ();

		Field.StartThread ();
		Send (new GameLog.AckResponseRequest ());

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
				if (cell.Val == 1 || cell.Val == 2) {
					obj = Instantiate (FieldObjects [0]);
				}else{
					obj = Instantiate (FieldObjects [1]);
				}
				obj.transform.localPosition = PointToVector (new Point (x, z));
				cell.Obj = obj.gameObject;
			}
		}

		foreach (var ch in Field.Map.Characters) {
			ch.Dir = Direction.South;
			UpdateCharacter(ch);
		}

		MoveCameraTo (PointToVector( Field.Player.Position));
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
			List<GameLog.ICommand> cmds = Field.ReadCommandsAsync ();
			if (cmds == null) {
				break;
			}
			CloseAction ();

			var promises = cmds.Select (cmd => {
				return cmd.Process (this);
			}).ToArray ();

			Promise.All (promises).Done (() => {
				switch (Field.WaitingType) {
				case WaitingType.None:
					break;
				case WaitingType.Ack:
					Send (new GameLog.AckResponseRequest ());
					break;
				case WaitingType.Action:
					mode = Mode.QMove;
					OpenAction (act=>{
						switch(act){
						case "auto":
							var path = Field.Thinking.ThinkAutoMove(Field.Player);
							if( path.Count > 0 ){
								StartMove(path);
							}
							break;
						case "attack":
							break;
						default:
							throw new Exception("invalid action" + act);
						}
					});
					AddAction ("auto", "自動");
					AddAction ("attack", "すぶり");
					break;
				}
			});
		}
	}

    public void Send(GameLog.IRequest request)
    {
		if (request == null) {
			throw new System.ArgumentNullException ("Request must not be null");
		}
		Field.RequestAsync (request);
		System.Threading.Thread.Sleep(0); // ちょっとだけまつ
		messageLoop ();
    }

	public void StartMove(List<Point> path){
		View.ShowCursor (path);
		var req = new GameLog.WalkRequest {
			Path = path.Select (p => new GameLog.Point (p)).ToList ()
		};
		mode = Mode.None;
		Send (req);
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

		// 攻撃
		if (Field.Player.Position == pos) {
			mode = Mode.None;
			Send (new GameLog.SkillRequest (){ CharacterId = Field.Player.Id, Dir = (int)Field.Player.Dir, SkillId = "attack" });
			return;
		}
			
		var target = Field.Map [pos].Character;
		if (target != null) {
			if (target != Field.Player && (Field.Player.Position-pos).GridLength() == 1) {
				var dir = (pos - Field.Player.Position).ToDir ();
				mode = Mode.None;
				Send (new GameLog.SkillRequest (){ CharacterId = Field.Player.Id, Dir = (int)dir, SkillId = "attack" });
				return;
			}
		}

		// 移動
		List<Point> path = null;
		path = Field.Map.PathFinder.FindPath (Field.Player.Position, pos, Field.Map.StepWalkableNow (), 10);

		if (path != null) {
			View.ShowCursor (path);
			var req = new GameLog.WalkRequest {
				Path = path.Select (p => new GameLog.Point (p)).ToList ()
			};
			mode = Mode.None;
			Send (req);
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

    public void UpdateCharacter(Character c)
    {
        var cc = GetCharacterRenderer(c);
        //var pos = new Vector3(c.Position.X + 0.5f, 0, c.Position.Y + 0.5f);
        //cc.transform.localPosition = pos;
		cc.Text.text = c.Name + "\nHP:" + c.Hp;
    }

	public void UpdateViewport(){
		Field.UpdateViewport ();

		var map = Field.Map;
		for (int z = 0; z < map.Height; z++) {
			for (int x = 0; x < map.Width; x++) {
				var obj = map [x, z].Obj;
				if (obj != null) {
					Color col;
					if (map [x, z].Viewport ) {
						col = Color.white;
					} else if (map [x, z].Open) {
						col = Color.gray;
					} else {
						col = Color.black;
					}
					obj.GetComponent<ColorChanger> ().Color = col;
					var c = map [x, z].Character;
					if (c != null) {
						var cc = GetCharacterRenderer (c);
						cc.GetComponent<CharacterContainer> ().Chara.OverColor = col;
					}
				}
			}
		}
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

	public IPromise StartWalking(Walking w){
		var promise = new Promise ();
		walking = w;
		w.Duration = 0.2f;
		w.CurrentTime = walkingRest;
		w.OnFinished = () => {
			promise.Resolve ();
		};
		return promise;
	}

	public class Walking {
		public class Item {
			public CharacterContainer CharacterContainer;
			public Vector3 From;
			public Vector3 To;
		}

		public Item[] Items;
		public float Duration;
		public float CurrentTime;
		public Action OnFinished;
	}

	Walking walking;
	float walkingRest;

	// 歩きのupdateごとの処理
	void updateWalking(){
		walkingRest = 0;
		if (walking == null) {
			return;
		}

		walking.CurrentTime += Time.deltaTime;
		var cur = Mathf.Min(walking.CurrentTime, walking.Duration);

		foreach (var w in walking.Items) {
			w.CharacterContainer.transform.localPosition = Vector3.Lerp (w.From, w.To, cur / walking.Duration);
		}

		if (walking.CurrentTime >= walking.Duration) {
			var onFinished = walking.OnFinished;
			walkingRest = walking.CurrentTime - walking.Duration;
			walking = null;
			View.SpendCurosr ();

			if (onFinished != null) {
				onFinished ();
			}
		}

		var player = GetCharacterRenderer(Field.Player);
		FocusToPoint = player.transform.localPosition;
	}

}
