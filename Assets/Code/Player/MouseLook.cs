using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
	public PlayerVitals vitals;

	[SerializeField]
	private float mouseSensitivity = 100;

	[SerializeField]
	private float deadMouseSensitivity = 2;

	[SerializeField]
	private Transform playerBody;

	private float xRotation = 0;

	private float limit = 86f;

	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		float mouseH;
		float mouseV;

		if (!vitals.dead)
		{
			mouseH = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
			mouseV = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
		}
		else
		{
			// Smooth out speed as it approaches goal
			float range = Mathf.Clamp(xRotation, -limit, 0);
			float ratio = Mathf.Clamp01(-range / limit);
			float factor = 1 - ratio;

			// Spin around Y, turn upwards on X
			mouseH = SeedlessRandom.NextFloatInRange(0.6f, 0.7f) * deadMouseSensitivity * factor * Time.deltaTime;
			mouseV = SeedlessRandom.NextFloatInRange(0.8f, 1) * deadMouseSensitivity * factor * Time.deltaTime;
		}

		xRotation -= mouseV;
		xRotation = Mathf.Clamp(xRotation, -limit, limit);

		playerBody.Rotate(Vector3.up, mouseH);
		transform.rotation = Quaternion.Euler(xRotation, playerBody.rotation.eulerAngles.y, 0f);
	}

	public void SetXRotation(float value)
	{
		xRotation = value;
	}
}
