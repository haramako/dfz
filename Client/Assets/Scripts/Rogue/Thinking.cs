using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Game{
	
	public enum ActionResultType {
		None,
		Move, // To, Dir
		Attack, // Target, To, Dir
		Skill,
	}

	public class ActionResult {
		public ActionResultType Type;
		public Character Target;
		public Point To;
		public Direction Dir;
	}

	public class Thinking {

		Field f;

		public class Hoge {
			public int X;
		}

		public Thinking(Field field){
			f = field;
		}

		public Character FindTarget(Character c){
			if (c.IsPlayer) {
				return null;
			} else {
				return f.Player;
			}
		}

		public struct MoveResult{
			public bool IsMove;
			public Point MoveTo;
		}

		/// <summary>
		/// 移動先を取得する
		/// </summary>
		/// <returns>移動祭の情報</returns>
		/// <param name="c">対象のキャラクター</param>
		public MoveResult ThinkMove(Character c){
			var target = FindTarget (c);
			if (target == null) {
				return new MoveResult{};
			}

			var path = f.Map.PathFinder.FindPath (c.Position, target.Position, f.Map.StepWalkable (), 4);
			if (path == null) {
				return new MoveResult{};
			}

			return new MoveResult{ IsMove = true, MoveTo = path [0] };
		}

		public ActionResult ThinkAttack(Character c){
			var target = FindTarget (c);

			// ターゲットに攻撃できる？
			if ((c.Position - target.Position).GridLength() <= 1) {
				if (f.Map.StepFlyable () (c.Position, target.Position) != PathFinder.CantMove) {
					return new ActionResult {
						Type = ActionResultType.Attack,
						Target = target,
						To = target.Position,
						Dir = (target.Position-c.Position).ToDir(),
					};
				}
			}

			// やることなし
			return new ActionResult{ Type = ActionResultType.None };
		}


		public List<Point> ThinkAutoMove(Character c){
			var path = new List<Point> ();
			var pos = c.Position;
			var prevDir = c.Dir;
			while (true) {
				var n = 0;
				var foundDir = Direction.None;
				foreach (var dir in DirectionUtil.All) {
					if (dir == prevDir.Inverse ()) {
						continue;
					}
					if (f.Map.StepWalkable()(pos,pos + dir) != PathFinder.CantMove) {
						if (f.Map [pos+dir].RoomId != 0) {
							continue;
						}
						if (n >= 1) {
							foundDir = Direction.None;
							break;
						} else {
							foundDir = dir;
							n++;
						}
					}
				}
				if (foundDir != Direction.None) {
					pos = pos + foundDir;
					prevDir = foundDir;
					path.Add (pos);
				} else {
					break;
				}
			}

			return path;
		}

		public int NewActionResult(){
			return 0;
			//return new ActionResult();
		}

		public ActionResult ThinkLua(Character c, SLua.LuaFunction func){
			var result = func.call (this, c);
			if (result == null) {
				return new ActionResult ();
			} else {
				return (ActionResult)result;
			}
		}
	}
}
