using UnityEngine;
using System.Collections;

/// <summary>
/// キャラクタを格納するプレファブと対応するクラス
/// </summary>
public class CharacterContainer : MonoBehaviour
{
	public CharacterRenderer Chara;
	public TextMesh Text;
	bool animating;

	public void Start()
	{
		GetComponent<Animation>().Play ("EnemyStay01");
	}

	public void Stay()
	{
		GetComponent<Animation>().Play ("EnemyStay01");
		animating = false;
	}

	public void Animate(string anim)
	{
		Debug.Log ("Animate " + this.name + " " + anim);
		GetComponent<Animation>().Play (anim);
		animating = true;
	}

	public void Update()
	{
		if (animating)
		{
			if (!GetComponent<Animation> ().isPlaying)
			{
				GetComponent<Animation>().Play ("EnemyStay01");
				animating = false;
				Debug.Log ("stop animation");
			}
		}
	}
}
