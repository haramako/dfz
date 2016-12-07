using System;
using System.Linq;

namespace Game
{
	public interface IGameLog {
	}

	/// <summary>
	/// ゲーム内のキャラクター
	/// </summary>
	public sealed partial class Character : Google.ProtocolBuffers.Message {

		public Point Position { 
			get { 
				return new Point(X,Y);
			}
			set {
				X = value.X;
				Y = value.Y;
			}
		}

		public Direction Dir {
			get { 
				return (Direction)SavedDir; 
			}
			set { 
				SavedDir = (int)value; 
			}
		}

		public bool IsDead { get { return Hp <= 0; } }
		public bool IsPlayer { get { return Type == CharacterType.Player; } }
		public bool IsEnemy { get { return Type == CharacterType.Enemy; } }

		Field f;

		// ここから下は実行時のみに利用されるデータ、つまり保存されないので１ターンで蒸発する

		/// <summary>
		/// このターンに移動したかどうか？
		/// </summary>
		public bool Moved;

		/// <summary>
		/// 現在のターンの予定行動
		/// </summary>
		public ActionResult Action;

		public void ClearTurnLocalVariables(){
			Moved = false;
			Action = new ActionResult ();
		}

		public void SetField(Field field){
			f = field;
		}

		public override string ToString(){
			return Name;
		}
	}

}

