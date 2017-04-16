using System.Collections.Generic;
using Game;
using Master;
using System;

namespace Game.FieldLoaders
{
	/// <summary>
	/// テスト用のTMXを直接指定するローダー
	/// </summary>
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
