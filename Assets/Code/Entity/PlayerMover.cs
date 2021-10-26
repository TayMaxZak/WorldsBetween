using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMover : MonoBehaviour
{
	// Position
	[HideInInspector]
	public int worldX, worldY, worldZ; // Coordinates in world space
	private int lastWorldX, lastWorldY, lastWorldZ;

	// Velocity
	[SerializeField]
	private Vector3 gravity = new Vector3(0, -12, 0);
	private Vector3 velocity;

	[SerializeField]
	private float walkSpeed = 3;
	private Vector3 walkVelocity = new Vector3();

	// Util
	private bool dirty = false;

	private Timer moveTickTimer = new Timer(0.05f);

	private bool didInit = false;

	private void Start()
	{
		UpdatePosition();

		didInit = true;
	}

	private void Update()
	{
		if (!didInit)
			return;

		moveTickTimer.Increment(Time.deltaTime);

		if (moveTickTimer.Expired())
		{
			MoveTick(moveTickTimer.maxTime);

			moveTickTimer.Reset();
		}
	}

	private void MoveTick(float deltaTime)
	{
		Vector3 velocityVectorArrows = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		walkVelocity = Vector3.ClampMagnitude(velocityVectorArrows, 1) * walkSpeed;
		walkVelocity = transform.rotation * walkVelocity;

		velocity.x = walkVelocity.x;
		velocity.z = walkVelocity.z;

		velocity += gravity * deltaTime;

		Block block;

		// Intersection with floor
		block = World.GetBlockFor(worldX, worldY - 1, worldZ);
		if (block.opacity > 127)
		{
			velocity.y = 0;

			block = World.GetBlockFor(worldX, worldY, worldZ);

			if (block.opacity > 127)
			{
				Move(Vector3.up);
			}
		}

		Move(velocity * deltaTime);
	}

	private void Move(Vector3 delta)
	{
		transform.position += delta;
		UpdatePosition();
	}

	private void UpdatePosition()
	{
		worldX = Mathf.RoundToInt(transform.position.x);
		worldY = Mathf.RoundToInt(transform.position.y);
		worldZ = Mathf.RoundToInt(transform.position.z);

		if (worldX != lastWorldX || worldY != lastWorldY || worldZ != lastWorldZ)
			dirty = true;

		lastWorldX = worldX;
		lastWorldY = worldY;
		lastWorldZ = worldZ;
	}
}
