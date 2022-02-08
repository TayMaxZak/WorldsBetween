using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIShiftCamera : MonoBehaviour
{
	[SerializeField]
	private Vector2 maxExtent = new Vector2(1,1);

	private float zPos;

	private void Awake()
	{
		zPos = transform.localPosition.z;
	}

	// Update is called once per frame
	void Update()
	{
		Vector2 mousePos = new Vector2(
			maxExtent.x * Mathf.Clamp(2 * (Input.mousePosition.x / Screen.width - 0.5f), -1, 1),
			maxExtent.y * Mathf.Clamp(2 * (Input.mousePosition.y / Screen.height - 0.5f), -1, 1)
		);
		transform.localPosition = new Vector3(mousePos.x, mousePos.y, zPos);
	}
}
