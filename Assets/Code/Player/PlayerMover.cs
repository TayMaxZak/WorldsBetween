using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class PlayerMover : Actor
{
	public Incursometer incursometer;
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

		if (!Player.Instance.vitals.dead && grounded && Input.GetButtonDown("Jump"))
			Jump();

		if (!Player.Instance.vitals.dead && Input.GetButtonDown("Sprint"))
		{
			if (sprinting || Player.Instance.vitals.currentStamina >= 20)
			{
				if (grounded)
					sprinting = !sprinting;
				else
					sprinting = true;
			}
		}

		Vector3 prevVel = velocity;

		base.Tick(deltaTime, partialTime, physicsTick);

		Vector3 newVel = velocity;

		// Velocity damage
		// TODO: Don't deal damage for rapidly increasing speed, only for rapidly decreasing
		float velDif = (newVel - prevVel).magnitude;
		float velDmg = Mathf.Max(0, velDif - gForceLimit) * gForceMult;
		Player.Instance.vitals.DealDamage(velDmg);

		//if (incursometer.flashlightOn)
		//{
		//	Transform t = incursometer.flashlight.transform;

		//	if (physicsTick)
		//	{
		//		BlockCastHit hit = PhysicsManager.BlockCastAxial(t.position, t.position + t.forward * flashlightLength);
		//		newFlashB = hit.hit ? (t.position + t.forward * Vector3.Distance(t.position, hit.worldPos)) : (t.position + t.forward * flashlightLength);
		//	}

		//	Shader.SetGlobalVector("FlashlightA", t.position);

		//	prevFlashB = Vector3.Lerp(prevFlashB, newFlashB, partialTime);
		//	Shader.SetGlobalVector("FlashlightB", prevFlashB);
		//}
	}

	private void Jump()
	{
		velocity += Vector3.up * jumpSpeed;
		grounded = false;
	}

	protected override Vector3 GetWalkVelocity()
	{
		if (!grounded)
			return Vector3.zero;

		Vector3 velocityVectorArrows = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

		Vector3 walkVelocity = Vector3.ClampMagnitude(velocityVectorArrows, 1) * (!inWater ? ((sprinting && grounded) ? sprintSpeed : walkSpeed) : swimSpeed);
		walkVelocity = !inWater ? transform.rotation * walkVelocity : cam.transform.rotation * walkVelocity;

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
