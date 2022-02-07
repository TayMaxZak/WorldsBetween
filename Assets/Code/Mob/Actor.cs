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
		PhysicsManager.Instance.Register(this);

		position = transform.position;
		prevPosition = new Vector3(position.x, position.y, position.z);
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
		UpdateBlockPosition();

		Chunk chunk;
		bool realChunk =
			(chunk = World.GetChunkFor(blockPosition.x, blockPosition.y, blockPosition.z)) != null
			&& chunk.procStage >= Chunk.ProcStage.MakeMesh;

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
		float offset = 0.5f;
		Vector3 offsetActual = position - blockPosition;
		//float eps = 0.001f;

		for (float x = (position.x - hitbox.size.x / 2); x <= (position.x + hitbox.size.x / 2); x++)
		{
			for (float y = (position.y - hitbox.size.y / 2); y <= (position.y + hitbox.size.y / 2); y++)
			{
				for (float z = (position.z - hitbox.size.z / 2); z <= (position.z + hitbox.size.z / 2); z++)
				{
					float tx = Mathf.Clamp(x + offset, (position.x - hitbox.size.x / 2), (position.x + hitbox.size.x / 2));
					float ty = Mathf.Clamp(y + offset, (position.y - hitbox.size.y / 2), (position.y + hitbox.size.y / 2));
					float tz = Mathf.Clamp(z + offset, (position.z - hitbox.size.z / 2), (position.z + hitbox.size.z / 2));

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

	public void AddVelocity(Vector3 vel)
	{
		velocity += vel;
	}

	public void SetVelocity(Vector3 vel)
	{
		velocity = vel;
	}

	protected void OnDrawGizmos()
	{
		float offset = 0.5f;
		Vector3 offsetActual = position - blockPosition;
		//float eps = 0.001f;

		Gizmos.color = Utils.colorBlue;
		Gizmos.DrawWireCube(position, hitbox.size);

		for (float x = (position.x - hitbox.size.x / 2); x <= (position.x + hitbox.size.x / 2); x++)
		{
			for (float y = (position.y - hitbox.size.y / 2); y <= (position.y + hitbox.size.y / 2); y++)
			{
				for (float z = (position.z - hitbox.size.z / 2); z <= (position.z + hitbox.size.z / 2); z++)
				{
					float tx = Mathf.Clamp(x + offset, (position.x - hitbox.size.x / 2), (position.x + hitbox.size.x / 2));
					float ty = Mathf.Clamp(y + offset, (position.y - hitbox.size.y / 2), (position.y + hitbox.size.y / 2));
					float tz = Mathf.Clamp(z + offset, (position.z - hitbox.size.z / 2), (position.z + hitbox.size.z / 2));

					Vector3 testPos = new Vector3((tx), (ty), (tz));

					Gizmos.color = Utils.colorOrange;
					Gizmos.DrawWireCube(testPos, Vector3.one * 0.95f);
				}
			}
		}
	}
}
