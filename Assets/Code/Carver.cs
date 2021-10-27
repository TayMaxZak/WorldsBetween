using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Carver : MonoBehaviour
{
	[HideInInspector]
	public int worldX, worldY, worldZ; // Coordinates in world space

	public float strength = 1;
	public float range = 5;

	public void UpdatePos()
	{
		worldX = Mathf.RoundToInt(transform.position.x);
		worldY = Mathf.RoundToInt(transform.position.y);
		worldZ = Mathf.RoundToInt(transform.position.z);
	}
}
