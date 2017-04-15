using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Master;
using Google.ProtocolBuffers;

public class User
{
}

public class G
{

	public static Cfs.Cfs Cfs { get; private set; }
	public static User User { get; private set; }
	//public static Dictionary<int,FangTemplate> FangTemplateDict { get; private set; }

	public static Dictionary<int, Stage> Stages = new Dictionary<int, Stage> ();
	public static Dictionary<int, Skill> Skills = new Dictionary<int, Skill> ();
	public static Dictionary<int, SkillEffect> SkillEffects = new Dictionary<int, SkillEffect> ();

	public static Dictionary<int, TestGame> TestGames = new Dictionary<int, TestGame> ();

	public static void Initialize(Cfs.Cfs cfs)
	{
		Cfs = cfs;
	}

	public static List<T> LoadPbFiles<T>(Func<T> constructor, string type) where T : Message
	{
		List<T> list = new List<T>();
		byte[] buf = new byte[1024 * 1024];
		var postfix = type + ".pb";
		foreach( var file in Cfs.bucket.Files.Values.Where(f => f.Filename.EndsWith(postfix) ))
		{
			int size;
			using (var s = Cfs.GetStream (file.Filename))
			{
				size = s.Read (buf, 0, buf.Length);
			}
			list.AddRange( PbFile.ReadPbList(constructor, buf, 0, size).ToList() );
		}
		if (list.Count <= 0)
		{
			throw new Exception ("no items found for " + type);
		}
		Debug.Log ("loaded " + type + " "  + list.Count + " items");
		return list;
	}

	public static void LoadMaster()
	{
		Stages = LoadPbFiles<Stage>(Stage.CreateInstance, "Stage").ToDictionary(i => i.Id);
		Skills = LoadPbFiles<Skill>(Skill.CreateInstance, "Skill").ToDictionary(i => i.Id);
		TestGames = LoadPbFiles<TestGame>(TestGame.CreateInstance, "TestGame").ToDictionary(i => i.Id);
		SkillEffects = LoadPbFiles<SkillEffect>(SkillEffect.CreateInstance, "SkillEffect").ToDictionary(i => i.Id);
	}

	public static Stage FindStageBySymbol(string sym)
	{
		return Stages.Values.FirstOrDefault (s => s.Symbol == sym);
	}

	public static Skill FindSkillBySymbol(string sym)
	{
		return Skills.Values.FirstOrDefault (s => s.Symbol == sym);
	}

	public static SkillEffect FindSkillEffectBySymbol(string sym)
	{
		return SkillEffects.Values.FirstOrDefault (s => s.Symbol == sym);
	}

	public static void Clear()
	{
	}
}
