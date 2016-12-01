using System;
using System.Linq;
using UnityEngine;

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

		public void SetField(Field field){
			f = field;
		}

		public override string ToString(){
			return Name;
		}
	}

}

