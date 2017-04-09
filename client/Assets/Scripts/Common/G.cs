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
		//FangTemplateDict = PbFile.ReadPbList<FangTemplate,FangTemplate.Builder> (FangTemplate.DefaultInstance, Cfs.GetStream("master-fang_template.pb")).ToDictionary(i=>i.Id);
		//FangTemplates = Cfs.GetBytes ();
	}

	public static void Clear()
	{
	}
}
