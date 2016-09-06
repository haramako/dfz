using System;
using System.Linq;
using UnityEngine;

namespace Game
{
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
				SavedDir = (int)Dir; 
			}
		}

		public bool IsDead { get { return Hp <= 0; } }

		public override string ToString(){
			return Name;
		}
	}

}

