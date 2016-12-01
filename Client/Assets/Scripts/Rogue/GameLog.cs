﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game;
using RSG;

namespace GameLog {
	public interface ICommand {
		IPromise Process (GameScene scene);
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
		public IPromise Process(GameScene scene){
			return Promise.Resolved ();
		}
	}

	public partial class Walk : ICommand {
		public IPromise Process(GameScene scene){
			var ch = scene.Field.FindCharacter (CharacterId);
			var cc = scene.GetCharacterRenderer (ch);
			cc.Animate ("EnemyWalk01");
			cc.transform.localRotation = ((Direction)Dir).ToWorldQuaternion();
			var w = new GameScene.Walking(){
				Items = new GameScene.Walking.Item[]{ new GameScene.Walking.Item{
						CharacterContainer=cc, 
						From=cc.transform.localPosition,
						To=scene.PointToVector(new Point(X,Y)),
					}}
			};
			return scene.StartWalking (w);
		}
	}

	public partial class WalkMulti : ICommand {
		public IPromise Process(GameScene scene){
			var items = Items.Select( w=>{
				var ch = scene.Field.FindCharacter (w.CharacterId);
				var cc = scene.GetCharacterRenderer (ch);
				cc.Animate ("EnemyWalk01");
				cc.transform.localRotation = ((Direction)w.Dir).ToWorldQuaternion();
				return new GameScene.Walking.Item{
					CharacterContainer=cc, 
					From=scene.PointToVector(new Point(w.OldX,w.OldY)),
					To=scene.PointToVector(new Point(w.X,w.Y)),
				};
			}).ToArray();
				
			var walking = new GameScene.Walking(){
				Items = items,
			};

			return scene.StartWalking (walking);
		}
	}

	public partial class AnimateCharacter : ICommand {
		public IPromise Process(GameScene scene){
			var c = scene.Field.FindCharacter (CharacterId);
			var cc = scene.GetCharacterRenderer(c);
			cc.Animate ("EnemyAttack01");
			cc.transform.localRotation = ((Direction)Dir).ToWorldQuaternion ();
			return PromiseEx.Delay (0.5f);
		}
	}

	public partial class KillCharacter : ICommand {
		public IPromise Process(GameScene scene){
			Debug.Log ("kill");
			var c = scene.Field.FindCharacter (CharacterId);
			var cc = scene.GetCharacterRenderer(c);
			cc.Animate ("EnemyDamage01");
			return PromiseEx.Delay (1.0f).Then (() => {
				Object.Destroy (cc.gameObject);
				scene.Characters.Remove (CharacterId);
			});
		}
	}
		
	//========================================
	// Requests
	//========================================

	public partial class WalkRequest : IRequest {
		public void Process(Field f){
			f.path_ = Path.Select (p => new Game.Point (p)).ToList ();
		}
	}

	public partial class AckResponseRequest : IRequest {
		public void Process(Field f){
		}
	}

	public partial class ShutdownRequest : IRequest {
		public void Process(Field f){
		}
	}

	public partial class SkillRequest : IRequest {
		public void Process(Field f){
			var c = f.FindCharacter (CharacterId);
			c.Dir = (Direction)Dir;
			f.SendAndWait (new AnimateCharacter{ CharacterId = c.Id, Dir = (int)c.Dir, X = c.Position.X, Y = c.Position.Y, Animation = Animation.Attack });
			var scope = new SpecialScope (ScopeType.Straight, ScopeTargetType.Others, 1);
			var special = new Game.Specials.Attack (){ };
			f.UseSkill (c, (Direction)Dir, scope, special);
		}
	}
}
