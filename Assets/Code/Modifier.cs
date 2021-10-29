using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Modifier : MonoBehaviour
{
	private bool didInit = false;

	[HideInInspector]
	public int worldX, worldY, worldZ; // Coordinates in world space

	private void Awake()
	{
		World.RegisterModifier(this);

		Init();
	}

	protected virtual bool Init()
	{
		if (didInit)
			return false;
		didInit = true;

		worldX = Mathf.RoundToInt(transform.position.x);
		worldY = Mathf.RoundToInt(transform.position.y);
		worldZ = Mathf.RoundToInt(transform.position.z);

		return true;
	}

	public virtual float StrengthAt(float x, float y, float z)
	{
		return 1;
	}
}
