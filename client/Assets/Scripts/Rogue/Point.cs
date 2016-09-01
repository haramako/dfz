using System;

namespace Rogue
{
    /// <summary>
    /// 点を表すクラス.
    /// 
    /// Yは正方向が北、負方向が南、Xは正方向が東、負方向が西となる
    /// </summary>
    [Serializable]
    public struct Point : IEquatable<Point> {
		/// <summary>
		/// X座標
		/// </summary>
		public int X { get; private set; }

		/// <summary>
		/// Y座標
		/// </summary>
		/// <value>The y.</value>
		public int Y { get; private set; }

		public Point(int x, int y){
			X = x;
			Y = y;
		}

		public Point(Point src){
			X = src.X;
			Y = src.Y;
		}

		public static Point operator + (Point a, Point b) {
			return new Point (a.X + b.X, a.Y + b.Y);
		}

		public static Point operator - (Point a, Point b) {
			return new Point (a.X - b.X, a.Y - b.Y);
		}

		public static Point operator + (Point a, Direction d) {
			return a + d.ToPos();
		}

		public static bool operator == (Point a, Point b) {
			return a.Equals(b);
		}
		public static bool operator != (Point a, Point b) {
			return !a.Equals(b);
		}

		public bool Equals (Point a) {
			return X == a.X && Y == a.Y;
		}
		public bool Equals (int _x, int _y) {
			return X == _x && Y == _y;
		}
		public override bool Equals (object obj) {
			if (obj == null || !(obj is Point)) {
				return false;
			}
			Point p = (Point)obj;
			return p.X == X && p.Y == Y;
		}

		public bool IsOrigin {
			get { return (X==0 && Y==0); }
		}

		public override int GetHashCode () {
			return X * 1000 + Y;
		}

		public override string ToString () {
			return "(" + X + ", " + Y + ")";
		}

		static Direction[] pos2dir = new Direction[] {
			Direction.NorthWest, Direction.North, Direction.NorthEast,
			Direction.West, Direction.None, Direction.East,
			Direction.SouthWest, Direction.South, Direction.SouthEast,
		};

		/// <summary>
		/// Directionに変換する
		/// </summary>
		/// <returns>The dir.</returns>
		public Direction ToDir () {
			if (Y >= -1 && Y <= 1 && X >= -1 && X <= 1) {
				return pos2dir [(Y + 1) * 3 + X + 1];
			}
			return Direction.None;
		}

        public UnityEngine.Vector2 ToVector2()
        {
            return new UnityEngine.Vector2(X, Y);
        }

		public float Length () {
			return (float)System.Math.Sqrt(X * X + Y * Y);
		}

		/// <summary>
		/// 縦横のみの移動で測った距離.
		/// 
		/// 斜めへの移動は、２として換算される。
		/// </summary>
		/// <returns>長さ</returns>
		public int GridLength () {
			return System.Math.Abs(X) + System.Math.Abs(Y);
		}
	}

}

