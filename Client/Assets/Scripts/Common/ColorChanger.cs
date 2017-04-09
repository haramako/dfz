using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorChanger : MonoBehaviour
{

	[SerializeField]
	Color color_ = Color.white;

	bool dirty = true;

	public Color Color
	{
		get { return color_; }
		set
		{
			if (value != color_)
			{
				color_ = value;
				dirty = true;
			}
		}
	}

	void Update()
	{
		OnPreRender ();
	}

	void OnPreRender()
	{
		if (dirty)
		{
			dirty = false;
			foreach (var meshRenderer in GetComponentsInChildren<MeshRenderer>())
			{
				meshRenderer.material.color = color_;
			}
		}
	}

}
