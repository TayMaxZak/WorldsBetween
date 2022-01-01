using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class PlayerMover : Actor
{
	public PlayerVitals vitals;
	public float gForceLimit = 10;
	public float gForceMult = 2;

	public float swimmingCost = 2;
	public float sprintingCost = 10;

	public bool sprinting = false;

	public Camera cam;
	private Vector3 eyeOffset;

	[SerializeField]
	private float walkSpeed = 0.8f;
	[SerializeField]
	private float swimSpeed = 0.4f;
	[SerializeField]
	private float jumpSpeed = 10;
	[SerializeField]
	private float sprintSpeed = 1.2f;

	private Vector3 jumpVel;

	public bool onRope;

	public bool grabbed;

	public override void Init()
	{
		base.Init();

		eyeOffset = cam.transform.localPosition;
	}

	public override void Tick(float deltaTime, float partialTime, bool physicsTick)
	{
		if (!didInit)
			return;

		if (!vitals.dead && grounded && Input.GetButtonDown("Jump"))
			Jump();

		if (!vitals.dead && Input.GetButtonDown("Sprint"))
		{
			if (sprinting || vitals.currentStamina >= 20)
			{
				if (grounded)
					sprinting = !sprinting;
				else
					sprinting = true;
			}
		}

		base.Tick(deltaTime, partialTime, physicsTick);
	}

	private void Jump()
	{
		jumpVel = Vector3.up * jumpSpeed;
		grounded = false;
	}

	protected override Vector3 GetWalkVelocity()
	{
		Vector3 velocityVectorArrows = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

		Vector3 walkVelocity = Vector3.ClampMagnitude(velocityVectorArrows, 1) * (!inWater ? ((sprinting && grounded) ? sprintSpeed : walkSpeed) : swimSpeed);
		walkVelocity = !inWater ? transform.rotation * walkVelocity : cam.transform.rotation * walkVelocity;

		walkVelocity += jumpVel;
		jumpVel = Vector3.zero;

		return walkVelocity;
	}

	public void AddVelocity(Vector3 vel)
	{
		velocity += vel;
	}

	public void SetVelocity(Vector3 vel)
	{
		velocity = vel;
	}
}
