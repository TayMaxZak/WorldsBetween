using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun : MonoBehaviour
{
	[ColorUsageAttribute(false, true)]
	public Color lightColor = Color.white;

	public Bounds sourcePoints;

	public Vector3 direction;

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = lightColor;
		Gizmos.DrawWireCube(sourcePoints.center, sourcePoints.size);
	}

	private void Awake()
	{
		direction = transform.forward;
	}
}
