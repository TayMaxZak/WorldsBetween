using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class PlayerMover : Actor
{
	public Camera cam;

	private float eyeOffset = 0.8f;
	private float swimTiltUp = 16;

	[Header("Vitals Costs")]
	public float velDmgMult = 2;
	public float velDmgLimit = 10;
	[Header("'")]
	public float sprintCost = 10;
	public float holdBreathCost = 2;

	public bool sprinting = false;

	[Header("Speeds")]
	[SerializeField]
	private float slowSpeed = 0.7f;
	[SerializeField]
	private float sprintSpeed = 1.5f;
	[SerializeField]
	private float swimSpeed = 0.3f;
	[SerializeField]
	private float jumpSpeed = 9;

	protected bool walking;
	protected bool eyesUnderWater;

	public override void Init()
	{
		base.Init();

		eyeOffset = cam.transform.localPosition.y;
	}

	public override void UpdateTick(bool isPhysicsTick, float tickDeltaTime, float tickPartialTime)
	{
		if (!didInit || !GameManager.GetFinishedLoading())
			return;

		if (!Player.Instance.vitals.dead && grounded && Input.GetButtonDown("Jump"))
			Jump();

		// Start sprinting if possible
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
		// If pressing inputs and out of water, use stamina. Stop sprinting otherwise
		if (sprinting && isPhysicsTick)
		{
			// Not pressing inputs, or in water
			if (!walking || inWater)
				sprinting = false;
			// On ground, so use stamina. If out of stamina, stop sprinting
			else if (grounded && !Player.Instance.vitals.UseStamina(sprintCost * tickDeltaTime, false, false))
				sprinting = false;
		}

		Vector3 prevVel = velocity;

		base.UpdateTick(isPhysicsTick, tickDeltaTime, tickPartialTime);

		Vector3 newVel = velocity;

		// Velocity damage
		// TODO: Don't deal damage for rapidly increasing speed, only for rapidly decreasing
		float velDif = (newVel - prevVel).magnitude;
		float velDmg = Mathf.Max(0, velDif - velDmgLimit) * velDmgMult;
		Player.Instance.vitals.DealDamage(velDmg);

		eyesUnderWater = blockPosition.y + eyeOffset + waterHeightOffset < World.GetWaterHeight();

		if (isPhysicsTick)
		{
			if (eyesUnderWater)
				Player.Instance.vitals.UseStamina(holdBreathCost * tickDeltaTime, true, false);
		}
	}

	private void Jump()
	{
		velocity += Vector3.up * jumpSpeed;
		grounded = false;
	}

	protected override Vector3 GetWalkVelocity()
	{
		if (Player.Instance.vitals.dead)
		{
			walking = false;
			return Vector3.zero;
		}

		float airborneMult = (!grounded && !inWater) ? 0.1f : 1;

		Vector3 inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
		inputDirection = Vector3.ClampMagnitude(inputDirection, 1);

		// Determine what speed to use
		float currentSpeed = (!inWater ? ((sprinting && grounded) ? sprintSpeed : slowSpeed) : swimSpeed) * airborneMult;

		// Rotate input direction
		Vector3 walkVelocity = !inWater ? (transform.rotation * inputDirection * currentSpeed) : (cam.transform.rotation * Quaternion.Euler(Vector3.right * -swimTiltUp) * inputDirection * currentSpeed);

		walking = walkVelocity.sqrMagnitude > 0;
		return walkVelocity;
	}
}
