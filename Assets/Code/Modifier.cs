using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Modifier : MonoBehaviour
{
	[HideInInspector]
	public int worldX, worldY, worldZ; // Coordinates in world space

	private void Awake()
	{
		World.RegisterModifier(this);
	}

	public virtual void Init()
	{
		worldX = Mathf.RoundToInt(transform.position.x);
		worldY = Mathf.RoundToInt(transform.position.y);
		worldZ = Mathf.RoundToInt(transform.position.z);
	}

	public virtual float StrengthAt(float x, float y, float z)
	{
		return 1;
	}
}
