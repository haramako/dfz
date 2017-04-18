using System.Collections.Generic;
using Game;
using Master;
using System;

namespace Game.FieldLoaders
{
	/// <summary>
	/// テスト用のTMXを直接指定するローダー
	/// </summary>
	public class TestGameTmxLoader : BaseLoader
	{
		public TestGameTmxLoader(FieldLoader fl, Field f): base(fl, f) {}

		public void InitField(TestGame tg)
		{
			var stage = G.FindStageBySymbol (tg.TmxName);

			map_ = new Map (stage.Width, stage.Height);
			for (int x = 0; x < stage.Width; x++)
			{
				for (int y = 0; y < stage.Height; y++)
				{
					var t = stage.Tiles [x + y * stage.Width];
					int t2 = 0;
					switch(t)
					{
						case 1:
							t2 = 2;
							break;
						case 2:
							t2 = 0;
							break;
						case 3:
							t2 = 1;
							break;
						default:
							t2 = t;
							break;
					}
					map_ [x, y].Val = t2;
				}
			}
			f_.Init (map_);

			SetupRoomId ();

			for( int i = 1; i < 10; i++)
			{
				var c = Character.CreateInstance();
				c.Id = i;
				c.Name = "E" + i;
				c.AtlasId = 593;
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
