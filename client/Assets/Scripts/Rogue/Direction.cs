using System;
using System.Collections;
using System.Collections.Generic;

namespace Game {

	/// <summary>
	/// ８方向の方向を表す値
	/// </summary>
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

	/// Direction関係の拡張メソッド
	public static class DirectionExtension {

		static readonly int[] xByDir = {0, 0, 1, 1, 1, 0, -1, -1, -1 };
		static readonly int[] yByDir = {0, 1, 1, 0, -1, -1, -1, 0, 1 };

		/// <summary>
		/// Pointに変換する(NORTH = (0,-1)とする)
		/// </summary>
		/// <returns>The position.</returns>
		public static Point ToPos (this Direction dir) {
			return new Point (xByDir [(int)dir], yByDir [(int)dir]);
		}
		
		/// <summary>
		/// 逆方向を取得する
		/// </summary>
		public static Direction Inverse (this Direction dir) {
			return dir.Rotate (4);
		}

		//// <summary>
		/// 回転する(nは正の数のときに右回り)
		/// </summary>
		/// <param name="n">回転方向（性の値の時は右回り、負の値の時は左回り。８で１回転する）</param>
		public static Direction Rotate (this Direction dir, int n) {
			if (dir == Direction.None) {
				return Direction.None;
			}else{
				var r = (((int)dir) - 1 + n) % 8;
				if( r < 0 ) r+=8;
				return (Direction)(r+1);
			}
		}

		#if UNITY_5
		/// <summary>
		/// 回転を表すQuartenionを返す.
		/// 北の方向は、全く回転しないものとなる。
		/// オブジェクトのデフォルトの向きは(0,0,-1)であるべき。
		/// </summary>
		/// <returns>Directionが表すQuaternion.</returns>
		/// <param name="dir">Dir.</param>
		public static UnityEngine.Quaternion ToWorldQuaternion(this Direction dir){
			if (dir == Direction.None) {
				return UnityEngine.Quaternion.identity;
			}else{
				return UnityEngine.Quaternion.AngleAxis(-45 + (int)dir * 45, UnityEngine.Vector3.up);
			}
		}
		#endif
	}
}
