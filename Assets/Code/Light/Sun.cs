using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun : MonoBehaviour
{
	[ColorUsage(false, true)]
	public Color lightColor = Color.white;

	[HideInInspector]
	public Bounds sourcePoints;

	private Vector3 forward;
	private Vector3 right;
	private Vector3 up;

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
		forward = transform.forward;
		right = transform.right;
		up = transform.up;

		Shader.SetGlobalVector("SunDirection", forward);
		Shader.SetGlobalColor("SunColor", lightColor);
	}

	public Vector3 GetDirection()
	{
		return forward;
	}

	public Vector3 GetRight()
	{
		return right;
	}

	public Vector3 GetUp()
	{
		return up;
	}
}
