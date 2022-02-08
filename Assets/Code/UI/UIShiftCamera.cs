using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIShiftCamera : MonoBehaviour
{
	[Header("XY Movement")]
	[SerializeField]
	private Vector2 xyMaxExtents = new Vector2(1,1);
	[SerializeField]
	private float xyDamping = 8f;
	private bool mouseInWindow = false;

	[Header("Z Movement")]
	[SerializeField]
	private float zoomDamping = 2f;
	[SerializeField]
	private float zPosInitialZoom = -10f;

	/* Targets for lerping */
	private float zPosTarget = 0;

	private Vector3 xyPosTarget = Vector3.zero;

	private void Awake()
	{
		zPosTarget = transform.localPosition.z;

		// Start lerping camera from inital z thats further away
		transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, zPosInitialZoom);
	}

	// Update is called once per frame
	void Update()
	{
		// Only start lerping once the cursor is in a normal position
		if (2 * Mathf.Abs(Input.mousePosition.x / Screen.width - 0.5f) < 1 || 2 * Mathf.Abs(Input.mousePosition.y / Screen.height - 0.5f) < 1)
			mouseInWindow = true;
		if (!mouseInWindow)
			return;

		Vector2 mousePos = new Vector2(
			xyMaxExtents.x * Mathf.Clamp(2 * (Input.mousePosition.x / Screen.width - 0.5f), -1, 1),
			xyMaxExtents.y * Mathf.Clamp(2 * (Input.mousePosition.y / Screen.height - 0.5f), -1, 1)
		);

		xyPosTarget = new Vector3(mousePos.x, mousePos.y, zPosTarget);

		// Main lerping
		Vector3 newPos = Vector3.Lerp(transform.localPosition, xyPosTarget, Time.deltaTime * xyDamping);

		// Zoom lerping
		newPos.z = Mathf.Lerp(transform.localPosition.z, xyPosTarget.z, Time.deltaTime * zoomDamping);

		transform.localPosition = newPos;
	}
}
