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
        public ConcurrentQueue<Message> SendQueue = new ConcurrentQueue<Message>();
        public ConcurrentQueue<Message> RecvQueue = new ConcurrentQueue<Message>();
        public Thread GameThread;

		Stage stage_;

		public void log(object obj){
			UnityEngine.Debug.Log (obj);
		}
		
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

        public Message Send(string command, params object[] param){
			log("Send: " + command + ": " + UnityEngine.JsonUtility.ToJson(param));
			SendQueue.Enqueue (new Message (command, param ));
            var recv = RecvQueue.Dequeue();
            log("Recv: " + recv.Type + ": " + UnityEngine.JsonUtility.ToJson(recv.Param));
            return recv;
        }

        public Message SendRecv(string recvType, string command, params object[] param)
        {
            var res = Send(command, param);
            if( res.Type != recvType)
            {
                throw new InvalidOperationException("require " + recvType + " but " + res.Type);
            }
            return res;
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
	                catch(Exception ex)
	                {
	                    UnityEngine.Debug.LogException(ex);
	                }
	            }
			}catch(Exception ex){
				UnityEngine.Debug.LogException (ex);
			}
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
			State = GameState.TurnEnd;
			foreach (var ch in Map.Characters.OrderBy(x=>x.Speed))
            {
				if (ch.Name == "P1") {
					if (path_ == null || path_.Count <= 0) {
						var res = SendRecv ("AMove", "QMove", ch);
						path_ = (List<Point>)res.Param [0];
					}
					if (path_.Count > 0) {
						WalkCharacter (ch, path_ [0]);
						path_.RemoveAt (0);
					}
				}
            }
        }
        
		public void DoTurnEnd(){
			State = GameState.TurnStart;
		}

        public void WalkCharacter(Character ch, Point pos)
        {
			var oldPos = ch.Position;
            Map.MoveCharacter(ch, pos);
			SendRecv(null, "Walk", ch, oldPos, pos);
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

