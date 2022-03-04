using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun : MonoBehaviour
{
	[ColorUsage(false, true)]
	public Color lightColor = Color.white;

	[HideInInspector]
	public Bounds sourcePoints;

	[HideInInspector]
	private Vector3 direction;

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = lightColor;

		Vector3 pos = -transform.forward * 50;

		Gizmos.DrawSphere(pos, 1);

		Gizmos.DrawLine(pos, pos + transform.forward * 20);

		Gizmos.DrawLine(pos + transform.right, pos + transform.right + transform.forward * 10);
		Gizmos.DrawLine(pos - transform.right, pos - transform.right + transform.forward * 10);
		Gizmos.DrawLine(pos + transform.up, pos + transform.up + transform.forward * 10);
		Gizmos.DrawLine(pos - transform.up, pos - transform.up + transform.forward * 10);
	}

	public void OnEnable()
	{
		direction = transform.forward;
	}

	public Vector3 GetDirection()
	{
		return direction;
	}
}
