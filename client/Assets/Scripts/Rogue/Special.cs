using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace Rogue {

	/// <summary>
	/// 杖、薬・スキル・敵の能力等の特殊能力を処理するクラス
	/// </summary>
	public class Special {
		public int Pow;
		public int Turn;
		public int Amount;
		public int Rand;
		public int Direct;
		public int Prob;

		public SpecialScope Scope { get; private set; }

		public virtual void Execute( Point pos ){
		}

	}
}
