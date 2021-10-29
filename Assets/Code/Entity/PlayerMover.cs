using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMover : MonoBehaviour
{
	public Camera cam;

	// Position
	[HideInInspector]
	public int worldX, worldY, worldZ; // Coordinates in world space

	private Vector3 lastActualPos;

	// Velocity
	[SerializeField]
	private Vector3 gravity = new Vector3(0, -12, 0);
	private Vector3 velocity;

	[SerializeField]
	private float walkSpeed = 3;
	private Vector3 walkVelocity = new Vector3();

	private Timer moveTickTimer = new Timer(0.05f);

	private bool didInit = false;

	private void Start()
	{
		UpdatePosition();

		cam.transform.parent = null;
	}

	private void Update()
	{
		Chunk chunk;
		if ((chunk = World.GetChunkFor(worldX, worldY, worldZ)) != null && chunk.genStage >= Chunk.GenStage.Lit)
			didInit = true;
		else
			didInit = false;

		if (!didInit)
			return;

		cam.transform.position = Vector3.Lerp(lastActualPos, transform.position, 1 - moveTickTimer.currentTime / moveTickTimer.maxTime) + Vector3.up;

		moveTickTimer.Increment(Time.deltaTime);

		if (moveTickTimer.Expired())
		{
			lastActualPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
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
		worldX = Mathf.FloorToInt(transform.position.x);
		worldY = Mathf.FloorToInt(transform.position.y);
		worldZ = Mathf.FloorToInt(transform.position.z);
	}
}
