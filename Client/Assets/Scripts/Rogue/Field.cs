using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Master;

namespace Game
{
	public class GameOverException : Exception {}

	public enum GameState {
		TurnStart,
        Play,
		TurnEnd,
		GameOver,
		Shutdowned,
	}

    public class Message
    {
        public string Type { get; private set; }
        public object[] Param { get; private set; }
        public Message(string type, params object[] param)
        {
            Type = type;
            Param = param;
        }
    }

    public class Field
	{
		public Map Map { get; private set; }
		public GameState State { get; private set; }
		public int TurnNum{ get; private set; }
		public ConcurrentQueue<GameLog.ICommand> SendQueue = new ConcurrentQueue<GameLog.ICommand>();
		public ConcurrentQueue<GameLog.IRequest> RecvQueue = new ConcurrentQueue<GameLog.IRequest>();
        public Thread GameThread;

		Stage stage_;

		public Field ()
		{
			State = GameState.TurnStart;
			TurnNum = 0;
		}

        public void StartThread()
        {
            GameThread = new Thread(Process);
            GameThread.Start();
        }

		public void Shutdown(){
			if (State != GameState.Shutdowned) {
				Request (GameLog.ShutdownRequest.CreateInstance ());
			}
		}

		public GameLog.ICommand Request(GameLog.IRequest request){
			if (State == GameState.Shutdowned) {
				throw new System.InvalidOperationException ("field is already shutdowned");
			}
			RecvQueue.Enqueue (request);
			return SendQueue.Dequeue ();
		}

		public void log(object obj){
					#if UNITY
					UnityEngine.Debug.Log (obj);
					#else
					System.Console.WriteLine(obj);
					#endif
				}

		string inspect(object obj){
#if UNITY
			return UnityEngine.JsonUtility.ToJson(obj);
#else
			return obj.ToString();
#endif
		}

		public class ShutdownException: Exception {}

		public GameLog.IRequest Send(GameLog.ICommand command){
			if (command == null) {
				throw new ArgumentNullException ("command must not be null");
			}
			log("F->S: " + command + ": " + inspect(command));
			SendQueue.Enqueue (command);
            var req = RecvQueue.Dequeue();
			log("F<-S: " + req.GetType() + ": " + inspect(req));
			if (req is GameLog.ShutdownRequest) {
				//SendQueue.Enqueue (GameLog.Shutdown.CreateInstance());
				throw new ShutdownException ();
			}
            return req;
        }

		public T SendRecv<T>(GameLog.ICommand command) where T : GameLog.IRequest
        {
			var res = Send(command);
			if( res.GetType() != typeof(T))
            {
				throw new InvalidOperationException("require " + typeof(T) + " but " + res.GetType());
            }
			return (T)res;
        }

		public Character FindCharacter(int cid){
			return Map.Characters.First (c => c.Id == cid);
		}

		public Character FindCharacter(string name){
			return Map.Characters.First (c => c.Name == name);
		}

        public void Process(){
			try{

	            while (true)
	            {
	                try
	                {
	                    switch (State)
	                    {
	                    case GameState.TurnStart:
	                        DoTurnStart();
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
					catch(ShutdownException){
						break;
					}
	                catch(Exception ex)
	                {
#if UNITY
	                    UnityEngine.Debug.LogException(ex);
#else
						throw;
#endif
	                }
	            }
			}catch(Exception ex){
#if UNITY
				UnityEngine.Debug.LogException (ex);
#else
				throw;
#endif
			}
			log ("Shutdown");
			SendQueue.Enqueue (GameLog.Shutdown.CreateInstance ());
			State = GameState.Shutdowned;
		}

		public void Init(Map map){
			Map = map;
		}
		
		public void Init(Stage stage)
        {
			stage_ = stage;
			Map = new Map (stage.Width, stage.Height);
			for (int x = 0; x < stage.Width; x++) {
				for (int y = 0; y < stage.Height; y++) {
					Map [x, y].Val = stage.Tiles [x + y * stage.Width];
				}
			}

			int i = 0;
			foreach (var sc in stage.Characters) {
				if (!(sc.Name.StartsWith ("E") || sc.Name.StartsWith ("P"))) {
					continue;
				}
				var c = Character.CreateInstance();
				c.Id = i++;
				c.Name = sc.Name;
				c.AtlasId = sc.Char;
				c.Speed = sc.Speed;
				if (sc.Name.StartsWith ("E")) {
					c.Speed = 10 + int.Parse(sc.Name.Substring(1));
				} else {
					c.Speed = 5 + int.Parse (sc.Name.Substring (1));
				}
				Map.AddCharacter(c, new Point(sc.X,	sc.Y));
			}
				
        }

		public void DoTurnStart(){
			TurnNum++;
			State = GameState.Play;
		}

		List<Point> path_;

		public void DoPlay(){
			foreach (var ch in Map.Characters.OrderBy(x=>x.Speed))
            {
				if (ch.Name == "P1") {
					if (path_ == null || path_.Count <= 0) {
						var res = SendRecv<GameLog.WalkRequest> (GameLog.WaitForRequest.CreateInstance());
						path_ = res.Path.Select (p => new Point (p)).ToList ();
					}
					if (path_.Count > 0) {
						WalkCharacter (ch, path_ [0]);
						path_.RemoveAt (0);
					}
				}
            }
			State = GameState.TurnEnd;
        }
        
		public void DoTurnEnd(){
			State = GameState.TurnStart;
		}

        public void WalkCharacter(Character ch, Point pos)
        {
			var oldPos = ch.Position;
            Map.MoveCharacter(ch, pos);
			ch.Dir = (pos - oldPos).ToDir ();
			var cmd = GameLog.Walk.CreateInstance ();
			cmd.CharacterId = ch.Id;
			cmd.X = pos.X;
			cmd.Y = pos.Y;
			cmd.Dir = (int)ch.Dir;
			SendRecv<GameLog.AckRequest> (cmd);
        }

		public string Display(){
			var sb = new StringBuilder ();
			for (int y = 0; y < Map.Width; y++) {
				for (int x = 0; x < Map.Height; x++) {
					var ch = Map[x,y].Character;
					if( ch != null ){
						sb.AppendFormat ("{0} ", ch.Name [0]);
					}else{
						sb.AppendFormat ("{0} ", Map [x, y].Val);
					}
				}
				sb.AppendLine ();
			}
			foreach( var ch in Map.Characters ){
				sb.AppendFormat( "{0:d2}:{1} {2} HP={3} ATK={4} DEF={5}\n", ch.Id, ch.Name, ch.Position, ch.Hp, ch.Attack, ch.Defense);
			}
			return sb.ToString ();
		}

	}
}

