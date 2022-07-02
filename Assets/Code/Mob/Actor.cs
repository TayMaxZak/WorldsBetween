using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Actor : MonoBehaviour
{
	[HideInInspector]
	public Vector3 position;
	private Vector3 prevPosition;

	// Velocity
	protected Vector3 velocity;
	protected Vector3 fallVelocity;
	protected Vector3 inputVelocity;

	protected bool inWater;
	protected float waterHeightOffset = -0.5f;

	protected bool didInit = false;

	public bool dead = false;

	protected virtual void Awake()
	{
		PhysicsManager.Instance.Register(this);
	}

	public virtual void Init()
	{
		Debug.Log(name + " init");

		position = transform.position;
		prevPosition = new Vector3(position.x, position.y, position.z);
		//UpdateBlockPosition();

		didInit = true;
	}

	public virtual void UpdateTick(bool isPhysicsTick, float tickDeltaTime, float tickPartialTime)
	{
		if (!didInit)
			return;

		// Update prev position for lerping
		if (isPhysicsTick)
			prevPosition = new Vector3(position.x, position.y, position.z);

		// Lerp logic position and visual position
		transform.position = Vector3.Lerp(prevPosition, position, 1 - (tickPartialTime / tickDeltaTime));
		//transform.position = position;

		// Physics stuff
		if (isPhysicsTick)
		{
			PhysicsTick(tickDeltaTime);

			//UpdateBlockPosition();
		}
	}

	public virtual void PhysicsTick(float deltaTime)
	{
		//UpdateBlockPosition();

		// Falling
		Vector3 fallAccel = PhysicsManager.Instance.gravity * deltaTime;

		fallVelocity += fallAccel;

		Move(fallVelocity * deltaTime);
	}

	protected virtual void Move(Vector3 velocity)
	{
		position += velocity;
	}

	public void AddVelocity(Vector3 vel)
	{
		velocity += vel;
	}

	public void SetVelocity(Vector3 vel)
	{
		velocity = vel;
	}

	protected void OnDrawGizmosSelected()
	{
		Gizmos.color = Utils.colorBlue;
		if (Application.isPlaying)
			Gizmos.DrawWireCube(position, Vector3.one);
		else
			Gizmos.DrawWireCube(transform.position, Vector3.one);
	}
}
