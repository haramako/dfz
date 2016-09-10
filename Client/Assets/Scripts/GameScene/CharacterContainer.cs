using UnityEngine;
using System.Collections;

/// <summary>
/// キャラクタを格納するプレファブと対応するクラス
/// </summary>
public class CharacterContainer : MonoBehaviour {
	public CharacterRenderer Chara;

	public void Animate(string anim){
		GetComponent<Animation>().Play (anim);
	}
}
