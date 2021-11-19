using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Modifier
{
	public string label = "";

	private bool didInit = false;

	public Vector3Int pos; // Coordinates in world space

	public virtual bool Init()
	{
		if (didInit)
			return false;
		didInit = true;

		return true;
	}

	public virtual float StrengthAt(float x, float y, float z)
	{
		return 1;
	}
}
