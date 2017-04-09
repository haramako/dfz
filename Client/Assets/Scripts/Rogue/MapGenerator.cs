using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
	public class MapGeneratorBase
	{
		protected Map map;
		protected RandomBase random;

		public virtual void Generate(Map map_, RandomBase random_)
		{
			map = map_;
			random = random_;
		}
	}

}

namespace Game.MapGenerator
{

	/// <summary>
	/// 矩形を表すオブジェクト.
	///
	/// (MinX,MinY)〜(MaxX,MaxY)の範囲、ただし Maxの点は含まない矩形を表す。
	/// </summary>
	public struct Rect
	{
		public int MinX;
		public int MinY;
		public int MaxX;
		public int MaxY;

		public int Width { get { return MaxX - MinX; } }
		public int Height { get { return MaxY - MinY; } }

		public Rect(int minX, int minY, int maxX, int maxY)
		{
			MinX = minX;
			MinY = minY;
			MaxX = maxX;
			MaxY = maxY;
		}

		public void Normalize()
		{
			if (MinX > MaxX)
			{
				var tmp = MaxX;
				MaxX = MinX;
				MinX = tmp;
			}
			if (MinY > MaxY)
			{
				var tmp = MaxY;
				MaxY = MinY;
				MinY = tmp;
			}
		}

		public override string ToString ()
		{
			return string.Format ("({0},{1})-({2},{3})", MinX, MinY, MaxX, MaxY);
		}
	}

	public class Util
	{

		/// <summary>
		/// 複数のPointから経路を表す点の集合を返す.
		///
		/// pointsの最初の点と最後の点も経路に含む。
		/// また、直角に移動しない場合は、InvalidOperationException 例外を投げる。
		/// </summary>
		/// <returns>経路のすべての点</returns>
		/// <param name="points">経路をあらわす複数の点</param>
		public static List<Point> MakePath(params Point[] points)
		{
			if (points.Length <= 0)
			{
				return new List<Point> { };
			}
			if (points.Length == 1)
			{
				return new List<Point> { points [0] };
			}

			List<Point> line = new List<Point> ();
			for (int i = 0; i < points.Length - 1; i++)
			{
				var p1 = points [i];
				var p2 = points [i + 1];
				if (p1.X == p2.X)
				{
					if (p1.Y < p2.Y)
					{
						for (int y = p1.Y; y < p2.Y; y++)
						{
							line.Add (new Point (p1.X, y));
						}
					}
					else
					{
						for (int y = p1.Y; y > p2.Y; y--)
						{
							line.Add (new Point (p1.X, y));
						}
					}
				}
				else if( p1.Y == p2.Y)
				{
					if (p1.X < p2.X)
					{
						for (int x = p1.X; x < p2.X; x++)
						{
							line.Add (new Point (x, p1.Y));
						}
					}
					else
					{
						for (int x = p1.X; x > p2.X; x--)
						{
							line.Add (new Point (x, p1.Y));
						}
					}
				}
				else
				{
					throw new InvalidOperationException("all path must be vertical/horizontal");
				}
			}
			line.Add (points [points.Length - 1]); // 最後の点を追加する

			return line;
		}

		/// <summary>
		/// 通路を描画する.
		///
		/// すでにある通路などと衝突した場合は、falseを返し、なにも描画されない。
		/// </summary>
		/// <returns><c>true</c>通路が描画できればtrueが返す。すでにある通路と衝突した場合はfalseを返す</returns>
		/// <param name="map">描画対象のMap</param>
		/// <param name="val">設定のVal</param>
		/// <param name="roomId">設定するRoomId</param>
		/// <param name="points">経路のPointの配列</param>
		public static bool DrawNode(Map map, int val, int roomId, params Point[] points)
		{
			var path = MakePath (points);

			foreach (var p in path)
			{
				for (int x = -1; x <= 1; x++)
				{
					for (int y = -1; y <= 1; y++)
					{
						if (map [p.X + x, p.Y + y].Val == 2)
						{
							return false;
						}
					}
				}
			}

			foreach (var p in path)
			{
				map [p.X, p.Y].Val = val;
				map [p.X, p.Y].RoomId = roomId;
			}

			return true;
		}

		/// <summary>
		/// 矩形の領域を描画する
		/// </summary>
		/// <param name="map">描画Map</param>
		/// <param name="r">対象の矩形(Maxの辺は含まない)</param>
		/// <param name="val">設定するVal</param>
		/// <param name="roomId">設定するRoomID</param>
		public static void DrawRect(Map map, Rect r, int val, int roomId)
		{
			r.Normalize ();
			for (var x = r.MinX; x < r.MaxX; x++)
			{
				for (var y = r.MinY; y < r.MaxY; y++)
				{
					map [x, y].Val = val;
					map [x, y].RoomId = roomId;
				}
			}
		}
	}

	/// <summary>
	/// 部屋作成用に分割した空間
	/// </summary>
	public class RoomArea
	{
		public int Id; // ID
		public Rect Outer; // 空白を含むエリアの矩形
		public Rect Inner; // 部屋そのものの矩形
		public RoomArea[] Neighbors; // 隣接している部屋

		/// <summary>
		/// 部屋が隣接しているかどうか、および接続している方向を取得する
		/// </summary>
		/// <returns>自分から見て対象のRoomAreaが隣接している方向</returns>
		/// <param name="room">対象のRoomArea</param>
		public Direction NeighborDir(RoomArea room)
		{
			var r = room.Outer;
			if (Outer.MinX == r.MaxX )
			{
				if ((Outer.MaxY > r.MinY && Outer.MinY < r.MaxY))
				{
					return Direction.West;
				}
			}
			else if (Outer.MaxX == r.MinX)
			{
				if ((Outer.MaxY > r.MinY && Outer.MinY < r.MaxY))
				{
					return Direction.East;
				}
			}
			else if (Outer.MinY == r.MaxY)
			{
				if (Outer.MaxX > r.MinX && Outer.MinX < r.MaxX)
				{
					return Direction.South;
				}
			}
			else if (Outer.MaxY == r.MinY)
			{
				if (Outer.MaxX > r.MinX && Outer.MinX < r.MaxX)
				{
					return Direction.North;
				}
			}
			return Direction.None;
		}

		// 文字列に変換する
		public override string ToString ()
		{
			return "RoomArea(id=" + Id + " o=" + Outer + " n=[" + string.Join (",", Neighbors.Select (r => "" + r.Id).ToArray()) + "])";
		}
	}

	/// <summary>
	/// 区間を２分割していって、部屋を配置する
	/// </summary>
	public class Simple : MapGeneratorBase
	{

		public Simple()
		{
		}

		public override void Generate(Map map_, RandomBase random_)
		{
			base.Generate (map_, random_);

			SplitRooms ();
		}

		public void SplitRooms()
		{
			var root = new Rect (0, 0, map.Width, map.Height);

			int i = 1;
			var rooms = SplitRoom (root, 8).Select(r => new RoomArea {Outer = r, Id = i++}).ToArray();

			foreach (var r in rooms)
			{
				r.Neighbors = rooms.Where (r2 => (r.NeighborDir (r2) != Direction.None)).ToArray ();
				if (r.Neighbors.Length > 4)
				{
					r.Neighbors = r.Neighbors.OrderBy(x => random.RangeInt(0, 100)).Take (4).ToArray ();
				}
			}

			foreach (var r in rooms)
			{
				MakeRoom (r);
			}

			foreach (var r in rooms)
			{
				foreach (var r2 in r.Neighbors)
				{
					if (r.Id < r2.Id)
					{
						bool drawn = false;
						for (int j = 0; j < 10; j++)
						{
							if (MakeNode (r, r2))
							{
								drawn = true;
								break;
							}
						}
						if (!drawn)
						{
							Console.WriteLine ("cant drawn");
						}
					}
				}
			}

			foreach (var r in rooms)
			{
				Console.WriteLine (r);
			}
		}

		public void MakeRoom(RoomArea r)
		{
			if (random.RangeInt (0, 10) < 1)
			{
				var w = 1;
				var h = 1;
				var x = random.RangeInt (2, r.Outer.Width - w - 2);
				var y = random.RangeInt (2, r.Outer.Height - h - 2);
				r.Inner = new Rect (r.Outer.MinX + x, r.Outer.MinY + y, r.Outer.MinX + x + w, r.Outer.MinY + y + h);
				Util.DrawRect (map, r.Inner, 1, r.Id);
				Util.DrawRect (map, r.Inner, 1, r.Id);
			}
			else
			{
				var w = random.RangeInt (3, r.Outer.Width - 4) + 1;
				var h = random.RangeInt (3, r.Outer.Height - 4) + 1;
				var x = random.RangeInt (2, r.Outer.Width - w - 2);
				var y = random.RangeInt (2, r.Outer.Height - h - 2);
				r.Inner = new Rect (r.Outer.MinX + x, r.Outer.MinY + y, r.Outer.MinX + x + w, r.Outer.MinY + y + h);
				Util.DrawRect (map, r.Inner, 1, r.Id);
			}
		}

		public bool MakeNode(RoomArea r1, RoomArea r2)
		{
			var dir = r1.NeighborDir (r2);
			var p1 = new Point (random.RangeInt (r1.Inner.MinX, r1.Inner.MaxX), random.RangeInt (r1.Inner.MinY, r1.Inner.MaxY));
			var p2 = new Point (random.RangeInt (r2.Inner.MinX, r2.Inner.MaxX), random.RangeInt (r2.Inner.MinY, r2.Inner.MaxY));

			switch (dir)
			{
				case Direction.East:
					return Util.DrawNode (map, 2, 0,
										  new Point (r1.Inner.MaxX, p1.Y),
										  new Point (r1.Outer.MaxX, p1.Y),
										  new Point (r1.Outer.MaxX, p2.Y),
										  new Point (r2.Inner.MinX - 1, p2.Y));
				case Direction.West:
					return Util.DrawNode (map, 2, 0,
										  new Point (r1.Inner.MinX - 1, p1.Y),
										  new Point (r1.Outer.MinX, p1.Y),
										  new Point (r1.Outer.MinX, p2.Y),
										  new Point (r2.Inner.MaxX, p2.Y));
				case Direction.North:
					return Util.DrawNode (map, 2, 0,
										  new Point (p1.X, r1.Inner.MaxY),
										  new Point (p1.X, r1.Outer.MaxY),
										  new Point (p2.X, r1.Outer.MaxY),
										  new Point (p2.X, r2.Inner.MinY - 1));
				case Direction.South:
					return Util.DrawNode (map, 2, 0,
										  new Point (p1.X, r1.Inner.MinY - 1),
										  new Point (p1.X, r1.Outer.MinY),
										  new Point (p2.X, r1.Outer.MinY),
										  new Point (p2.X, r2.Inner.MaxY));
				default:
					throw new InvalidOperationException ("invalid dir");
			}
		}

		public Rect[] SplitRoom(Rect room, int minSize)
		{
			if ( room.Width > room.Height )
			{
				if (room.Width > minSize * 2)
				{
					var splitX = minSize + random.RangeInt (0, room.Width - minSize * 2);
					var left = new Rect (room.MinX, room.MinY, room.MinX + splitX, room.MaxY);
					var right = new Rect (room.MinX + splitX, room.MinY, room.MaxX, room.MaxY);
					return SplitRoom (left, minSize).Concat (SplitRoom(right, minSize)).ToArray ();
				}
				else
				{
					return new Rect[] { room };
				}
			}
			else
			{
				if (room.Height > minSize * 2)
				{
					var splitY = minSize + random.RangeInt (0, room.Height - minSize * 2);
					var bottom = new Rect (room.MinX, room.MinY, room.MaxX, room.MinY + splitY);
					var top = new Rect (room.MinX, room.MinY + splitY, room.MaxX, room.MaxY);
					return SplitRoom (bottom, minSize).Concat (SplitRoom(top, minSize)).ToArray ();
				}
				else
				{
					return new Rect[] { room };
				}
			}
		}
	}
}
