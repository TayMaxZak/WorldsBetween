using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BlockAttributes
{
	public static BlockAttributes empty = new BlockAttributes();

	private float r;
	private float g;
	private float b;
	private float a;

	public void SetMoss(float value)
	{
		r = value;
	}

	public float GetMoss()
	{
		return r;
	}

	public void SetDamage(float value)
	{
		a = value;
	}

	public float GetDamage()
	{
		return a;
	}
}
