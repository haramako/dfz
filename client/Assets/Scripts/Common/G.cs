using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Master;

public class User {
}

public class G {

	public static Cfs.Cfs Cfs { get; private set; }
	public static User User { get; private set; }
	public static Dictionary<int,FangTemplate> FangTemplateDict { get; private set; }

	public static void Initialize(Cfs.Cfs cfs){
		Cfs = cfs;
	}

	public static void LoadMaster(){
		FangTemplateDict = PbFile.ReadPbList<FangTemplate,FangTemplate.Builder> (FangTemplate.DefaultInstance, Cfs.GetStream("master-fang_template.pb")).ToDictionary(i=>i.Id);
		//FangTemplates = Cfs.GetBytes ();
	}

	public static void Clear(){
	}
}
