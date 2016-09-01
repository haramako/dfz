using System;
using System.Collections.Generic;
using System.Linq;

namespace Rogue {

	public class ScopeParam {
		public Map Map;
		public Character From;
		public Point FromPoint;
		public Direction Dir;
	}

	public class ScopeResult {
		public ScopeParam Param;
		public List<Point> Path;
		public List<Point> Targets;
	}

	/// <summary>
	/// 特殊効果の効果範囲を表す
	/// TODO: only/without, limit, num などを実装する？
	/// </summary>
	public class SpecialScope {
		public enum ScopeTargetType {
			Both,
			Ours,
			Others,
		}

		public enum ScopeType {
			/// 自分が対象
			Self,
			/// 直線状が対象
			Straight,
			/// (OBSOLETED)貫通する直線状自分が対象
			Around,
		}

		public ScopeTargetType TargetType { get; private set; }
		public ScopeType Type { get; private set; }
		public int Range { get; private set; }

		public SpecialScope(){
		}

		public ScopeResult GetRange( ScopeParam p ){
			var res = new ScopeResult();
			res.Param = p;

			// 当たり判定リストを作成
			switch (Type) {
			case ScopeType.Self:
				res.Path = new List<Point>{ p.FromPoint };
				break;
			case ScopeType.Straight:
				{
					var tmpPath = p.Map.PathFinder.FindStraight (p.FromPoint, p.Dir, Range, true, p.Map.StepFlyable());
					res.Path = tmpPath;
					//StraightPaths.Add (new POINT2 (from, tmpPath [tmpPath.Count - 1]));
				}
				break;
			case ScopeType.Around:
				res.Path = p.Map.PathFinder.FindAround (p.FromPoint, Range, false, p.Map.FloorIsFlyable);
				break;
			default:
				throw new Exception ("invalid scope " + Type);
			}

			// 当たり判定リストに存在するtargetを抽出
			if (Type == ScopeType.Self ) {
				// Selfなら問答なし
				res.Targets = res.Path;
			} else {
				res.Targets = res.Path.Where ((pos) => {
					var cell = p.Map[pos];
					if( cell.Character == null ){
						return false;
					}else{
						return true;
					}
				}).ToList ();
			}

			return res;
		}
	}
}

	
