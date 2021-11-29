using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class PlayerMover : MonoBehaviour
{
	[SerializeField]
	private Sound enterWaterSound;

	[SerializeField]
	public Transform locator;
	[SerializeField]
	public Transform locatorBlock;

	[SerializeField]
	public Transform body;

	public Camera cam;
	private Vector3 eyeOffset;

	// Position
	[HideInInspector]
	public int worldX, worldY, worldZ; // Coordinates in world space

	private int lastWorldX, lastWorldY, lastWorldZ; // Coordinates in world space

	private Vector3 lastActualPos;

	// Velocity
	[SerializeField]
	private Vector3 gravity = new Vector3(0, -20, 0);
	[SerializeField]
	private Vector3 velocity;

	[SerializeField]
	private float walkSpeed = 4.5f;
	[SerializeField]
	private float swimSpeed = 7;

	private Vector3 walkVelocity = new Vector3();

	[SerializeField]
	private Timer moveTickTimer = new Timer(0.2f);
	private float epsilon;

	private bool didInit = false;

	private bool underWater;

	private bool grounded = false;
	private Vector3 jumpVel;

	//private PointLightSource flashlight = new PointLightSource(2.0f, 1.0f);

	private void Awake()
	{
		epsilon = moveTickTimer.maxTime * 2;
	}

	private void Start()
	{
		UpdatePosition();

		eyeOffset = cam.transform.localPosition;

		locator.parent = null;
		locatorBlock.parent = null;

		didInit = true;
	}

	private void Update()
	{
		if (Input.GetButtonDown("Quit"))
			Application.Quit();

		if (grounded && Input.GetButtonDown("Jump"))
			Jump();

		MainUpdate();
	}

	private void MainUpdate()
	{
		if (!didInit)
			return;

		body.position = Vector3.up + Vector3.Lerp(lastActualPos, locator.position, 1 - moveTickTimer.currentTime / moveTickTimer.maxTime);

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

	private void Jump()
	{
		jumpVel = Vector3.up * 7;
		grounded = false;
	}

	private void MoveTick(float deltaTime)
	{
		UpdatePosition();

		bool realChunk = true;

		Chunk chunk;
		if ((chunk = World.GetChunkFor(worldX, worldY, worldZ)) != null && chunk.genStage >= Chunk.GenStage.Ready)
			realChunk = true;
		else
			realChunk = false;

		// Apply water effects
		bool newUnderWater = worldY - 0.4f < World.GetWaterHeight() || !realChunk;

		if (realChunk && newUnderWater && !underWater && enterWaterSound)
			AudioManager.PlaySound(enterWaterSound, transform.position);

		underWater = newUnderWater;

		grounded = !World.GetBlockFor(worldX, Mathf.FloorToInt(locator.position.y - 0.45f), worldZ).IsAir() && !underWater;

		// Directional input
		Vector3 velocityVectorArrows = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
		walkVelocity = Vector3.ClampMagnitude(velocityVectorArrows, 1) * (!underWater ? walkSpeed : swimSpeed);
		walkVelocity = !underWater ? body.rotation * walkVelocity : cam.transform.rotation * walkVelocity;

		// Applying input velocity
		float smooth = 1 - (underWater ? 1 - deltaTime * 2 : 1 - deltaTime * 6);
		velocity.x = Mathf.Lerp(velocity.x, walkVelocity.x, smooth);
		if (underWater)
			velocity.y = Mathf.Lerp(velocity.y, walkVelocity.y, smooth);
		velocity.z = Mathf.Lerp(velocity.z, walkVelocity.z, smooth);

		// Falling
		Vector3 fallVelocity = (underWater ? 0.1f : 1) * gravity * deltaTime;

		Intersecting(deltaTime, ref fallVelocity);

		velocity += fallVelocity;

		velocity += jumpVel;
		jumpVel = Vector3.zero;

		// Drag
		if (underWater)
			velocity *= 1f - deltaTime / 2;

		Intersecting(deltaTime, ref velocity);

		Move(velocity * deltaTime);
	}

	private bool Intersecting(float deltaTime, ref Vector3 testVel)
	{
		Block checkBlock;

		bool intersected = false;

		// Intersection with surface
		Vector3Int checkPos = new Vector3Int(Mathf.FloorToInt(locator.position.x + testVel.x * deltaTime),
			Mathf.FloorToInt(locator.position.y + testVel.y * deltaTime),
			Mathf.FloorToInt(locator.position.z + testVel.z * deltaTime));
		checkBlock = World.GetBlockFor(checkPos);

		bool realChunk = checkBlock != Block.empty;

		if (!checkBlock.IsAir() && realChunk)
		{
			intersected = true;

			//checkPos.y += 1;
			//checkBlock = World.GetBlockFor(checkPos);
			//if (!checkBlock.IsAir() && realChunk)
			//{
			//	intersected = true;
			//}
		}

		// Repel
		Vector3 normal = new Vector3Int(worldX, worldY, worldZ) - checkPos;
		normal = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));

		if (intersected)
		{
			if (testVel.sqrMagnitude * deltaTime > epsilon)
				testVel += Vector3.Scale(testVel, -1.05f * normal);
			else
				testVel += Vector3.Scale(testVel, -normal);
		}

		return intersected;
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
			// Dirty
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
		Gizmos.DrawRay(body.position - Vector3.up, Vector3.up * 1.5f);


		Gizmos.color = Color.red;

		float offset = 0.5f;
		float deltaTime = moveTickTimer.maxTime;
		Gizmos.DrawWireCube(new Vector3(
				offset + Mathf.FloorToInt(locator.position.x + velocity.x * deltaTime),
				offset + Mathf.FloorToInt(locator.position.y + velocity.y * deltaTime),
				offset + Mathf.FloorToInt(locator.position.z + velocity.z * deltaTime)
			),
			Vector3.one);
	}
}
