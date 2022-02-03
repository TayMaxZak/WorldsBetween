using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
	[SerializeField]
	public Vector3 velocity;

	void Update()
	{
		transform.Rotate(velocity * Time.deltaTime, Space.Self);
	}
}
