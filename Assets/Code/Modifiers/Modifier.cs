using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Modifier
{
	public string label = "";

	private bool didInit = false;

	public virtual bool Init()
	{
		if (didInit)
			return false;
		didInit = true;

		return true;
	}

	public virtual ModifierOutput OutputAt(float x, float y, float z)
	{
		return new ModifierOutput { passed = false, addOrSub = false };
	}
}

public struct ModifierOutput
{
	public bool passed;
	public bool addOrSub;
}
