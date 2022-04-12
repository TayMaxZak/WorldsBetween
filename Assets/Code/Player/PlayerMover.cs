using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class PlayerMover : Actor
{
	public Camera cam;

	private float eyeOffset = 0.8f;
	private float handOffset = 0.8f;
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
		handOffset = Player.Instance.hand.transform.localPosition.y;
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

	public override void PhysicsTick(float deltaTime, float partialTime)
	{
		UpdateBlockPosition();

		// Apply water physics
		bool newInWater = blockPosition.y + waterHeightOffset < World.GetWaterHeight();
		//bool newInWater = true;

		// Entered water, break Y velocity on impact
		if (newInWater && !inWater)
		{
			velocity.y *= 0.5f;
		}
		inWater = newInWater;

		// Falling
		Vector3 fallVelocity = (inWater ? 0.08f : 1) * PhysicsManager.Instance.gravity * deltaTime;

		grounded = LegCheck(deltaTime, ref fallVelocity);

		bool surfaceFriction = false;
		surfaceFriction |= grounded;

		velocity += fallVelocity;

		Vector3 walkVelocity = GetWalkVelocity();
		if (!dead)
		{
			Intersecting(deltaTime, ref walkVelocity);

			// Applying input velocity
			velocity += walkVelocity;
		}

		surfaceFriction |= Intersecting(deltaTime, ref velocity);

		surfaceFriction |= velocity.y > 0;

		Move(velocity * deltaTime);

		float friction = surfaceFriction ? 4.5f : 0;

		// Drag
		if (inWater)
			velocity *= 1f - (friction * deltaTime + deltaTime * 1.8f);
		else
			velocity *= 1f - (friction * deltaTime + deltaTime * 0.2f);
	}

	protected new bool Intersecting(float deltaTime, ref Vector3 testVel)
	{
		//float eps = 0.001f;

		for (float x = (position.x - hitbox.size.x / 2); x <= (position.x + hitbox.size.x / 2) + 1; x++)
		{
			for (float y = position.y + handOffset; y <= (position.y + hitbox.size.y / 2) + 1; y++)
			{
				for (float z = (position.z - hitbox.size.z / 2); z <= (position.z + hitbox.size.z / 2) + 1; z++)
				{
					float tx = Mathf.Clamp(x, (position.x - hitbox.size.x / 2), (position.x + hitbox.size.x / 2));
					float ty = Mathf.Clamp(y, (position.y + handOffset), (position.y + hitbox.size.y / 2));
					float tz = Mathf.Clamp(z, (position.z - hitbox.size.z / 2), (position.z + hitbox.size.z / 2));

					Vector3 testPos = new Vector3((tx), (ty), (tz));

					BlockCastHit hit = PhysicsManager.BlockCastAxial(testPos, testPos + testVel * deltaTime);

					Vector3 reflected = Vector3.Reflect(testVel, hit.normal);
					reflected.Scale(new Vector3(Mathf.Abs(hit.normal.x), Mathf.Abs(hit.normal.y), Mathf.Abs(hit.normal.z)));
					testVel += reflected;

					if (hit.hit)
						return true;
				}
			}
		}
		return false;
	}

	protected bool LegCheck(float deltaTime, ref Vector3 testVel)
	{
		//float eps = 0.001f;

		for (float x = (position.x - hitbox.size.x / 2); x <= (position.x + hitbox.size.x / 2) + 1; x++)
		{
			float y = (position.y + hitbox.size.y / 2) + handOffset;

			for (float z = (position.z - hitbox.size.z / 2); z <= (position.z + hitbox.size.z / 2) + 1; z++)
			{
				float tx = Mathf.Clamp(x, (position.x - hitbox.size.x / 2), (position.x + hitbox.size.x / 2));
				float ty = Mathf.Clamp(y, (position.y + handOffset), (position.y + hitbox.size.y / 2));
				float tz = Mathf.Clamp(z, (position.z - hitbox.size.z / 2), (position.z + hitbox.size.z / 2));

				Vector3 testPos = new Vector3((tx), (ty), (tz));

				Vector3 offset = Vector3.down * hitbox.size.y + testVel * deltaTime;

				BlockCastHit hit = PhysicsManager.BlockCastAxial(testPos, testPos + offset);

				Vector3 reflected = Vector3.Reflect(Vector3.down + testVel, hit.normal);
				reflected.Scale(new Vector3(Mathf.Abs(hit.normal.x), Mathf.Abs(hit.normal.y), Mathf.Abs(hit.normal.z)));
				testVel += reflected;

				if (hit.hit)
					return true;
			}
		}
		return false;
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
