using System;

namespace dfz
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Hello World!");
			var sv = new SLua.LuaSvr();
			sv.init(null, () => { });
			sv.luaState.doString("print(1);");
		}
	}
}
