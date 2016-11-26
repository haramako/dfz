using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game;

namespace GameLog {
	public interface ICommand {
		void Process (GameScene scene);
	}

	public interface IRequest {
		void Process (Field field);
	}

	public partial class Point {
		public Point(Game.Point src){
			X = src.X;
			Y = src.Y;
		}
		public Point(int x, int y){
			X = x;
			Y = y;
		}
		public static implicit operator Game.Point(Point src){
			return new Game.Point (src.X, src.Y);
		}
	}

	public partial class Shutdown : ICommand {
		public void Process(GameScene scene){
		}
	}

	public partial class WaitForRequest : ICommand {
		public void Process(GameScene scene){
			scene.mode = GameScene.Mode.QMove;
		}
	}

	public partial class Walk : ICommand {
		public void Process(GameScene scene){
			var ch = scene.Field.FindCharacter (CharacterId);
			var cc = scene.GetCharacterRenderer (ch);
			cc.Animate ("EnemyWalk01");
			cc.transform.localRotation = ((Direction)Dir).ToWorldQuaternion();
			scene.StartWalking (cc, new Point (X, Y));
			scene.View.SpendCurosr ();
		}
	}

	//========================================
	// Requests
	//========================================

	public partial class WalkRequest : IRequest {
		public void Process(Field f){
		}
	}

	public partial class AckRequest : IRequest {
		public void Process(Field f){
		}
	}

	public partial class ShutdownRequest : IRequest {
		public void Process(Field f){
		}
	}
}
