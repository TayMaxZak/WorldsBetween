using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILookAtCamera : MonoBehaviour
{
	private Camera mainCamera;

	// Start is called before the first frame update
	void Awake()
	{
		mainCamera = Camera.main;
	}

	// Update is called once per frame
	void Update()
	{
		transform.forward = -(mainCamera.transform.position - transform.position).normalized;
	}
}
