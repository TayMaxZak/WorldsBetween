using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Actor : MonoBehaviour
{
	public Vector3 position;
	private Vector3 prevPosition;

	public Vector3Int blockPosition;
	private Vector3Int prevBlockPosition;

	// Velocity
	[SerializeField]
	protected Vector3 velocity;
	[SerializeField]
	private Vector3 input;

	// Hitbox
	[SerializeField]
	private BoxCollider hitbox;

	public bool inWater;

	[SerializeField]
	protected bool grounded = false;

	protected bool didInit = false;

	public bool dead = false;

	private void Awake()
	{
		position = transform.position;
		prevPosition = new Vector3(position.x, position.y, position.z);
	}

	private void Start()
	{
		PhysicsManager.Instance.Register(this);
	}

	public virtual void Init()
	{
		Debug.Log(name + " init");

		UpdateBlockPosition();

		didInit = true;
	}

	public virtual void Tick(float deltaTime, float partialTime, bool physicsTick)
	{
		if (!didInit)
			return;

		// Update prev position for lerping
		if (physicsTick)
			prevPosition = new Vector3(position.x, position.y, position.z);

		// Lerp logic position and visual position
		transform.position = Vector3.Lerp(prevPosition, position, 1 - (partialTime / deltaTime));

		// Physics stuff
		if (physicsTick)
		{
			PhysicsTick(deltaTime, partialTime);

			UpdateBlockPosition();
		}
	}

	public virtual void PhysicsTick(float deltaTime, float partialTime)
	{
		Vector3 prevVelocity = velocity;

		UpdateBlockPosition();

		Chunk chunk;
		bool realChunk =
			(chunk = World.GetChunkFor(blockPosition.x, blockPosition.y, blockPosition.z)) != null
			&& chunk.genStage >= Chunk.GenStage.MakeSurface;

		// Apply water physics
		bool newInWater = blockPosition.y - 0.4f < World.GetWaterHeight() && realChunk;
		//bool newInWater = true;

		// Entered water, break velocity on impact
		if (newInWater && !inWater)
		{
			velocity *= 0.5f;
		}
		inWater = newInWater;

		// Falling
		Vector3 fallVelocity = (inWater ? 0.08f : 1) * PhysicsManager.Instance.gravity * deltaTime;

		grounded = Intersecting(deltaTime, ref fallVelocity);

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

		Move(velocity * deltaTime);

		float friction = surfaceFriction ? 4.5f : 0;

		// Drag
		if (inWater)
			velocity *= 1f - (friction * deltaTime + deltaTime * 1.8f);
		else
			velocity *= 1f - (friction * deltaTime + deltaTime * 0.2f);
	}

	protected virtual Vector3 GetWalkVelocity()
	{
		return input;
	}

	protected bool Intersecting(float deltaTime, ref Vector3 testVel)
	{
		Vector3 edgeOffset = new Vector3(
			SoftAbs(testVel.x) * (hitbox.size.x / 2),
			SoftAbs(testVel.y) * (hitbox.size.y / 2),
			SoftAbs(testVel.z) * (hitbox.size.z / 2)
		);

		Debug.DrawRay(position, edgeOffset, Color.red, 0.1f);

		Vector3 edgePos = position + edgeOffset;

		// Predict next position
		Vector3 testPosition = new Vector3(
			(edgePos.x + testVel.x * deltaTime),
			(edgePos.y + testVel.y * deltaTime),
			(edgePos.z + testVel.z * deltaTime)
		);

		Vector3Int testBlockPosition = new Vector3Int(Mathf.FloorToInt(testPosition.x), Mathf.FloorToInt(testPosition.y), Mathf.FloorToInt(testPosition.z));

		Debug.DrawLine(edgePos, testPosition, Color.green, 0.1f);
		Debug.DrawLine(position, testPosition, Color.magenta, 0.1f);
		Debug.DrawLine(edgePos, testBlockPosition, Color.cyan, 0.1f);
		Debug.DrawLine(testPosition, testBlockPosition + Vector3.one * 0.5f, Color.blue, 0.1f);

		if (!World.GetBlockFor(testBlockPosition).IsAir())
		{
			Vector3 normal = testVel.normalized * -1;
			Vector3 reflected = Vector3.Reflect(testVel, normal);
			reflected.Scale(new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z)));

			testVel += reflected * 1.1f;

			return true;
		}

		return false;
	}

	protected void Move(Vector3 delta)
	{
		position += delta;
		UpdateBlockPosition();
	}

	public void UpdateBlockPosition()
	{
		blockPosition = new Vector3Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));

		if (!blockPosition.Equals(prevBlockPosition))
		{

		}

		prevBlockPosition = new Vector3Int(blockPosition.x, blockPosition.y, blockPosition.z);
	}

	private float SoftAbs(float val)
	{
		return Mathf.Clamp(val * 100, -1, 1);
	}

	protected void OnDrawGizmos()
	{
		Gizmos.color = Utils.colorDarkGrayBlue;
		Gizmos.DrawWireCube(position, hitbox.size);

		Gizmos.color = Utils.colorOrange;
		Gizmos.DrawWireSphere(position, 0.1f);
	}
}
