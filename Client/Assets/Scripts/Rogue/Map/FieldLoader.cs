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
				new FieldLoaders.TestTmxLoader (this, f).InitField(tg);
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
					if (map_.FloorIsWalkable (pos))
					{
						return pos;
					}
				}
				throw new Exception ("cannot find position");
			}
		}

		/// <summary>
		/// テスト用のTMXを直接指定するローダー
		/// </summary>
		public class TestTmxLoader : BaseLoader
		{
			public TestTmxLoader(FieldLoader fl, Field f): base(fl, f) {}
			public void InitField(TestGame tg)
			{
			}
		}

		/// <summary>
		/// ランダムマップ用のローダ
		/// </summary>
		public class RandomStageLoader : BaseLoader
		{
			public RandomStageLoader(FieldLoader fl, Field f): base(fl, f) {}

			public void InitField(DungeonStage ds)
			{
				map_ = new Map (64, 64);
				var gen = new MapGenerator.Simple ();
				gen.Generate (map_, new RandomXS ((int)rand_.NextUInt32 ()));

				f_.Init (map_);

				for( int i = 1; i < 50; i++)
				{
					var c = Character.CreateInstance();
					c.Id = i;
					c.Name = "E" + i;
					c.AtlasId = 168;
					c.Hp = 50;
					c.Attack = 30;
					c.Defense = 20;
					c.Speed = 1;
					c.Type = CharacterType.Enemy;
					if (c.Name == "E1")
					{
						f_.SetPlayerCharacter (c);
						c.Hp = 200;
					}
					var pos = FindRandom ();
					map_.AddCharacter (c, pos);
				}

			}
		}
	}

}
