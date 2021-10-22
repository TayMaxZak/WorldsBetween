using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class LightSource : MonoBehaviour
{
	[HideInInspector]
	public int worldX, worldY, worldZ; // Coordinates in world space

	public float brightness = 1; // From 0 to 1
	public float colorTemp = 0; // From -1 to 1

	public void UpdatePos()
	{
		worldX = Mathf.RoundToInt(transform.position.x);
		worldY = Mathf.RoundToInt(transform.position.y);
		worldZ = Mathf.RoundToInt(transform.position.z);
	}
}
