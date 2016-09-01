using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Rogue
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

    public class Game
	{
        public static Game Instance;

		public Map Map { get; private set; }
		public GameState State { get; private set; }
		public int TurnNum{ get; private set; }
        public ConcurrentQueue<Message> SendQueue = new ConcurrentQueue<Message>();
        public ConcurrentQueue<Message> RecvQueue = new ConcurrentQueue<Message>();
        public Thread GameThread;

		public void log(object obj){
			UnityEngine.Debug.Log (obj);
		}
		
		public Game ()
		{
			Map = new Map (20, 20);
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

        public void Process(){
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
		}

        public void Init()
        {
            var atlas = new int[] { 1, 2, 3, 168, 174, 210, 211 };
            for ( int i=0; i<20; i++)
            {
                Point pos;
                while (true)
                {
                    pos = new Point(UnityEngine.Random.Range(8, 20), UnityEngine.Random.Range(8, 20));
                    if (Map[pos].Character == null) break;
                }
                var c = new Character();
                c.Id = i;
                c.AtlasId = atlas[UnityEngine.Random.Range(0, atlas.Length - 1)];
                if (Map[pos].Character == null)
                {
                    Map.AddCharacter(c, pos);
                }
            }
        }

		public void DoTurnStart(){
			TurnNum++;
			State = GameState.Play;
		}

		public void DoPlay(){
			State = GameState.TurnEnd;
            foreach (var ch in Map.Characters)
            {
                var res = SendRecv("AMove", "QMove", ch);
                var path = (Point[])res.Param[0];
				WalkCharacter(ch, path);
				if (res.Param.Length > 1) {
					var subcommand = (string)res.Param [1];
					if (subcommand == "Attack") {
						var targetPos = (Point)res.Param [2];
						var target = Map [targetPos].Character;
						SendRecv (null, "Attack", ch, target, (target.Position - ch.Position).ToDir (), 99);
					}
				}
            }
        }
        
		public void DoTurnEnd(){
			State = GameState.TurnStart;
		}

        public void WalkCharacter(Character ch, Point[] path)
        {
            Map.MoveCharacter(ch, path.Last());
            SendRecv(null, "Walk", ch, path);
        }

		public string Display(){
			var sb = new StringBuilder ();
			for (int y = 0; y < Map.Width; y++) {
				for (int x = 0; x < Map.Height; x++) {
					var ch = Map[x,y].Character;
					if( ch != null ){
						sb.AppendFormat ("{0} ", ch.Name[0]);
					}else{
						if (Map [x, y].Kind.Id == 0) {
							sb.AppendFormat (". ", Map [x, y].Kind.Id);
						} else {
							sb.AppendFormat ("{0} ", Map [x, y].Kind.Id);
						}
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

