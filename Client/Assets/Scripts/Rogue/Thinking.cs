using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Game{
	public class Thinking {

		Field f;

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

	}
}
