using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomModifier : Modifier
{
	public Bounds bounds;
	public bool addOrSub = false;

	public override float StrengthAt(float x, float y, float z)
	{
		bool inside = bounds.Contains(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));

		return inside ? 1 : 0;
	}
}
