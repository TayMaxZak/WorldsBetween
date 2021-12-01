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
	private float walkSpeed = 0.8f;
	[SerializeField]
	private float swimSpeed = 0.4f;
	[SerializeField]
	private float jumpSpeed = 10;

	[SerializeField]
	private Timer moveTickTimer = new Timer(0.2f);
	private float epsilon;

	private bool didInit = false;

	private bool underWater;

	private bool grounded = false;
	private Vector3 jumpVel;

	public bool ticking = false;
	public float tickingDelta;

	public bool onRope;

	//private PointLightSource flashlight = new PointLightSource(2.0f, 1.0f);

	private void Awake()
	{
		epsilon = moveTickTimer.maxTime * 2;
		tickingDelta = moveTickTimer.maxTime;
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

		if (Input.GetButtonDown("Vitals"))
			UIManager.SetWatchRaised(true);
		else if (Input.GetButtonUp("Vitals"))
			UIManager.SetWatchRaised(false);

		MainUpdate();
	}

	private void MainUpdate()
	{
		if (!didInit)
			return;

		ticking = false;

		body.position = Vector3.up + Vector3.Lerp(lastActualPos, locator.position, 1 - moveTickTimer.currentTime / moveTickTimer.maxTime);

		moveTickTimer.Increment(Time.deltaTime);

		if (moveTickTimer.Expired())
		{
			ticking = true;

			lastActualPos = new Vector3(locator.position.x, locator.position.y, locator.position.z);
			MoveTick(moveTickTimer.maxTime);

			float offset = 0.5f;
			locatorBlock.position = new Vector3(worldX + offset, worldY + offset, worldZ + offset);

			moveTickTimer.Reset();
		}
	}

	private void Jump()
	{
		jumpVel = Vector3.up * jumpSpeed;
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

		if (realChunk && newUnderWater && !underWater)
		{
			if (enterWaterSound)
				AudioManager.PlaySound(enterWaterSound, transform.position);

			velocity *= 0.5f;
		}

		underWater = newUnderWater;

		//grounded = !World.GetBlockFor(worldX, Mathf.FloorToInt(locator.position.y - 0.45f), worldZ).IsAir() && !underWater;

		bool useFriction = false;

		// Falling
		Vector3 fallVelocity = (underWater ? 0.08f : 1) * gravity * deltaTime;

		grounded = Intersecting(deltaTime, ref fallVelocity);
		useFriction |= grounded;

		velocity += fallVelocity;

		velocity += jumpVel;
		jumpVel = Vector3.zero;

		// Directional input
		Vector3 velocityVectorArrows = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
		Vector3 walkVelocity = Vector3.ClampMagnitude(velocityVectorArrows, 1) * (!underWater ? walkSpeed : swimSpeed);
		walkVelocity = !underWater ? body.rotation * walkVelocity : cam.transform.rotation * walkVelocity;

		if (grounded || underWater)
		{
			Intersecting(deltaTime, ref walkVelocity);

			// Applying input velocity
			velocity += walkVelocity;
		}

		useFriction |= Intersecting(deltaTime, ref velocity);

		Move(velocity * deltaTime);

		float friction = useFriction ? 4.5f : 0;
		friction += onRope ? 0.25f : 0;

		// Drag
		if (underWater)
			velocity *= 1f - (friction * deltaTime + deltaTime * 1.8f);
		else
			velocity *= 1f - (friction * deltaTime + deltaTime * 0.2f);
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

	public void AddVelocity(Vector3 vel)
	{
		velocity += vel;
	}

	public void SetVelocity(Vector3 vel)
	{
		velocity = vel;
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
