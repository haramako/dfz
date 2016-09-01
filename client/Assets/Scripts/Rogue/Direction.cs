using System;
using System.Collections;
using System.Collections.Generic;

namespace Rogue {

	public enum Direction {
		None,
		North,
		NorthEast,
		East,
		SouthEast,
		South,
		SouthWest,
		West,
		NorthWest,
	}

	public class DirectionUtil {
		public static readonly Direction[] All = new Direction[]{Direction.North, Direction.NorthEast, Direction.East, Direction.SouthEast, Direction.South, Direction.SouthWest, Direction.West, Direction.NorthWest};
		public static readonly Direction[] All4 = new Direction[]{Direction.North, Direction.East, Direction.South, Direction.West};
	}

	/// DIRECTION関係の拡張メソッド
	public static class DirectionExtension {

		static readonly int[] xByDir = {0, 0, 1, 1, 1, 0, -1, -1, -1 };
		static readonly int[] yByDir = {0, -1, -1, 0, 1, 1, 1, 0, -1 };

		/// <summary>
		/// Pointに変換する(NORTH = (0,-1)とする)
		/// </summary>
		/// <returns>The position.</returns>
		/// <param name="dir">Dir.</param>
		public static Point ToPos (this Direction dir) {
			return new Point (xByDir [(int)dir], yByDir [(int)dir]);
		}
		
		/// <summary>
		/// 逆方向を取得する
		/// </summary>
		/// <param name="dir">Dir.</param>
		public static Direction Inverse (this Direction dir) {
			return dir.Rotate (4);
		}

		//// <summary>
		/// 回転する(nは正の数のときに右回り)
		/// </summary>
		/// <param name="dir">Dir.</param>
		/// <param name="n">N.</param>
		public static Direction Rotate (this Direction dir, int n) {
			if (dir == Direction.None) {
				return Direction.None;
			}else{
				var r = (((int)dir) - 1 + n) % 8;
				if( r < 0 ) r+=8;
				return (Direction)(r+1);
			}
		}
	}
}
