using System.Collections;
using System.Collections.Generic;
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

	protected bool grounded = false;

	protected bool didInit = false;

	public bool dead = false;

	private void Awake()
	{
		position = transform.position;
		prevPosition = new Vector3(position.x, position.y, position.z);

		UpdatePosition();
	}

	private void Start()
	{
		PhysicsManager.Instance.Register(this);
	}

	public virtual void Init()
	{
		didInit = true;
	}

	public virtual void Tick(float deltaTime, float partialTime, bool physicsTick)
	{
		if (!didInit)
			return;

		if (physicsTick)
			prevPosition = new Vector3(position.x, position.y, position.z);

		transform.position = Vector3.Lerp(prevPosition, position, 1 - (partialTime / deltaTime));

		if (!physicsTick)
			return;

		prevPosition = new Vector3(position.x, position.y, position.z);

		PhysicsTick(deltaTime, partialTime);

		UpdatePosition();
	}

	public virtual void PhysicsTick(float deltaTime, float partialTime)
	{
		Vector3 prevVelocity = velocity;

		UpdatePosition();

		bool realChunk = true;

		Chunk chunk;
		if ((chunk = World.GetChunkFor(blockPosition.x, blockPosition.y, blockPosition.z)) != null && chunk.genStage >= Chunk.GenStage.CalcLight)
			realChunk = true;
		else
			realChunk = false;

		// Apply water effects
		bool newInWater = blockPosition.y - 0.4f < World.GetWaterHeight() && realChunk;

		// Enter water
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
		Block checkBlock;

		bool intersected = false;

		// Intersection with surface
		Vector3Int checkPos = new Vector3Int(Mathf.FloorToInt(position.x + testVel.x * deltaTime),
			Mathf.FloorToInt(position.y + testVel.y * deltaTime),
			Mathf.FloorToInt(position.z + testVel.z * deltaTime));
		checkBlock = World.GetBlockFor(checkPos);

		bool realChunk = checkBlock != Block.empty;

		if (!checkBlock.IsAir() && realChunk)
		{
			intersected = true;
		}

		// Repel
		Vector3 normal = blockPosition - checkPos;
		normal = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));

		if (intersected)
		{
			testVel += Vector3.Scale(testVel, -normal);
		}

		return intersected;
	}

	protected void Move(Vector3 delta)
	{
		position += delta;
		UpdatePosition();
	}

	protected void UpdatePosition()
	{
		blockPosition = new Vector3Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y), Mathf.FloorToInt(position.z));

		if (!blockPosition.Equals(prevBlockPosition))
		{
			// Dirty
		}

		prevBlockPosition = new Vector3Int(blockPosition.x, blockPosition.y, blockPosition.z);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Utils.colorYellow;
		Gizmos.DrawWireCube(blockPosition + Vector3.one * 0.5f, Vector3.one);

		Gizmos.color = Utils.colorOrange;
		Gizmos.DrawWireSphere(position, 0.9f);

		Gizmos.color = Utils.colorBlue;
		Gizmos.DrawRay(transform.position - Vector3.up, Vector3.up * 1.5f);
	}
}
