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

		public Master.SpecialScope Scope { get; private set; }

		public virtual void Execute (Field f, SpecialParam p)
		{
		}

		static public Special Create(Master.SpecialTemplate t)
		{
			var type = Type.GetType (t.Type);
			var instance = (Special)Activator.CreateInstance (type);
			instance.T = t;
			return instance;
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
			f.AddDamage (p.Target, new GameLog.DamageInfo () { Amount = 10 });
		}
	}
}
