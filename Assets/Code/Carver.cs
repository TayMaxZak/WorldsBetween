using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Carver : Modifier
{
	public float strength = 1;
	public float range = 5;

	public override float StrengthAt(float x, float y, float z)
	{
		bool inside = range * range > Utils.DistanceSqr(worldX, worldY, worldZ, (int)x, (int)y, (int)z);

		return inside ? strength : 0;
	}
}
