using System;
using Game;
using Master;
using System.Collections.Generic;

namespace Game.FieldLoaders
{

	/// <summary>
	/// 固定マップ用のローダ
	/// </summary>
	public class FixedStageLoader : BaseLoader
	{
		public FixedStageLoader(FieldLoader fl, Field f): base(fl, f) {}

		public void InitField(FieldLoader fl, Field f, DungeonStage ds)
		{
			var stage = G.FindStageBySymbol(ds.StageName);
			var map = new Map (stage.Width, stage.Height);
			for (int x = 0; x < stage.Width; x++)
			{
				for (int y = 0; y < stage.Height; y++)
				{
					map [x, y].Val = stage.Tiles [x + y * stage.Width];
				}
			}
			f.Init (map);

			int i = 0;
			foreach (var sc in stage.Characters)
			{
				if (!(sc.Name.StartsWith ("E") || sc.Name.StartsWith ("P")))
				{
					continue;
				}
				var c = Character.CreateInstance ();
				c.Id = i++;
				c.Hp = 20;
				c.Name = sc.Name;
				c.AtlasId = sc.Char;
				c.Speed = sc.Speed;
				c.Type = CharacterType.Enemy;
				if (sc.Name == "P1")
				{
					f.SetPlayerCharacter (c);
					c.Hp = 100;
					c.Speed = 5 + int.Parse (sc.Name.Substring (1));
				}
				else if (sc.Name.StartsWith ("E"))
				{
					c.Speed = 10 + int.Parse (sc.Name.Substring (1));
				}
				else
				{
					c.Speed = 5 + int.Parse (sc.Name.Substring (1));
				}
				map.AddCharacter (c, new Point (sc.X,	sc.Y));
			}

		}
	}

}
