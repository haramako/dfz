using System;
using System.Text;
using System.Collections.Generic;

namespace Game
{

	public enum CellStatusType {
		None,
		Fire,
		Freeze,
		Grow,
		Lightning,
	}

	public abstract class CellStatus {
		public virtual CellStatusType Type { get { return CellStatusType.None; } }
		public virtual CellStatusType Turn { get; private set; }
		public virtual void OnEnterCharacter(){}
		public virtual void OnExitCharacter(){}
		public virtual void OnTurnStart(){}
		public virtual void OnTurnEnd(){}
	}

	public class CellFire : CellStatus {
		public override CellStatusType Type { get { return CellStatusType.Fire; } }
	}

	public class CellFreeze : CellStatus {
		public override CellStatusType Type { get { return CellStatusType.Freeze; } }
	}
	
	public class Floor {
		public int Val;
		public List<CellStatus> Statuses = new List<CellStatus>();
		public Character Character;

		public bool Walkable {
			get {
				return Val == 1;
			}
		}

		public bool Flyable {
			get {
				return Val == 1;
			}
		}

	}

	public class Map
	{
		public int Width { get; private set;}
		public int Height { get; private set;}
		public PathFinder PathFinder { get; private set; }
		public List<Character> Characters = new List<Character>();

		Floor NullFloor;
		Floor[] Data;

		public Map (int width, int height)
		{
			Width = width;
			Height = height;
			NullFloor = new Floor (){ Val = 0 };
			Data = new Floor[Width * Height];
			for (int i = 0; i < Width * Height; i++) {
				Data [i] = new Floor (){ Val = 0 };
			}
			PathFinder = new PathFinder (Width, Height);
		}

		public Floor this [int x, int y] { get { return GetCell(x,y); } }
		public Floor this [Point p] { get { return GetCell(p); } }

		public Floor GetCell(Point pos){
			return GetCell(pos.X,pos.Y);
		}

		public Floor GetCell(int x, int y){
			if (x < 0 || x >= Width || y < 0 || y >= Height) {
				return NullFloor;
			} else {
				return Data [y * Width + x];
			}
		}

		public string Display(){
			var sb = new StringBuilder ();
			for (int y = 0; y < Height; y++) {
				for (int x = 0; x < Width; x++) {
					sb.AppendFormat ("{0},", GetCell(x, y).Val);
				}
				sb.AppendLine ();
			}
			return sb.ToString ();
		}

		//=======================================================
		// キャラクターの追加と削除
		//=======================================================

		public void AddCharacter(Character c, Point pos){
            if (this[pos].Character != null) throw new Exception("cannot add character");
            c.Position = pos;
            Characters.Add (c);
			this [c.Position].Character = c;
		}

		public void RemoveCharacter(Character c){
            Characters.Remove (c);
            if (this[c.Position].Character != c) throw new Exception("cannot remove character");
            this[c.Position].Character = null;
		}

		public void MoveCharacter(Character c, Point pos){
			if (this [c.Position].Character != c) throw new Exception ("cannot remove character");
			this [c.Position].Character = null;
			c.Position = pos;
			this [c.Position].Character = c;
		}




		//=======================================================================================
		// 経路選択関係のコード
		// PathFinderの引数として使われることを想定している
		//
		// Floor***able() は、対象のマップセルが、***可能かどうかを表す
		//
		// Step***able()は、対象の（隣接する)fromとtoの２セルの間が***可能かどうかを表す。
		// これは、ナナメ移動が移動先のセルだけではなく、両隣のセルも移動可能でないとできないため、その判定に必要となる。
		//
		// 使用例：
		// 
		// MapManager map;
		// List<Point> path = map.PathFinder( new Point(10,10), new Point(20,20), map.StepWalkable() );
		//    => path に 通常移動ユニットの(10,10)から(20,20)への経路情報が入る
		//
		// List<Point> path = map.PathFinder( new Point(10,10), new Point(20,20), map.StepFlyable() );
		//    => path に 飛行ユニットの(10,10)から(20,20)への経路情報が入る
		//
		//=======================================================================================

		public bool FloorIsAnywhere(Point p){
			return GetCell(p) != NullFloor;
		}

		public bool FloorIsWalkable(Point p){
			return GetCell (p).Walkable;
		}

		public bool FloorIsFlyable(Point p){
			return GetCell (p).Flyable;
		}

		public bool FloorIsWalkableNow(Point p) { 
			var cell = GetCell (p);
			return cell.Walkable && cell.Character == null;
		}

		public bool FloorIsFlyableNow(Point p) { 
			var cell = GetCell (p);
			return cell.Flyable && cell.Character == null;
		}

		public bool FloorIsRoom(Point p) { 
			var cell = GetCell (p);
			return cell.Walkable && cell.Character == null;
		}

		/// <summary>
		/// Step***able系の関数を合成する。.
		/// このゲームの基本である、両隣のマスがあいてる場合のみナナメ移動は可能というロジックを追加する。
		/// </summary>
		/// <returns>合成された PathFinder.Stepable デリゲート</returns>
		/// <param name="predicate">移動可能かどうかを示す、Floor***able系の関数</param>
		/// <param name="slantAnywhere">ナナメの場合に壁にじゃまされない場合はtrue</param>
		public PathFinder.Stepable MakeWalkableFunc(Predicate<Point> predicate, bool slantAnywhere) {
			if (slantAnywhere) {
				return (from, to) => {
					if (predicate (to)) {
						// 移動可能
						return 1;
					} else {
						// 移動不可
						return 9999;
					}
				};
			} else {
				return (from, to) => {
					if( from.X != to.X && from.Y != to.Y ){
						// 斜め移動の場合
						if( predicate (to) && FloorIsFlyable(new Point(from.X, to.Y)) && FloorIsFlyable(new Point(to.X, from.Y)) ){
							return 1;
						}else{
							return 9999;
						}
					}
					if (predicate (to)) {
						// 移動可能
						return 1;
					} else {
						// 移動不可
						return 9999;
					}
				};
			}
		}

        public void TemporaryOpen(Point pos, Action action)
        {
            var backup = this[pos].Character;
			this [pos].Character = null;
            try
            {
                action();
            }
            finally
            {
                this[pos].Character = backup;
            }
        }

        // 隣接する２マスの経路が、移動可能かどうかを表すdelegateを返す( 挙動は、対応するFloor***able()関数を参照 )
		public PathFinder.Stepable StepWalkable(bool slantAnywhere = false) { return MakeWalkableFunc(FloorIsWalkable, slantAnywhere); }
		public PathFinder.Stepable StepFlyable(bool slantAnywhere = false) { return MakeWalkableFunc(FloorIsFlyable, slantAnywhere); }
		public PathFinder.Stepable StepWalkableNow(bool slantAnywhere = false) { return MakeWalkableFunc(FloorIsWalkableNow, slantAnywhere); }
		public PathFinder.Stepable StepFlyableNow(bool slantAnywhere = false)  { return MakeWalkableFunc(FloorIsFlyableNow, slantAnywhere); }
		public PathFinder.Stepable StepAnywhere(bool slantAnywhere = false)  { return (f, t) => (FloorIsWalkable (t) ? 1 : (FloorIsAnywhere(t)? 2:9999)); }
	}
	
}

