using System.Collections.Generic;
using System.Linq;
using Game;
#if UNITY_5
using UnityEngine;
#endif

namespace Master
{

	public partial class SkillCode
	{
		Special[] specialList_;

		/// <summary>
		/// 特殊効果(Special)の配列を返す.
		/// これは、ゲーム内では、SpecialTemplateではなく、Specialを使うため
		/// </summary>
		public Special[] SpecialList
		{
			get
			{
				if (specialList_ != null)
				{
					return specialList_;
				}
				specialList_ = Specials.Select (t => Special.Create (t)).ToArray ();
				return specialList_;
			}
		}
	}

}
