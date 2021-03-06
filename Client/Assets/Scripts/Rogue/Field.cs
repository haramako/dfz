﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Master;
using SLua;

namespace Game
{
	public class GameOverException : Exception {}
	public class ShutdownException: Exception {}

	public enum GameState
	{
		Prelude,
		TurnStart,
		Think,
		Move,
		Play,
		TurnEnd,
		GameOver,
		Shutdowned,
	}

	public enum WaitingType
	{
		None,
		Ack, // AckRequest
		Action, // WalkRequest
	}

	[CustomLuaClass]
	public class Field
	{
		Thread gameThread;
		ConcurrentQueue<List<GameLog.ICommand>> sendQueue = new ConcurrentQueue<List<GameLog.ICommand>>(1);
		ConcurrentQueue<GameLog.IRequest> recvQueue = new ConcurrentQueue<GameLog.IRequest>(1);
		List<GameLog.ICommand> commandList = new List<GameLog.ICommand>();

		public WaitingType WaitingType { get; private set; }
		public int RequestTimeoutMillis = -1;

		public Map Map { get; private set; }
		public GameState State { get; private set; }
		public int TurnNum { get; private set; }
		public Thinking Thinking { get; private set; }

		public Character Player { get; private set; }
		public LuaState L { get; private set; }

		Stage stage;

		/// <summary>
		/// 通信ログを表示しない
		/// </summary>
		public bool NoLog;
		public bool NoUnity;

		/// <summary>
		/// シャットダウンの原因となったエラー
		/// </summary>
		/// <value>The shutdown error.</value>
		public Exception ShutdownError { get; private set; }

		public Field ()
		{
			State = GameState.Prelude;
			Thinking = new Thinking (this);
			TurnNum = 0;
		}

		//========================================================
		// プロセス通信（外部から呼ぶもの）
		//========================================================

		void validateReadyToRequest()
		{
			if (State == GameState.Shutdowned)
			{
				throw new System.InvalidOperationException ("field is already shutdowned");
			}
			if (recvQueue.Count > 0)
			{
				throw new System.InvalidOperationException ("field has recv quue item");
			}
		}

		public void StartThread()
		{
			if (State != GameState.Prelude)
			{
				throw new InvalidOperationException ("field state is not prelude");
			}

			gameThread = new Thread(Process);
			gameThread.Start();

			sendQueue.Dequeue (RequestTimeoutMillis); // スレッドが開始するまでまつ
		}

		public void Shutdown()
		{
			if (State != GameState.Shutdowned)
			{
				recvQueue.Enqueue (new GameLog.ShutdownRequest());

				try
				{
					sendQueue.Dequeue (RequestTimeoutMillis); // 反応があるまで待つ
				}
				catch( Exception ex)
				{
					gameThread.Abort ();
				}
			}
			if (ShutdownError != null)
			{
				throw ShutdownError;
			}
		}

		public List<GameLog.ICommand> Request(GameLog.IRequest request)
		{
			if (request == null)
			{
				throw new ArgumentNullException ("request must not be null");
			}
			validateReadyToRequest ();
			if (!recvQueue.TryEnqueue (request))
			{
				throw new InvalidOperationException ("cant enqueue into recvQueue");
			}

			var result = new List<GameLog.ICommand> ();
			while (true)
			{
				var cmds = sendQueue.Dequeue (RequestTimeoutMillis);

				result.AddRange (cmds);
				if (WaitingType != WaitingType.Ack)
				{
					return result;
				}
				else
				{
					recvQueue.Enqueue (new GameLog.AckResponseRequest ());
				}
			}
		}

		public void RequestAsync(GameLog.IRequest request)
		{
			validateReadyToRequest ();
			recvQueue.Enqueue (request);
		}

		public List<GameLog.ICommand> ReadCommandsAsync()
		{
			List<GameLog.ICommand> cmds;
			if (sendQueue.TryDequeue (out cmds))
			{
				return cmds;
			}
			else
			{
				return null;
			}
		}

		//========================================================
		// プロセス通信（内部から呼ぶもの）
		//========================================================

		public void Send(GameLog.ICommand command)
		{
			if (command == null)
			{
				throw new ArgumentNullException ("command must not be null");
			}
			commandList.Add (command);
			log("F->S:" + TurnNum + ":" + command + ": " + inspect(command));
		}

		public void SendAndWait(GameLog.ICommand command)
		{
			Send (command);
			WaitForAck ();
		}

		public GameLog.IRequest WaitForRequest(WaitingType waitingType)
		{
			WaitingType = waitingType;

			sendQueue.Enqueue (commandList);
			commandList = new List<GameLog.ICommand> ();
			log ("F->S:" + TurnNum + ":(Waiting) " + waitingType);

			var req = recvQueue.Dequeue();
			log ("F<-S:" + TurnNum + ":" + req.GetType () + ": " + inspect (req));
			if (req is GameLog.ShutdownRequest)
			{
				throw new ShutdownException ();
			}
			waitingType = WaitingType.None;
			return req;
		}

		public T WaitForRequest<T>(WaitingType waitingType) where T : GameLog.IRequest
		{
			var res = WaitForRequest(waitingType);
			if( res.GetType() != typeof(T))
			{
				throw new InvalidOperationException("require " + typeof(T) + " but " + res.GetType());
			}
			return (T)res;
		}

		public void WaitForAck()
		{
			WaitForRequest<GameLog.AckResponseRequest> (WaitingType.Ack);
		}

		//========================================================
		// ユーティリティ
		//========================================================

		[System.Diagnostics.DebuggerNonUserCode]
		public void log(object obj)
		{
			if (NoLog)
			{
			}
			else if (!NoUnity)
			{
				#if UNITY_5
				UnityEngine.Debug.Log (obj);
				#endif
			}
			else
			{
				System.Console.WriteLine (obj);
			}
		}

		public void logException(Exception ex)
		{
			if (NoLog)
			{
			}
			else if (!NoUnity)
			{
				#if UNITY_5
				UnityEngine.Debug.LogException (ex);
				#endif
			}
			else
			{
				log (ex);
			}
		}

		string inspect(object obj)
		{
			if (!NoUnity)
			{
				#if UNITY_5
				return UnityEngine.JsonUtility.ToJson (obj);
				#else
				return null;
				#endif
			}
			else
			{
				return obj.ToString ();
			}
		}

		public Character FindCharacter(int cid)
		{
			return Map.Characters.First (c => c.Id == cid);
		}

		public Character FindCharacter(string name)
		{
			return Map.Characters.First (c => c.Name == name);
		}

		public void SetPlayerCharacter(Character c)
		{
			c.Type = CharacterType.Player;
			Player = c;
		}

		public void ShowMessage(string msg, params object[] param)
		{
			var strParam = param.Select (x => x.ToString ()).ToArray ();
			Send (new GameLog.Message { MessageId = msg, Param = strParam.ToList() });
			log ("MSG: " + msg + " " + string.Join (" ", strParam));
		}

		public string Display()
		{
			var sb = new StringBuilder ();
			for (int y = 0; y < Map.Width; y++)
			{
				for (int x = 0; x < Map.Height; x++)
				{
					var ch = Map[x, y].Character;
					if( ch != null )
					{
						sb.AppendFormat ("{0} ", ch.Name [0]);
					}
					else
					{
						sb.AppendFormat ("{0} ", Map [x, y].Val);
					}
				}
				sb.AppendLine ();
			}
			foreach( var ch in Map.Characters )
			{
				sb.AppendFormat( "{0:d2}:{1} {2} HP={3} ATK={4} DEF={5}\n", ch.Id, ch.Name, ch.Position, ch.Hp, ch.Attack, ch.Defense);
			}
			return sb.ToString ();
		}

		//========================================================
		// 初期化
		//========================================================

		LuaSvr luaSvr;

		public void InitLua(string src)
		{
			//luaSvr = new LuaSvr ();
			//luaSvr.init (null, () => {});
			//L = luaSvr.luaState;
			//L.doString (src);
		}

		public void Init(Map map)
		{
			Map = map;
		}

		//========================================================
		// ターンの処理
		//========================================================

		public void Process()
		{
			try
			{
				InitLua (@"
function test(think, c)
  print(getmetatable(think))
  print(think:ToString())
  local r = think:NewActionResult()
  local y = Slua.CreateClass('Game.Thinking.Hoge')
  local x = Slua.CreateClass('Game.ActionResult')
  --r.MoveTo = 1
  --return r
  return nil
end
");

				while (true)
				{
					try
					{
						switch (State)
						{
							case GameState.Prelude:
								WaitForAck();
								State = GameState.TurnStart;
								break;
							case GameState.TurnStart:
								DoTurnStart();
								break;
							case GameState.Think:
								DoThink();
								break;
							case GameState.Move:
								DoMove();
								break;
							case GameState.Play:
								DoPlay();
								break;
							case GameState.TurnEnd:
								DoTurnEnd();
								break;
							case GameState.GameOver:
								break;
						}
					}
					catch (GameOverException)
					{
						State = GameState.GameOver;
					}
					catch(ShutdownException)
					{
						throw;
					}
					catch(Exception)
					{
						throw;
					}
				}

			}
			catch(ShutdownException)
			{
				// DO NOTHING
			}
			catch(Exception ex)
			{
				logException (ex);
				ShutdownError = ex;
			}

			log ("Shutdown");
			Send (new GameLog.Shutdown () {});

			sendQueue.Enqueue (commandList);
			log("enqueued");

			State = GameState.Shutdowned;
		}

		public void DoTurnStart()
		{
			TurnNum++;

			foreach (var c in Map.Characters)
			{
				c.ClearTurnLocalVariables ();
			}

			State = GameState.Think;
		}

		public List<Point> path_ = new List<Point>();

		public void DoThink()
		{
			while (true)
			{
				if ( path_.Count <= 0)
				{
					var req = WaitForRequest (WaitingType.Action);
					req.Process (this);
					if (!(req is GameLog.WalkRequest))
					{
						break;
					}
				}
				else
				{
					if (Map.FloorIsWalkableNow (path_ [0]))
					{
						break;
					}
					else
					{
						path_.Clear ();
					}
				}
			}
			State = GameState.Move;
		}

		public void DoMove()
		{
			var commands = new List<GameLog.Walk> ();

			if (path_.Count > 0)
			{
				commands.Add(makeWalkCommand (Player, path_ [0]));
				path_.RemoveAt (0);
			}

			foreach (var c in Map.Characters.OrderBy(x => x.Speed))
			{
				if (!c.IsPlayer)
				{
					var move = Thinking.ThinkMove (c);
					if (move.IsMove)
					{
						if (Map.FloorIsWalkableNow (move.MoveTo))
						{
							commands.Add (makeWalkCommand (c, move.MoveTo));
							c.Moved = true;
						}
					}
				}
			}

			Send (new GameLog.WalkMulti () { Items = commands });

			WaitForAck ();
			State = GameState.Play;
		}

		public void DoPlay()
		{

			foreach (var c in Map.Characters)
			{
				if( c.IsPlayer || c.Moved)
				{
					continue;
				}

				var action = Thinking.ThinkAttack (c);
				if (action.Type == ActionResultType.Attack)
				{
					AttackCharacter (c, action.Dir);
				}
			}

			State = GameState.TurnEnd;
		}

		public void DoTurnEnd()
		{
			State = GameState.TurnStart;
		}

		//========================================================
		// キャラクターの処理
		//========================================================

		GameLog.Walk makeWalkCommand(Character c, Point pos)
		{
			var oldPos = c.Position;
			Map.MoveCharacter(c, pos);
			c.Dir = (pos - oldPos).ToDir ();
			return new GameLog.Walk
			{
				CharacterId = c.Id,
				X = pos.X,
				Y = pos.Y,
				OldX = oldPos.X,
				OldY = oldPos.Y,
				Dir = (int)c.Dir
			};
		}

		public void WalkCharacter(Character c, Point pos)
		{
			Send( makeWalkCommand (c, pos));
		}

		public void UseSkill(Character c, Direction dir, Skill skill)
		{
			foreach( var code in skill.Codes)
			{
				UseSkillCode (c, dir, code);
			}
		}

		public void UseSkillCode(Character c, Direction dir, SkillCode code)
		{
			ShowMessage ("UseSkill", c.Name);
			c.Dir = dir;
			var result = code.Scope.GetRange (new ScopeParam () { From = c, FromPoint = c.Position, Dir = dir, Map = Map });

			var effect = G.FindSkillEffectBySymbol (code.Effect);
			ShowSkillEffect (code.Scope, result, effect);

			foreach (var p in result.Targets)
			{
				log (p);
				var target = Map [p].Character;
				if (target != null)
				{
					foreach (var special in code.SpecialList)
					{
						if (target.IsDead)
						{
							break;
						}
						ExecuteSpecial (special, new SpecialParam
						{
							FromCharacter = c,
							FromPoint = c.Position,
							Target = target,
							Pos = target.Position
						});
					}
				}
			}
		}

		public void ShowSkillEffect(SpecialScope scope, ScopeResult sr, SkillEffect effect)
		{
			SendAndWait (new GameLog.ShowSkillEffect
			{
				Scope = scope,
				CharacterId = sr.Param.From.Id,
				Dir = (int)sr.Param.From.Dir,
				Path = sr.Path.Select(p => ((GameLog.Point)p)).ToList(),
				Targets = sr.Targets.Select(p => ((GameLog.Point)p)).ToList(),
				Effect = effect,
			});
		}

		public void AttackCharacter(Character c, Direction dir)
		{
			c.Dir = dir;
			//SendAndWait (new GameLog.AnimateCharacter { CharacterId = c.Id, Dir = (int)c.Dir, X = c.Position.X, Y = c.Position.Y, Animation = GameLog.Animation.Attack });
			var skill = G.FindSkillBySymbol ("attack");
			foreach (var code in skill.Codes)
			{
				UseSkill (c, dir, skill);
			}
		}

		public void ExecuteSpecial(Special special, SpecialParam p)
		{
			special.Execute (this, p);
		}

		public void AddDamage(Character c, GameLog.DamageInfo damage)
		{
			ShowMessage ("DamageCharacter", c.Name, damage.Amount);
			Send (new GameLog.ShowEffect { X = c.X, Y = c.Y, Dir = (int)c.Dir, EffectSymbol = "EfDamage01" });
			c.Hp -= damage.Amount;
			if (c.Hp <= 0)
			{
				c.Hp = 0;
				KillCharacter (c);
			}
			else
			{
				SendAndWait (new GameLog.AnimateCharacter { CharacterId = c.Id, Animation = GameLog.Animation.Damaged });
			}
		}

		public void KillCharacter(Character c)
		{
			ShowMessage ("KillCharacter", c.Name);
			SendAndWait (new GameLog.KillCharacter { CharacterId = c.Id, X = c.Position.X, Y = c.Position.Y });
			Map.RemoveCharacter (c);
		}

		public void UpdateViewport()
		{
			if (Player == null)
			{
				return;
			}

			var room = Map [Player.Position].RoomId;

			for (int x = 0; x < Map.Width; x++)
			{
				for (int y = 0; y < Map.Height; y++)
				{
					Map [x, y].Viewport = false;
				}
			}
			for (int x = 0; x < Map.Width; x++)
			{
				for (int y = 0; y < Map.Height; y++)
				{
					var f = Map [x, y];

					var view = false;
					if (f.RoomId == 0)
					{
						if (Player.Position == new Point (x, y))
						{
							view = true;
						}
					}
					else if (f.RoomId == room)
					{
						view = true;
					}

					if (view)
					{
						for (int dx = -1; dx <= 1; dx++)
						{
							for (int dy = -1; dy <= 1; dy++)
							{
								Map [x + dx, y + dy].Viewport = true;
								Map [x + dx, y + dy].Open = true;
							}
						}
					}
				}
			}
		}
	}
}

