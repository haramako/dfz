using System;
using System.Collections.Generic;
using Rogue;
#if false

namespace Rogue.AStar {

	/// <summary>
	/// 探索MAPDATA
	/// </summary>
	public class GridData {

		/// <summary>移動可能方向bit</summary>
		public byte direction;

		/// <summary>必要コスト</summary>
		public System.UInt16 cost;

		public void Reset() {
			direction = 0x00;
			cost = 0;
		}
	}

	/// <summary>
	/// 探索キュー
	/// </summary>
	internal class PriorityQueue<T> {
		private List<T> InnerList = new List<T>();

		public T this[int index]
		{
			get { return InnerList[index]; }
			set
			{
				InnerList[index] = value;
				Update(index);
			}
		}

		public int Count
		{
			get { return InnerList.Count; }
		}

		private IComparer<T> mComparer;

		public PriorityQueue() {
			mComparer = Comparer<T>.Default;
		}

		public PriorityQueue(IComparer<T> comparer) {
			mComparer = comparer;
		}

		public PriorityQueue(IComparer<T> comparer, int capacity) {
			mComparer = comparer;
			InnerList.Capacity = capacity;
		}

		public void Clear() {
			InnerList.Clear();
		}

		public int Push(T item) {
			int p = InnerList.Count;
			int p2;
			T swapT;

			InnerList.Add(item);
			do {
				if (p == 0) {
					break;
				}
				p2 = (p - 1) / 2;
				if (mComparer.Compare(InnerList[p], InnerList[p2]) < 0) {
					swapT = InnerList[p];
					InnerList[p] = InnerList[p2];
					InnerList[p2] = swapT;
					p = p2;
				} else {
					break;
				}
			} while (true);
			return p;
		}

		public T Pop() {
			int p = 0;
			int p1;
			int p2;
			int pn;
			T result = InnerList[0];
			T swapT;

			InnerList[0] = InnerList[InnerList.Count - 1];
			InnerList.RemoveAt(InnerList.Count - 1);
			do {
				pn = p;
				p1 = 2 * p + 1;
				p2 = 2 * p + 2;
				if (InnerList.Count > p1 && mComparer.Compare(InnerList[p], InnerList[p1]) > 0) {
					p = p1;
				}
				if (InnerList.Count > p2 && mComparer.Compare(InnerList[p], InnerList[p2]) > 0) {
					p = p2;
				}
				if (p == pn) {
					break;
				}
				swapT = InnerList[p];
				InnerList[p] = InnerList[pn];
				InnerList[pn] = swapT;
			} while (true);
			return result;
		}

		public void Update(int i) {
			int p = i;
			int pn;
			int p1;
			int p2;
			T swapT;

			do {
				if (p == 0) {
					break;
				}
				p2 = (p - 1) / 2;
				if (mComparer.Compare(InnerList[p], InnerList[p2]) < 0) {
					swapT = InnerList[p];
					InnerList[p] = InnerList[p2];
					InnerList[p2] = swapT;
					p = p2;
				} else {
					break;
				}
			} while (true);
			if (p < i) {
				return;
			}

			do {
				pn = p;
				p1 = 2 * p + 1;
				p2 = 2 * p + 2;
				if (InnerList.Count > p1 && mComparer.Compare(InnerList[p], InnerList[p1]) > 0) {
					p = p1;
				}
				if (InnerList.Count > p2 && mComparer.Compare(InnerList[p], InnerList[p2]) > 0) {
					p = p2;
				}
				if (p == pn) {
					break;
				}
				swapT = InnerList[p];
				InnerList[p] = InnerList[pn];
				InnerList[pn] = swapT;
			} while (true);
		}

		public T Peek() {
			if (InnerList.Count > 0) {
				return InnerList[0];
			}
			return default(T);
		}
	}

	/// <summary>
	/// 探索ノードデータ
	/// </summary>
	public struct PathFinderNode {

		/// <summary>座標(X)</summary>
		public sbyte X;

		/// <summary>座標(Y)</summary>
		public sbyte Y;

		/// <summary>コスト</summary>
		public short C;

		/// <summary>元座標(X)</summary>
		public sbyte PX;

		/// <summary>元座標(Y)</summary>
		public sbyte PY;

		/// <summary>スコア（小数あり）</summary>
		public float S;

		/// <summary>ヒューリスティック（小数あり）</summary>
		public float H;
	}

	/// <summary>
	/// ノード比較関数
	/// </summary>
	internal class ComparePFNode : IComparer<PathFinderNode> {

		public int Compare(PathFinderNode a, PathFinderNode b) {
			// スコア値で比較
			if (a.S > b.S) {
				return 1;
			} else if (a.S < b.S) {
				return -1;
			}
			return 0;
		}
	}

	/// <summary>
	/// A-star探索アルゴリズム
	/// http://ja.wikipedia.org/wiki/A*
	/// </summary>
	public class PathFinder {

		/// <summary>Openリスト</summary>
		private PriorityQueue<PathFinderNode> mOpen = new PriorityQueue<PathFinderNode>(new ComparePFNode());

		/// <summary>Closeリスト= 戻り値になるので共有すると壊れるので注意！（少メモリ化）</summary>
		private List<PathFinderNode> mClose = new List<PathFinderNode>();

		/// <summary>
		/// テンポラリ
		/// </summary>
		private int i, j, newX, newY, newC, foundInOpenIndex, foundInCloseIndex;

		/// <summary>
		/// Finds the path.
		/// 縦横移動＞ななめ移動 = 敵移動用
		/// </summary>
		/// <returns>The path.</returns>
		/// <param name="grid">Grid.</param>
		/// <param name="start">Start.</param>
		/// <param name="end">End.</param>
		/// <param name="heavyDiagonals">If set to <c>true</c> heavy diagonals.</param>
		/// <param name="searchLimit">Search limit.</param>
		public List<PathFinderNode> FindPath(ref GridData[,] grid, Point start, Point end, bool heavyDiagonals = false, int searchLimit = 1000) {
			mOpen.Clear();
			mClose.Clear();

			PathFinderNode parentNode;
			parentNode.C = 0;
			parentNode.H = 2;
			parentNode.S = 2; // parentNode.C + parentNode.H;
			parentNode.X = (sbyte)start.X;
			parentNode.Y = (sbyte)start.Y;
			parentNode.PX = parentNode.X;
			parentNode.PY = parentNode.Y;
			mOpen.Push(parentNode);

			while (mOpen.Count > 0) {
				parentNode = mOpen.Pop();

				// たどり着いたら成功
				if (parentNode.X == end.X && parentNode.Y == end.Y) {
					mClose.Add(parentNode);

					// 最短経路を登録(不要な経路を削除)
					PathFinderNode fNode = mClose[mClose.Count - 1];
					for (i = mClose.Count - 1; i >= 0; i--) {
						if (fNode.PX == mClose[i].X && fNode.PY == mClose[i].Y || i == mClose.Count - 1) {
							fNode = mClose[i];
						} else {
							mClose.RemoveAt(i);
						}
					}
					// 成功終了
					return mClose;
				}

				// 探索回数がオーバーしたら終了
				if (mClose.Count > searchLimit) {
					return null;
				}
				// ８方向チェック
				for (i = 0; i < 8; i++) {
					// その方向には移動できない
					if ((grid[parentNode.X, parentNode.Y].direction & directionBit[i]) == 0x00) {
						continue;
					}

					// 移動先座標計算
					newX = parentNode.X + direction[i, 0];
					newY = parentNode.Y + direction[i, 1];

					// コスト計算
					newC = parentNode.C + grid[newX, newY].cost;
					if (heavyDiagonals && i > 3) {
						// 斜め移動を控えるモード時は斜めコストを倍にする
						newC += grid[newX, newY].cost;
					}

					// 移動コストに変化が無ければ対象外
					if (newC == parentNode.C) {
						continue;
					}

					// 移動ターゲットにするか調べる
					foundInOpenIndex = -1;
					for (j = 0; j < mOpen.Count; j++) {
						if (mOpen[j].X == newX && mOpen[j].Y == newY) {
							foundInOpenIndex = j;
							break;
						}
					}
					if (foundInOpenIndex != -1 && mOpen[foundInOpenIndex].C <= newC) {
						continue;
					}
					foundInCloseIndex = -1;
					for (j = 0; j < mClose.Count; j++) {
						if (mClose[j].X == newX && mClose[j].Y == newY) {
							foundInCloseIndex = j;
							break;
						}
					}
					if (foundInCloseIndex != -1 && mClose[foundInCloseIndex].C <= newC) {
						continue;
					}

					// open登録
					PathFinderNode newNode;
					newNode.X = (sbyte)newX;
					newNode.Y = (sbyte)newY;
					newNode.C = (short)newC;
					// ヒューリスティック関数は簡易計算（縦横移動を重視）
					newNode.H = 2 * (Math.Abs(newX - end.X) + Math.Abs(newY - end.Y));
					newNode.S = newNode.C + newNode.H;
					newNode.PX = parentNode.X;
					newNode.PY = parentNode.Y;
					mOpen.Push(newNode);
				}
				// close登録
				mClose.Add(parentNode);
			}
			// 発見できなかった
			return null;
		}

		/// <summary>
		/// Finds the path for straightline distance.
		/// プレイヤー移動用
		/// </summary>
		/// <returns>The path for straightline distance.</returns>
		/// <param name="grid">Grid.</param>
		/// <param name="start">Start.</param>
		/// <param name="end">End.</param>
		/// <param name="heavyDiagonals">If set to <c>true</c> heavy diagonals.</param>
		/// <param name="searchLimit">Search limit.</param>
		public List<PathFinderNode> FindPathForStraightlineDistance(ref GridData[,] grid, Point start, Point end, bool heavyDiagonals = false, int searchLimit = 1000) {
			mOpen.Clear();
			mClose.Clear();

			PathFinderNode parentNode;
			parentNode.C = 0;
			parentNode.H = 2;
			parentNode.S = 2; // parentNode.C + parentNode.H;
			parentNode.X = (sbyte)start.X;
			parentNode.Y = (sbyte)start.Y;
			parentNode.PX = parentNode.X;
			parentNode.PY = parentNode.Y;
			mOpen.Push(parentNode);

			// start地点とgoal地点のvector2
			UnityEngine.Vector2 v2now = new UnityEngine.Vector2();
			UnityEngine.Vector2 v2goal = new UnityEngine.Vector2(end.X, end.Y);

			while (mOpen.Count > 0) {
				parentNode = mOpen.Pop();

				// たどり着いたら成功
				if (parentNode.X == end.X && parentNode.Y == end.Y) {
					mClose.Add(parentNode);

					// 最短経路を登録(不要な経路を削除)
					PathFinderNode fNode = mClose[mClose.Count - 1];
					for (i = mClose.Count - 1; i >= 0; i--) {
						if (fNode.PX == mClose[i].X && fNode.PY == mClose[i].Y || i == mClose.Count - 1) {
							fNode = mClose[i];
						} else {
							mClose.RemoveAt(i);
						}
					}
					// 成功終了
					return mClose;
				}

				// 探索回数がオーバーしたら終了
				if (mClose.Count > searchLimit) {
					return null;
				}

				// ８方向チェック
				for (i = 0; i < 8; i++) {
					// その方向には移動できない
					if ((grid[parentNode.X, parentNode.Y].direction & directionBit[i]) == 0x00) {
						continue;
					}

					// 移動先座標計算
					newX = parentNode.X + direction[i, 0];
					newY = parentNode.Y + direction[i, 1];

					// コスト計算
					newC = parentNode.C + grid[newX, newY].cost;
					if (heavyDiagonals && i > 3) {
						// 斜め移動を控えるモード時は斜めコストを倍にする
						newC += grid[newX, newY].cost;
					}

					// 移動コストに変化が無ければ対象外
					if (newC == parentNode.C) {
						continue;
					}

					// 移動ターゲットにするか調べる
					foundInOpenIndex = -1;
					for (j = 0; j < mOpen.Count; j++) {
						if (mOpen[j].X == newX && mOpen[j].Y == newY) {
							foundInOpenIndex = j;
							break;
						}
					}
					if (foundInOpenIndex != -1 && mOpen[foundInOpenIndex].C <= newC) {
						continue;
					}
					foundInCloseIndex = -1;
					for (j = 0; j < mClose.Count; j++) {
						if (mClose[j].X == newX && mClose[j].Y == newY) {
							foundInCloseIndex = j;
							break;
						}
					}
					if (foundInCloseIndex != -1 && mClose[foundInCloseIndex].C <= newC) {
						continue;
					}

					// open登録
					PathFinderNode newNode;
					newNode.X = (sbyte)newX;
					newNode.Y = (sbyte)newY;
					newNode.C = (short)newC;
					// ヒューリスティック関数（直線距離を重視）
					v2now.x = newX; v2now.y = newY;
					newNode.H = (v2now - v2goal).magnitude;
					newNode.S = newNode.C + newNode.H;
					newNode.PX = parentNode.X;
					newNode.PY = parentNode.Y;
					mOpen.Push(newNode);
				}
				// close登録
				mClose.Add(parentNode);
			}

			// 発見できなかった
			return null;
		}
	}

}
#endif
