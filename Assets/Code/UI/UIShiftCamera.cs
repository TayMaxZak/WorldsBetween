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

	[SerializeField]
	private float wanderAmount = 0.3f;
	[SerializeField]
	private float wanderTimeMin = 2;
	[SerializeField]
	private float wanderTimeMax = 6;
	private Timer wanderTimer;

	[Header("Z Movement")]
	[SerializeField]
	private float zoomDamping = 2f;
	[SerializeField]
	private float zPosInitialZoom = -10f;

	/* Targets for lerping */
	private float zPosTarget = 0;

	private Vector3 xyPosTarget = Vector3.zero;

	private Vector2 xyPosWanderPrev = Vector2.zero;
	private Vector2 xyPosWanderTarget = Vector2.zero;

	private void Awake()
	{
		zPosTarget = transform.localPosition.z;

		// Start lerping camera from inital z thats further away
		transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, zPosInitialZoom);

		wanderTimer = new Timer(SeedlessRandom.NextFloatInRange(wanderTimeMin, wanderTimeMax));
		wanderTimer.currentTime = 0;
	}

	// Update is called once per frame
	void Update()
	{
		// Only start lerping once the cursor is in a normal position
		if (2 * Mathf.Abs(Input.mousePosition.x / Screen.width - 0.5f) < 1 && 2 * Mathf.Abs(Input.mousePosition.y / Screen.height - 0.5f) < 1)
			mouseInWindow = true;
		if (!mouseInWindow)
			return;

		// Clamped pos from input
		Vector2 mousePos = new Vector2(
			xyMaxExtents.x * Mathf.Clamp(2 * (Input.mousePosition.x / Screen.width - 0.5f), -1, 1),
			xyMaxExtents.y * Mathf.Clamp(2 * (Input.mousePosition.y / Screen.height - 0.5f), -1, 1)
		);

		// Randomm smooth wandering
		wanderTimer.Increment(Time.deltaTime);
		if (wanderTimer.Expired())
		{
			// Move wander target
			xyPosWanderPrev = xyPosWanderTarget;
			xyPosWanderTarget = new Vector2(SeedlessRandom.NextFloatInRange(-1, 1), SeedlessRandom.NextFloatInRange(-1, 1));

			// Reset to a new random time
			wanderTimer.maxTime = SeedlessRandom.NextFloatInRange(wanderTimeMin, wanderTimeMax);
			wanderTimer.Reset();
		}
		Vector2 wanderPos = Vector2.Lerp(xyPosWanderPrev, xyPosWanderTarget, 1 - wanderTimer.currentTime / wanderTimer.maxTime);

		// Sum position
		xyPosTarget = new Vector3(mousePos.x + wanderPos.x * wanderAmount, mousePos.y + wanderPos.y * wanderAmount, zPosTarget);

		// Main lerping
		Vector3 newPos = Vector3.Lerp(transform.localPosition, xyPosTarget, Time.deltaTime * xyDamping);
		// Zoom lerping
		newPos.z = Mathf.Lerp(transform.localPosition.z, xyPosTarget.z, Time.deltaTime * zoomDamping);

		// Apply position
		transform.localPosition = newPos;
	}
}
