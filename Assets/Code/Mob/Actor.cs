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
		// Predict next position
		Vector3 testPosition = new Vector3(
			(position.x + testVel.x * deltaTime),
			(position.y + testVel.y * deltaTime),
			(position.z + testVel.z * deltaTime)
		);

		Vector3Int testBlockPosition = new Vector3Int(
			(int)(testPosition.x),
			(int)(testPosition.y),
			(int)(testPosition.z)
		);

		// Get surfaces
		Chunk chunkA = World.GetChunkFor(blockPosition);
		Chunk chunkB = World.GetChunkFor(testBlockPosition);

		HashSet<BlockSurface> surfs = new HashSet<BlockSurface>();
		if (chunkA != null)
			surfs.UnionWith(chunkA.GetSurfaces());
		if (chunkB != null)
			surfs.UnionWith(chunkB.GetSurfaces());

		// Test surfaces
		List<BlockSurface> impacted = new List<BlockSurface>();

		float size = Mathf.Max(0.5f, testVel.magnitude * deltaTime);
		foreach (BlockSurface surf in surfs)
		{
			float closeness = 1 / (position - surf.GetWorldPosition()).magnitude;
			float length = 0.1f + closeness * 0.3321f;

			// Only consider surfaces that face towards velocity
			float normDot = Vector3.Dot(-surf.normal, testVel.normalized);

			if (normDot < 0)
			{
				Debug.DrawRay(surf.GetWorldPosition(), surf.normal * length + SeedlessRandom.RandomPoint(0.01f), Color.red, deltaTime);
				continue;
			}

			Vector3 posAtoSurf = (position - surf.GetWorldPosition());

			Vector3 posAtoBlock = (position - surf.GetBlockWorldPosition());

			Vector3 posBtoSurf = (testPosition - surf.GetWorldPosition());

			Vector3 posAtoPosB = (testPosition - position);

			// Only consider surfaces that face towards us
			float difDotA = Vector3.Dot(posAtoSurf.normalized, surf.normal);
			float difDotB = Vector3.Dot(posBtoSurf.normalized, surf.normal);

			if (Vector3.Dot(posAtoBlock.normalized, surf.normal) < 0)
			{
				Debug.DrawRay(surf.GetWorldPosition(), surf.normal * length + SeedlessRandom.RandomPoint(0.01f), Color.magenta, deltaTime);
				continue;
			}

			float blockSize = 0.5f;
			if (posAtoSurf.sqrMagnitude > size * size && Mathf.Max(Mathf.Abs(posAtoBlock.x), Mathf.Abs(posAtoBlock.y), Mathf.Abs(posAtoBlock.z)) > blockSize + size)
			{
				Debug.DrawRay(surf.GetWorldPosition(), surf.normal * length + SeedlessRandom.RandomPoint(0.01f), Color.gray, deltaTime);
				continue;
			}

			impacted.Add(surf);

			Debug.DrawRay(surf.GetWorldPosition(), surf.normal * length + SeedlessRandom.RandomPoint(0.01f), Color.cyan, deltaTime * 2);
		}

		// Sort impacts
		impacted = impacted.OrderBy(
			x => Vector3.SqrMagnitude(x.GetWorldPosition() - position)
		).ToList();

		foreach (BlockSurface surf in impacted)
		{
			Vector3 reflected = Vector3.Reflect(testVel, surf.normal);
			reflected.Scale(new Vector3(Mathf.Abs(surf.normal.x), Mathf.Abs(surf.normal.y), Mathf.Abs(surf.normal.z)));

			testVel += reflected * 1.1f;

			Debug.DrawRay(surf.GetWorldPosition(), surf.normal * (0.5321f + SeedlessRandom.NextFloat() * 0.2f) + SeedlessRandom.RandomPoint(0.04f), Color.white, deltaTime * 2);

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

	protected void OnDrawGizmos()
	{
		Gizmos.color = Utils.colorYellow;
		Gizmos.DrawWireCube(blockPosition + Vector3.one * 0.5f, Vector3.one);

		Gizmos.color = Utils.colorOrange;
		Gizmos.DrawWireSphere(position, 0.9f);
	}
}
