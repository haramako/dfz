using System;
using System.Linq;
using System.Reflection;

namespace Game
{

	public class SpecialParam
	{
		public Character FromCharacter;
		public Point FromPoint;
		public Point Pos;
		public Character Target;
	}

	/// <summary>
	/// 杖、薬・スキル・敵の能力等の特殊能力を処理するクラス
	/// </summary>
	public abstract class Special
	{
		public Master.SpecialTemplate T;

		public virtual void Execute (Field f, SpecialParam p)
		{
		}

		static public Special Create(Master.SpecialTemplate t)
		{
			var type = Type.GetType ("Game.Specials." + t.Type, true, true);
			var instance = (Special)Activator.CreateInstance (type);
			instance.T = t;
			return instance;
		}

		public GameLog.DamageInfo CalcDamage(Character executer, Character target)
		{
			var di = new GameLog.DamageInfo();

			var damage = 0;


			Logger.Info ("攻撃力: {0}, 防御力: {1}", executer.Attack, target.Defense);

			damage += executer.Attack * T.Pow / 100 - target.Defense / 2;

			damage += T.Amount;

			di.Amount = damage;

			return di;
		}
	}
}

namespace Game.Specials
{
	public class Attack : Special
	{
		public override void Execute(Field f, SpecialParam p)
		{
			f.ShowMessage ("AttackTo", p.FromCharacter.Name, p.Target.Name);
			f.AddDamage (p.Target, CalcDamage (p.FromCharacter, p.Target));
		}
	}
}
