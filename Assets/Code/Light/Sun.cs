using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun : MonoBehaviour
{
	[ColorUsageAttribute(false, true)]
	public Color lightColor = Color.white;

	public Bounds sourcePoints;

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = lightColor;
		Gizmos.DrawWireCube(sourcePoints.center, sourcePoints.size);
	}
}
