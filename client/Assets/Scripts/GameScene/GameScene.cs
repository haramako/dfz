using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RSG;
using Rogue;
using UnityEngine.UI;

public class GameScene : MonoBehaviour {

    public enum Mode
    {
        None,
        QMove,
        Walking,
    }

	public Terrain Terrain;
	public MeshRenderer TerrainGridMesh;
    public TerrainGrid TerrainGridActive;
	public GameObject CameraTarget;
	public Camera MainCamera;
	public CharacterRenderer CharacterBase;

	public GameObject ActionButtons;
	public PoolBehavior ActionButtonBase;
	public PoolBehavior AttackMarkBase;

    Vector3 FocusToPoint; // カメラが向かうべきポイント
    float CameraDistanceTo; // カメラの距離
    int CurZoom = 1;

    public new Dictionary<int, CharacterRenderer> Characters = new Dictionary<int, CharacterRenderer>();

    public Game Game { get; private set; }

    Mode mode;

	void Start(){

        mode = Mode.None;
        TerrainGridActive.SetActiveGrids(new Point[0]);
        FocusToPoint = CameraTarget.transform.localPosition;
        CameraDistanceTo = -20f;
		ActionButtons.SetActive (false);

        Game = new Game();
        Game.Init();
        RedrawAll();

        Game.StartThread();
    }

    void Update(){
		Application.targetFrameRate = 60;

        var curPos = CameraTarget.transform.localPosition;
        curPos = Vector3.Lerp(FocusToPoint, curPos, Mathf.Pow(0.05f, Time.deltaTime));
        CameraTarget.transform.localPosition = curPos;

        var curDistance = MainCamera.transform.localPosition;
        curDistance.z = Mathf.Lerp(CameraDistanceTo, curDistance.z, Mathf.Pow(0.1f, Time.deltaTime));
        MainCamera.transform.localPosition = curDistance;


        if (mode == Mode.None)
        {
            Rogue.Message mes;
            if (Game.SendQueue.TryDequeue(out mes))
            {
                this.SendMessage(mes.Type, mes);
            }
        }
	}

    void Send(string command, params object[] param)
    {
        Game.RecvQueue.Enqueue(new Message(command, param));
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
		var rot = Quaternion.Euler (0, 45, 0);
		delta = rot * delta;
        MoveCameraTo( CameraTarget.transform.localPosition - delta * 10f);
	}

	public void OnPointerClick (PointerEventData ev){
        if (ev.button != PointerEventData.InputButton.Left) return;
        if (ev.dragging == true) return;

		Rogue.Point hit;
		if (LaycastByScreenPosInt (ev.position, out hit)) {
            Debug.Log(hit);
            switch (mode)
            {
            case Mode.QMove:
                {
					if (hit == curPosition) {
						OnActionButtonClick ("move");
					} else if (curRange.Any (p => (p == hit))) {
						Debug.Log ("from" + curCharacter.Position + " to " + hit);
						Point[] path = null;
						Game.Map.TemporaryOpen (curCharacter.Position, () => {
							path = Game.Map.PathFinder.FindPath (curPosition, hit, 6, Game.Map.StepWalkableNow ()).ToArray ();
						});
						if (path != null) {
							curPosition = hit;
							Walking (curCharacter, path);
						}
					}else if( curAttackRange.Any(p=>(p==hit))){
						if ((hit - curPosition).GridLength () <= 1) {
							var cr = GetCharacterRenderer (curCharacter);
							cr.State = CharacterRendererState.None;
							Send("AMove", curPath, "Attack", hit);
							mode = Mode.None;
							CloseAction();
						}
					}
                }
                break;
            default:
                // DO NOTHING;
                break;
            }
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
		
	// 高さを取得する
	public float GetTerrainHeight(Vector3 pos){
		var data = Terrain.terrainData;
		return data.GetInterpolatedHeight (pos.x / data.size.x, pos.z / data.size.z);
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

    public void RedrawAll()
    {
        foreach( var ch in Game.Map.Characters)
        {
            UpdateCharacter(ch);
        }
    }

    public CharacterRenderer GetCharacterRenderer(Character ch)
    {
        CharacterRenderer cr;
        if (!Characters.TryGetValue(ch.Id, out cr))
        {
            cr = Instantiate(CharacterBase);
            cr.name = ch.Name + ":" + ch.Id;
            cr.AtlasName = string.Format("Enemy{0:0000}", ch.AtlasId);
            Characters[ch.Id] = cr;
        }
        return cr;
    }

    public void UpdateCharacter(Character ch)
    {
        var cr = GetCharacterRenderer(ch);
        var pos = new Vector3(ch.Position.X + 0.5f, 0, ch.Position.Y + 0.5f);
        pos.y = GetTerrainHeight(pos);
        cr.transform.localPosition = pos;
    }

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
    // メッセージハンドラ
    //===================================================================

    Character curCharacter;
	Point curPosition;
	Point[] curRange;
	Point[] curAttackRange;
	Point[] curPath;

    public void QMove(Message mes)
    {
        var ch = (Character)mes.Param[0];
        var cr = GetCharacterRenderer(ch);
        Point[] range = null;
        Game.Map.TemporaryOpen(ch.Position, () => { 
			range = Game.Map.PathFinder.FindMoveRange(ch.Position, 3, Game.Map.StepWalkableNow()).ToArray(); 
			List<Point> atks = new List<Point>();
			foreach (var pos in range) {
				foreach( var dir in DirectionUtil.All4 ){
					var p2 = pos + dir.ToPos();
					if( Game.Map[p2].Character != null ){
						atks.Add(p2);
					}
				}
			}
			curAttackRange = atks.Distinct().ToArray();
		});

		cr.State = CharacterRendererState.Active;
        mode = Mode.QMove;
        curCharacter = ch;
		curPosition = ch.Position;
		curRange = range;
        TerrainGridActive.SetActiveGrids(range);

		AttackMarkBase.ReleaseAll ();
		foreach (var pos in curAttackRange) {
			var attackMark = AttackMarkBase.Create ();
			var cr2 = GetCharacterRenderer (Game.Map [pos].Character);
			attackMark.transform.SetParent (null, false);
			attackMark.transform.position = cr2.transform.position + new Vector3(-0.5f,1f,-0.5f);
		}

		OpenAction (act=>{
			switch(act){
			case "cancel":
				curPosition = ch.Position;
				RedrawAll();
				FocusTo(cr.transform.position);
				break;
			case "move":
				cr.State = CharacterRendererState.None;
				Send("AMove", curPath);
				mode = Mode.None;
				CloseAction();
				break;
			case "attack":
				break;
			case "skill":
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

	void Walking(Character ch, Point[] path){
		StartCoroutine (WalkingCoroutin (ch, path));
	}

	IEnumerator WalkingCoroutin(Character ch, Point[] path){
		float n = 0;
		var cr = GetCharacterRenderer(ch);
		var dir = Direction.None;
		while (true)
		{
			bool finish = false;
			n += Time.deltaTime * 5;
			var i = (int)n;
			Vector2 pos;
			if (n >= path.Length - 1)
			{
				finish = true;
				pos = path[path.Length - 1].ToVector2();
			}
			else
			{
				var prevPos = path[i];
				var nextPos = path[i + 1];
				dir = (nextPos - prevPos).ToDir();
				pos = Vector2.Lerp(prevPos.ToVector2(), nextPos.ToVector2(), Mathf.Repeat(n, 1));
			}

			var pos3 = new Vector3(pos.x + 0.5f, 0, pos.y + 0.5f);
			pos3.y = GetTerrainHeight(pos3);
			cr.transform.localPosition = pos3;
			cr.Dir = dir;

			if (finish) break;
			yield return null;
		}

		FocusTo(cr.transform.position);

		Game.Map.TemporaryOpen (curCharacter.Position, () => {
			curPath = Game.Map.PathFinder.FindPath (curCharacter.Position, path[path.Length-1], 3, Game.Map.StepWalkableNow ()).ToArray ();
		});
	}

    IEnumerator Walk(Message mes)
    {
        float n = 0;
        var ch = (Character)mes.Param[0];
        var cr = GetCharacterRenderer(ch);
        var path = (Point[])mes.Param[1];
        var dir = Direction.None;
        while (true)
        {
            bool finish = false;
            n += Time.deltaTime * 5;
            var i = (int)n;
            Vector2 pos;
            if (n >= path.Length - 1)
            {
                finish = true;
                pos = path[path.Length - 1].ToVector2();
            }
            else
            {
                var prevPos = path[i];
                var nextPos = path[i + 1];
                dir = (nextPos - prevPos).ToDir();
                pos = Vector2.Lerp(prevPos.ToVector2(), nextPos.ToVector2(), Mathf.Repeat(n, 1));
            }

            var pos3 = new Vector3(pos.x + 0.5f, 0, pos.y + 0.5f);
            pos3.y = GetTerrainHeight(pos3);
            cr.transform.localPosition = pos3;
            cr.Dir = dir;

            if (finish) break;
            yield return null;
        }
        FocusTo(cr.transform.position);

        RedrawAll();
        Send(null);
    }

	IEnumerator Attack(Message mes)
	{
		var ch = (Character)mes.Param[0];
		var target = (Character)mes.Param [1];
		var dir = (Direction)mes.Param [2];
		var damage = (int)mes.Param [3];

		var cr = GetCharacterRenderer(ch);
		var targetCr = GetCharacterRenderer (target);

		cr.Dir = dir;
		cr.Pose = CharacterRendererPose.Attack;
		targetCr.Pose = CharacterRendererPose.Damage;

		FocusTo(cr.transform.position);

		yield return new WaitForSeconds(1.0f);

		cr.Pose = CharacterRendererPose.Move;
		targetCr.Pose = CharacterRendererPose.Move;

		RedrawAll();
		Send(null);
	}

}
