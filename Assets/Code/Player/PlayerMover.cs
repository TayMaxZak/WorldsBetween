using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class PlayerMover : MonoBehaviour
{
	[SerializeField]
	public Transform locator;
	[SerializeField]
	public Transform locatorBlock;

	public Camera cam;
	private Vector3 camOffset;

	// Position
	[HideInInspector]
	public int worldX, worldY, worldZ; // Coordinates in world space

	private int lastWorldX, lastWorldY, lastWorldZ; // Coordinates in world space

	private Vector3 lastActualPos;

	// Velocity
	[SerializeField]
	private Vector3 gravity = new Vector3(0, -20, 0);
	private Vector3 velocity;

	[SerializeField]
	private float walkSpeed = 4.5f;
	private float swimSpeed = 9;

	private Vector3 walkVelocity = new Vector3();

	private Timer moveTickTimer = new Timer(0.1f);

	private bool didInit = false;
	
	[System.NonSerialized]
	[HideInInspector]
	public bool realChunk = true;

	//private PointLightSource flashlight = new PointLightSource(2.0f, 1.0f);

	private void Start()
	{
		UpdatePosition();

		camOffset = cam.transform.localPosition;
		cam.transform.parent = null;

		locator.parent = null;
		locatorBlock.parent = null;

		didInit = true;
	}

	private void Update()
	{
		if (Input.GetButtonDown("Quit"))
			Application.Quit();

		MainUpdate();
	}

	private void MainUpdate()
	{
		if (!didInit)
			return;

		cam.transform.position = Vector3.Lerp(lastActualPos, locator.position, 1 - moveTickTimer.currentTime / moveTickTimer.maxTime) + camOffset;

		moveTickTimer.Increment(Time.deltaTime);

		if (moveTickTimer.Expired())
		{
			lastActualPos = new Vector3(locator.position.x, locator.position.y, locator.position.z);
			MoveTick(moveTickTimer.maxTime);

			float offset = 0.5f;
			locatorBlock.position = new Vector3(worldX + offset, worldY + offset, worldZ + offset);

			moveTickTimer.Reset();
		}
	}

	private void MoveTick(float deltaTime)
	{
		UpdatePosition();

		Chunk chunk;
		if ((chunk = World.GetChunkFor(worldX, worldY, worldZ)) != null && chunk.genStage >= Chunk.GenStage.Ready)
			realChunk = true;
		else
			realChunk = false;

		bool underWater = worldY - 0.4f < World.GetWaterHeight() || !realChunk;

		Vector3 velocityVectorArrows = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		walkVelocity = Vector3.ClampMagnitude(velocityVectorArrows, 1) * (!underWater ? walkSpeed : swimSpeed);
		walkVelocity = !underWater ? transform.rotation * walkVelocity : cam.transform.rotation * walkVelocity;

		velocity += gravity * deltaTime;

		float smooth = underWater ? 0.5f : 0.9f;
		velocity.x = Mathf.Lerp(velocity.x, walkVelocity.x, smooth);
		if (underWater)
			velocity.y = Mathf.Lerp(velocity.y, walkVelocity.y, smooth);
		velocity.z = Mathf.Lerp(velocity.z, walkVelocity.z, smooth);

		if (underWater)
			velocity *= 0.85f;

		Block block;

		// Intersection with floor
		block = World.GetBlockFor(worldX, worldY - 1, worldZ);
		if (!block.IsAir() && realChunk)
		{
			velocity.y = 0;

			block = World.GetBlockFor(worldX, worldY, worldZ);

			if (!block.IsAir())
			{
				Move(Vector3.up);
			}
		}

		Move(velocity * deltaTime);
	}

	private void Move(Vector3 delta)
	{
		locator.position += delta;
		UpdatePosition();
	}

	private void UpdatePosition()
	{
		worldX = Mathf.FloorToInt(locator.position.x);
		worldY = Mathf.FloorToInt(locator.position.y);
		worldZ = Mathf.FloorToInt(locator.position.z);

		if (worldX != lastWorldX || worldY != lastWorldY || worldZ != lastWorldZ)
		{
			//flashlight.UpdatePosition(locator.position);
		}

		lastWorldX = worldX;
		lastWorldY = worldY;
		lastWorldZ = worldZ;
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Utils.colorYellow;
		Gizmos.DrawWireCube(locatorBlock.position, Vector3.one);
		Gizmos.color = Utils.colorOrange;
		Gizmos.DrawWireSphere(locator.position, 0.9f);
		Gizmos.color = Utils.colorBlue;
		Gizmos.DrawRay(cam.transform.position - camOffset, Vector3.up * 2);
	}
}
