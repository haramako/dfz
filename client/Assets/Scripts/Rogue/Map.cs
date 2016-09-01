using System;
using System.Text;
using System.Collections.Generic;

namespace Rogue
{

	public class CellKind {
		public int Id;
		public int Sprite;
		public bool Walkable;
		public bool Flyable;
		public CellKind(int id, int sprite, bool walkable, bool flyable){
			Id = id;
			Sprite = sprite;
			Walkable = walkable;
			Flyable = flyable;
		}

		static CellKind[] list = new CellKind[]{ 
			new CellKind(0,-1,false, false), 
			new CellKind(1,0, true, true ),
			new CellKind(2,39, false, false ),
			new CellKind(3,21,true,true),
		};

		public static CellKind Find(int id){
			return list[id];
		}

		static CellKind(){
			list = new CellKind[100];
			for (int i = 0; i < list.Length; i++) {
				if (i == 0) {
					list [i] = new CellKind (i, -1, false, false);
				} else if ( i<20 ){
					list [i] = new CellKind (i, i, true, true);
				}else{
					list [i] = new CellKind (i, i, false, false);
				}
			}
		}

	}

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
		public CellStatusType Type { get { return CellStatusType.Fire; } }
	}

	public class CellFreeze : CellStatus {
		public CellStatusType Type { get { return CellStatusType.Freeze; } }
	}
	
	public class Floor {
		public CellKind Kind;
		public List<CellStatus> Statuses = new List<CellStatus>();
		public Character Character;
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
			NullFloor = new Floor(){ Kind= CellKind.Find(0) };
			Data = new Floor[Width * Height];
			for (int i = 0; i < Width * Height; i++) {
				Data [i] = new Floor (){ Kind = CellKind.Find (1) };
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
					sb.AppendFormat ("{0},", GetCell(x, y).Kind.Id);
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

		public delegate bool FloorPredicate(ref Floor cell);

		public bool FloorIsAnywhere(Point p){
			return true;
		}

		public bool FloorIsWalkable(Point p){
			return GetCell (p).Kind.Walkable;
		}

		public bool FloorIsFlyable(Point p){
			return GetCell (p).Kind.Flyable;
		}

		public bool FloorIsWalkableNow(Point p) { 
			var cell = GetCell (p);
			return cell.Kind.Walkable && cell.Character == null;
		}

		public bool FloorIsFlyableNow(Point p) { 
			var cell = GetCell (p);
			return cell.Kind.Flyable && cell.Character == null;
		}

		public bool FloorIsRoom(Point p) { 
			var cell = GetCell (p);
			return cell.Kind.Walkable && cell.Character == null;
		}

		/// <summary>
		/// Step***able系の関数を合成する。.
		/// このゲームの基本である、両隣のマスがあいてる場合のみナナメ移動は可能というロジックを追加する。
		/// </summary>
		/// <returns>合成された PathFinder.Stepable デリゲート</returns>
		/// <param name="predicate">移動可能かどうかを示す、Floor***able系の関数</param>
		/// <param name="slantAnywhere">ナナメの場合に壁にじゃまされない場合はtrue</param>
		public PathFinder.Stepable MakeWalkableFunc(Predicate<Point> predicate, bool slantAnywhere = false) {
			return (from,to)=>{
				if( !slantAnywhere ){
					if( from.X != to.X && from.Y != to.Y ){
                        return 9999;
						// if( !FloorIsFlyable(new Point(from.X, to.Y)) || !FloorIsFlyable(new Point(to.X, from.Y))) return 9999;
					}
				}
				return predicate(to)?1:9999;
			};
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
        public PathFinder.Stepable StepWalkable() { return MakeWalkableFunc(FloorIsWalkable); }
		public PathFinder.Stepable StepFlyable() { return MakeWalkableFunc(FloorIsFlyable); }
		public PathFinder.Stepable StepWalkableNow() { return MakeWalkableFunc(FloorIsWalkableNow); }
		public PathFinder.Stepable StepFlyableNow()  { return MakeWalkableFunc(FloorIsFlyableNow); }
		// 投擲が通過可能かどうか(ナナメ方向の邪魔判定が違う)
		public PathFinder.Stepable StepThrowable() { return MakeWalkableFunc(FloorIsFlyable, true ); }
		public PathFinder.Stepable StepThrowableNow() { return MakeWalkableFunc(FloorIsFlyableNow, true ); }
	}
	
}

