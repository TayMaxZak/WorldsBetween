using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
	[SerializeField]
	private float mouseSensitivity = 100f;

	[SerializeField]
	private Transform playerBody;

	private float xRotation = 0f;

	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;

		float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
		float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

		xRotation -= mouseY;
		xRotation = Mathf.Clamp(xRotation, -86f, 86f);

		playerBody.Rotate(Vector3.up, mouseX);
		transform.rotation = Quaternion.Euler(xRotation, playerBody.rotation.eulerAngles.y, 0f);
	}
}
