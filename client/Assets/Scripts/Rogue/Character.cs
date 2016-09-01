using System;
using System.Linq;
using UnityEngine;

namespace Rogue
{
	public enum CharacterType {
		Player,
		Enemy
	}

	/// <summary>
	/// ゲーム内のキャラクター
	/// </summary>
	public class Character {

		public int Id;
        public int AtlasId;
		public string Name;
		public CharacterType Type;
		public int Hp;
		public int MaxHp;
		public int Attack = 10;
		public int Defense;
		public Point Position;
		public Direction Dir = Direction.South;
		public bool IsDead { get { return Hp <= 0; } }

		public Character(){
		}

        Game game { get { return Game.Instance; } }

		public virtual void DoAttack(){}
		public virtual void DoMove(){}
		public virtual void DoTurnEnd(){}
			
		public virtual void AttackTo(Character target){
			int damage = Attack+UnityEngine.Random.Range(0,5);
			if (target != null) {
				Dir = (target.Position - Position).ToDir ();
				game.Send ("message", string.Format ("{0}の攻撃！\n{1}に{2}のダメージを与えた", Name, target.Name, damage));
				game.Send("attack", this, Dir, damage, Math.Max (target.Hp - damage, 0));
				game.Send("damaged", target, damage, Math.Max (target.Hp - damage, 0));
				target.AddDamage (damage);
			} else {
				game.Send("attack", this, Dir, damage);
			}
		}

		public bool AddDamage(int damage){
			Hp -= damage;
			bool dead = false;
			if( Hp <= 0 ){
				Hp = 0;
				dead = true;
				if (Type == CharacterType.Enemy) {
					game.Map.RemoveCharacter (this);
					game.Send ("message", string.Format ("{0}を倒した", Name));
					game.Send ("dead", this);
				} else {
					game.Send ("message", string.Format ("{0}は力尽きた・・・", Name));
					game.Send ("gameover", this);
					throw new GameOverException ();
				}
			}
			return dead;
		}

		public bool IsAttackableTo(Character target){
			return target != null && game.Map.StepFlyable() (Position, target.Position) <= 1;
		}

		public override string ToString(){
			return Name;
		}
	}

}

