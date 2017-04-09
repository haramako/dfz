using UnityEngine;
using System;
using LuaInterface;
using SLua;
using System.Collections.Generic;
public class Lua_Game_Field : LuaObject {
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int constructor(IntPtr l) {
		try {
			Game.Field o;
			o=new Game.Field();
			pushValue(l,true);
			pushValue(l,o);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int StartThread(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			self.StartThread();
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int Shutdown(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			self.Shutdown();
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int Request(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			GameLog.IRequest a1;
			checkType(l,2,out a1);
			var ret=self.Request(a1);
			pushValue(l,true);
			pushValue(l,ret);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int RequestAsync(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			GameLog.IRequest a1;
			checkType(l,2,out a1);
			self.RequestAsync(a1);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int ReadCommandsAsync(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			var ret=self.ReadCommandsAsync();
			pushValue(l,true);
			pushValue(l,ret);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int Send(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			GameLog.ICommand a1;
			checkType(l,2,out a1);
			self.Send(a1);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int SendAndWait(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			GameLog.ICommand a1;
			checkType(l,2,out a1);
			self.SendAndWait(a1);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int WaitForRequest(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			Game.WaitingType a1;
			checkEnum(l,2,out a1);
			var ret=self.WaitForRequest(a1);
			pushValue(l,true);
			pushValue(l,ret);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int WaitForAck(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			self.WaitForAck();
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int log(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			System.Object a1;
			checkType(l,2,out a1);
			self.log(a1);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int logException(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			System.Exception a1;
			checkType(l,2,out a1);
			self.logException(a1);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int FindCharacter(IntPtr l) {
		try {
			int argc = LuaDLL.lua_gettop(l);
			if(matchType(l,argc,2,typeof(string))){
				Game.Field self=(Game.Field)checkSelf(l);
				System.String a1;
				checkType(l,2,out a1);
				var ret=self.FindCharacter(a1);
				pushValue(l,true);
				pushValue(l,ret);
				return 2;
			}
			else if(matchType(l,argc,2,typeof(int))){
				Game.Field self=(Game.Field)checkSelf(l);
				System.Int32 a1;
				checkType(l,2,out a1);
				var ret=self.FindCharacter(a1);
				pushValue(l,true);
				pushValue(l,ret);
				return 2;
			}
			pushValue(l,false);
			LuaDLL.lua_pushstring(l,"No matched override function to call");
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int SetPlayerCharacter(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			Game.Character a1;
			checkType(l,2,out a1);
			self.SetPlayerCharacter(a1);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int ShowMessage(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			System.String a1;
			checkType(l,2,out a1);
			System.Object[] a2;
			checkParams(l,3,out a2);
			self.ShowMessage(a1,a2);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int Display(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			var ret=self.Display();
			pushValue(l,true);
			pushValue(l,ret);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int InitLua(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			System.String a1;
			checkType(l,2,out a1);
			self.InitLua(a1);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int Init(IntPtr l) {
		try {
			int argc = LuaDLL.lua_gettop(l);
			if(matchType(l,argc,2,typeof(Master.Stage))){
				Game.Field self=(Game.Field)checkSelf(l);
				Master.Stage a1;
				checkType(l,2,out a1);
				self.Init(a1);
				pushValue(l,true);
				return 1;
			}
			else if(matchType(l,argc,2,typeof(Game.Map))){
				Game.Field self=(Game.Field)checkSelf(l);
				Game.Map a1;
				checkType(l,2,out a1);
				self.Init(a1);
				pushValue(l,true);
				return 1;
			}
			pushValue(l,false);
			LuaDLL.lua_pushstring(l,"No matched override function to call");
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int InitRandom(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			Master.Stage a1;
			checkType(l,2,out a1);
			self.InitRandom(a1);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int Process(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			self.Process();
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int DoTurnStart(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			self.DoTurnStart();
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int DoThink(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			self.DoThink();
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int DoMove(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			self.DoMove();
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int DoPlay(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			self.DoPlay();
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int DoTurnEnd(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			self.DoTurnEnd();
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int WalkCharacter(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			Game.Character a1;
			checkType(l,2,out a1);
			Game.Point a2;
			checkValueType(l,3,out a2);
			self.WalkCharacter(a1,a2);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int UseSkill(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			Game.Character a1;
			checkType(l,2,out a1);
			Game.Direction a2;
			checkEnum(l,3,out a2);
			Master.SpecialScope a3;
			checkType(l,4,out a3);
			Game.Special a4;
			checkType(l,5,out a4);
			self.UseSkill(a1,a2,a3,a4);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int AttackCharacter(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			Game.Character a1;
			checkType(l,2,out a1);
			Game.Direction a2;
			checkEnum(l,3,out a2);
			self.AttackCharacter(a1,a2);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int ExecuteSpecial(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			Game.Special a1;
			checkType(l,2,out a1);
			Game.SpecialParam a2;
			checkType(l,3,out a2);
			self.ExecuteSpecial(a1,a2);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int AddDamage(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			Game.Character a1;
			checkType(l,2,out a1);
			GameLog.DamageInfo a2;
			checkType(l,3,out a2);
			self.AddDamage(a1,a2);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int KillCharacter(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			Game.Character a1;
			checkType(l,2,out a1);
			self.KillCharacter(a1);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int UpdateViewport(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			self.UpdateViewport();
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int get_RequestTimeoutMillis(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			pushValue(l,true);
			pushValue(l,self.RequestTimeoutMillis);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int set_RequestTimeoutMillis(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			System.Int32 v;
			checkType(l,2,out v);
			self.RequestTimeoutMillis=v;
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int get_NoLog(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			pushValue(l,true);
			pushValue(l,self.NoLog);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int set_NoLog(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			System.Boolean v;
			checkType(l,2,out v);
			self.NoLog=v;
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int get_NoUnity(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			pushValue(l,true);
			pushValue(l,self.NoUnity);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int set_NoUnity(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			System.Boolean v;
			checkType(l,2,out v);
			self.NoUnity=v;
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int get_path_(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			pushValue(l,true);
			pushValue(l,self.path_);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int set_path_(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			System.Collections.Generic.List<Game.Point> v;
			checkType(l,2,out v);
			self.path_=v;
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int get_WaitingType(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			pushValue(l,true);
			pushEnum(l,(int)self.WaitingType);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int get_Map(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			pushValue(l,true);
			pushValue(l,self.Map);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int get_State(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			pushValue(l,true);
			pushEnum(l,(int)self.State);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int get_TurnNum(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			pushValue(l,true);
			pushValue(l,self.TurnNum);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int get_Thinking(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			pushValue(l,true);
			pushValue(l,self.Thinking);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int get_Player(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			pushValue(l,true);
			pushValue(l,self.Player);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int get_L(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			pushValue(l,true);
			pushValue(l,self.L);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int get_ShutdownError(IntPtr l) {
		try {
			Game.Field self=(Game.Field)checkSelf(l);
			pushValue(l,true);
			pushValue(l,self.ShutdownError);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	static public void reg(IntPtr l) {
		getTypeTable(l,"Game.Field");
		addMember(l,StartThread);
		addMember(l,Shutdown);
		addMember(l,Request);
		addMember(l,RequestAsync);
		addMember(l,ReadCommandsAsync);
		addMember(l,Send);
		addMember(l,SendAndWait);
		addMember(l,WaitForRequest);
		addMember(l,WaitForAck);
		addMember(l,log);
		addMember(l,logException);
		addMember(l,FindCharacter);
		addMember(l,SetPlayerCharacter);
		addMember(l,ShowMessage);
		addMember(l,Display);
		addMember(l,InitLua);
		addMember(l,Init);
		addMember(l,InitRandom);
		addMember(l,Process);
		addMember(l,DoTurnStart);
		addMember(l,DoThink);
		addMember(l,DoMove);
		addMember(l,DoPlay);
		addMember(l,DoTurnEnd);
		addMember(l,WalkCharacter);
		addMember(l,UseSkill);
		addMember(l,AttackCharacter);
		addMember(l,ExecuteSpecial);
		addMember(l,AddDamage);
		addMember(l,KillCharacter);
		addMember(l,UpdateViewport);
		addMember(l,"RequestTimeoutMillis",get_RequestTimeoutMillis,set_RequestTimeoutMillis,true);
		addMember(l,"NoLog",get_NoLog,set_NoLog,true);
		addMember(l,"NoUnity",get_NoUnity,set_NoUnity,true);
		addMember(l,"path_",get_path_,set_path_,true);
		addMember(l,"WaitingType",get_WaitingType,null,true);
		addMember(l,"Map",get_Map,null,true);
		addMember(l,"State",get_State,null,true);
		addMember(l,"TurnNum",get_TurnNum,null,true);
		addMember(l,"Thinking",get_Thinking,null,true);
		addMember(l,"Player",get_Player,null,true);
		addMember(l,"L",get_L,null,true);
		addMember(l,"ShutdownError",get_ShutdownError,null,true);
		createTypeMetatable(l,constructor, typeof(Game.Field));
	}
}
