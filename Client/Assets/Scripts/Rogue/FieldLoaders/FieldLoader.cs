using System.Collections.Generic;
using Game;
using Master;
using System;

namespace Game
{

	public class FieldLoader
	{

		public FieldLoader()
		{
		}

		public void LoadStage(Field f, DungeonStage ds)
		{
			switch (ds.Type)
			{
				case StageType.Random:
					new FieldLoaders.RandomStageLoader (this, f).InitField (ds);
					break;
				case StageType.Fixed:
					// TODO: 未実装
					break;
			}
		}

		public void LoadTestGame(Field f, TestGame tg)
		{
			if (tg.DungeonId != 0)
			{
				// TODO: 未実装
			}
			else if (tg.StageId != 0)
			{
				// TODO: 未実装
			}
			else if (!string.IsNullOrEmpty (tg.TmxName))
			{
				new FieldLoaders.TestGameTmxLoader (this, f).InitField(tg);
			}
			else
			{
				throw new Exception ("invalid TestGame");
			}
		}
	}

	namespace FieldLoaders
	{

		public class BaseLoader
		{
			protected FieldLoader fl_;
			protected Field f_;
			protected RandomBase rand_;
			protected Map map_;

			public BaseLoader(FieldLoader fl, Field f)
			{
				fl_ = fl;
				f_ = f;
				rand_ = new RandomXS (12345);
			}

			public Point FindRandom()
			{
				for (int i = 0; i < 1000; i++)
				{
					var x = rand_.RangeInt (0, map_.Width);
					var y = rand_.RangeInt (0, map_.Height);
					var pos = new Point (x, y);
					if (map_.FloorIsWalkableNow (pos))
					{
						return pos;
					}
				}
				throw new Exception ("cannot find position");
			}

			/// <summary>
			/// ルーム番号を自動的に降る
			/// </summary>
			public void SetupRoomId()
			{
				int roomId = 1; // 次に使うルーム番号
				var pathFinder = new PathFinder(map_.Width, map_.Height);
				for( var y = 0; y < map_.Height; y++)
				{
					for( var x = 0; x < map_.Width; x++)
					{
						var cell = map_.GetCell (x, y);
						if (cell.Val == 2 && cell.RoomId == 0)
						{
							// 部屋が続いているマスを取得する
							var path = pathFinder.FindFill (
										   new Point (x, y),
										   true,
										   (p1, p2) => (map_.GetCell (p2).Val == 2) ? 1 : PathFinder.CantMove);
							foreach (var p in path)
							{
								map_.GetCell (p).RoomId = roomId;
							}
							UnityEngine.Debug.Log ("FindRoom " + x + " " + y + " " + path.Count);
							roomId++;
						}
					}
				}
			}
		}

	}

}
