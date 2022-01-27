using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMover : MonoBehaviour
{
	public float mag = 2;
	public float speed = 2;

	private void Update()
	{
		transform.position += transform.rotation * new Vector3(Mathf.Sin(Time.time * speed), Mathf.Cos(Time.time * speed), Mathf.Sin(Time.time * speed)) * mag * Time.deltaTime;
	}
}
