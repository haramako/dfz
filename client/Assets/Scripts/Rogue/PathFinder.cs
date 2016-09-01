using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Rogue {

	/// <summary>
	/// 経路選択エンジン
	/// </summary>
	public class PathFinder {
		/// スタックの最大サイズ
		private static readonly int MaxStack = 1000;

		/// 各位置の残り移動力
		private int[] rest;
		/// 各の歩いてきた方向（南から北に歩いてきた場合は、北が格納されるので注意！）
		private Direction[] fromDir;

		/// 経路選択で一時的に使用するスタック
		private Point[] stack = new Point[MaxStack];
		/// stackの位置（未使用領域のの最小のindex）
		private int stackPos = 0;

		/// ２点間の移動が可能かを返すデリゲート
		public delegate int Stepable(Point _from, Point _to);

		public int Width { get; private set; }
		public int Height { get; private set; }

		public PathFinder(int width, int height){
			Width = width;
			Height = height;
			rest = new int[Width * Height];
			fromDir = new Direction[Width * Height];
		}

		/// <summary>
		/// 経路情報のアップデートを行う
		/// </summary>
		/// <param name="_from">始点.</param>
		/// <param name="movePoint">移動力</param>
		/// <param name="movePoint">最後の地点は移動可能とする</param>
		/// <param name="isWalkable">移動可能かを返すdelegate</param>
		void updateMoveCostTable( Point _from, int movePoint, Stepable isWalkable ){
			// テンポラリ領域を初期化する
			for (int i = 0; i < Width * Height; i++) {
				rest [i] = -1;
				fromDir [i] = Direction.None;
			}

			// スタックに最初の位置を入れる
			stackPos = 0;
			rest[_from.Y*Width+_from.X] = movePoint;
			stack[stackPos++] = _from;

			// A* アルゴリズムで検索する
			while( stackPos > 0 ){
				Point cur = stack[--stackPos];
				int curRest = rest[cur.Y*Width+cur.X];
				foreach( Direction dir in DirectionUtil.All ){
					Point dirPos = dir.ToPos ();
					Point moveTo = new Point(cur.X + dirPos.X, cur.Y + dirPos.Y);
					var cost = isWalkable (cur, moveTo);
					if( cost < 9999 && rest[moveTo.Y*Width+moveTo.X] <= curRest - cost ){
						// 斜めより縦横を優先
						if ((rest [moveTo.Y*Width+moveTo.X] == curRest - cost) &&
						    (fromDir [moveTo.Y*Width+moveTo.X].ToPos ().Length () == 1.0)) continue;
						rest[moveTo.Y*Width+moveTo.X] = curRest - cost;
						fromDir [moveTo.Y*Width+moveTo.X] = dir;
						stack[stackPos++] = moveTo;
					}
				}
			}
		}

        /// <summary>
        /// 移動範囲取得する
        /// </summary>
        public List<Point> FindMoveRange(Point _from, int movePoint, Stepable isWalkable)
        {
            updateMoveCostTable(_from, movePoint, isWalkable);

            var result = new List<Point>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (rest[y * Width + x] >= 0)
                    {
                        result.Add(new Point(x, y));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 始点と終点を指定して経路を選択する
        /// </summary>
        public List<Point> FindPath( Point _from, Point _to, int movePoint, Stepable isWalkable ){
			updateMoveCostTable(_from, movePoint, isWalkable);

			// restから導かれる複数の経路から一つの経路にしぼる
			int curRest = rest[_to.Y*Width+_to.X];
			if( curRest >= 0 ){
				var result = new List<Point>();
				Point cur = _to;

                while ( !(cur == _from) ){
                    result.Add(new Point(cur));
                    cur -= fromDir [cur.Y*Width+cur.X].ToPos ();
				}
                result.Add(new Point(cur)); // 最後に自分を含む

                result.Reverse ();
				return result;
			}
			return null;
		}

		/// <summary>
		/// 位置と方向を指定して、直線状の移動可能な終点を返す
		/// </summary>
		/// <returns>始点から終点までの位置のリスト（開始位置は含まず、そこから１進んだ場所から）</returns>
		/// <param name="startPos">開始位置.</param>
		/// <param name="startDir">開始方向.</param>
		/// <param name="startDir">終点を含むかどうか.</param>
		/// <param name="isWalkable">移動可能かどうかを表すdelegate</param>
		public List<Point> FindStraight( Point pos, Direction dir, int distance, bool includeStop, Stepable isWalkable ){
			var result = new List<Point>();
			Point dirPos = dir.ToPos ();
			for( int i=0; i<distance; i++){
				var nextPos = pos + dirPos;
				var cost = isWalkable (pos, nextPos);
				if( cost > 1 ){
					if( includeStop ) result.Add (nextPos);
					return result;
				}
				result.Add (new Point(nextPos));
				pos = nextPos;
			}
			return result;
		}

		/// <summary>
		/// 指定位置からＮマス以内のマスを返す
		/// </summary>
		/// <returns>対象のマスのリスト</returns>
		/// <param name="pos">中心位置.</param>
		/// <param name="distance">距離.</param>
		/// <param name="includeCenter">中心を含むかどうか.</param>
		/// <param name="isWalkable">移動可能かどうかを表すdelegate</param>
		public List<Point> FindAround(Point pos, int distance, bool includeCenter, Predicate<Point> isWalkable) {
			var result = new List<Point>();
			for( int x=-distance; x<=distance; x++){
				for( int y=-distance; y<=distance; y++){
					if( !includeCenter && x == 0 && y == 0 ) continue;
					var p = new Point(pos.X+x, pos.Y+y);
					if( isWalkable(p) ) result.Add ( p );
				}
			}
			return result;
		}
			
		/// <summary>
		/// 連続領域を返す
		/// </summary>
		/// <returns>対象のマスのリスト</returns>
		/// <param name="pos">中心位置.</param>
		/// <param name="includeCenter">中心を含むかどうか.</param>
		/// <param name="isWalkable">連続領域かどうかdelegate</param>
		public List<Point> FindFill( Point pos, bool includeCenter, Stepable isWalkable ){
			updateMoveCostTable(pos, 40, isWalkable);
			var result = new List<Point>();

			// テンポラリ領域を集計する
			for(int x=0; x<Width; x++){
				for(int y=0; y<Height; y++){

					if (!includeCenter && pos.X == x && pos.Y == y) continue; // includeCenterがfalseなら中央は含まない
					if( rest[y*Width+x] >= 0 ) result.Add (new Point(x,y));
				}
			}

			return result;
		}

		/// <summary>
		/// 指定位置から一番近いマスを探す
		/// 同じ距離のものが複数あった場合は、ドラン君からの位置が遠いものを選択する
		/// </summary>
		/// <returns>対象のマスのリスト</returns>
		/// <param name="pos">中心位置.</param>
		/// <param name="distance">距離.</param>
		/// <param name="includeCenter">中心を含むかどうか.</param>
		/// <param name="walkFlyType">Walk fly type.</param>
		/// <param name="isWalkable">移動可能かどうかを表すdelegate</param>
		public Point FindNearest(Point pos, int limit, bool includeCenter, Predicate<Point> isWalkable) {
			if (includeCenter && isWalkable(pos)) return pos;
			Point playerPos = new Point (0, 0);
			var posList = FindAround(pos, limit, includeCenter, isWalkable)
				.OrderBy(p => (p - pos).Length() - (p - playerPos).Length()/1000.0f);
			if (posList.Count() > 0) {
				return posList.First();
			} else {
				return new Point(0,0);
			}
		}

		public string Display(){
			var sb = new StringBuilder ();
			for (int y = 0; y < Height; y++) {
				for (int x = 0; x < Width; x++) {
					sb.AppendFormat ("{0},", rest [y * Width + x]);
				}
				sb.AppendLine ();
			}
			return sb.ToString ();
		}
	}
}
