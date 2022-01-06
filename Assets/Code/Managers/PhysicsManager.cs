using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Threading.Tasks;

public partial class PhysicsManager : MonoBehaviour
{
	public static PhysicsManager Instance;

	private List<Actor> actors = new List<Actor>();

	[SerializeField]
	private Timer physicsTickTimer = new Timer(0.2f);
	public float epsilon = 0.1f;

	public bool physicsTicking = false;
	[System.NonSerialized]
	public float tickingDelta;

	public Vector3 gravity = new Vector3(0, -20, 0);

	private void Awake()
	{
		// Ensure singleton
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}
		else
			Instance = this;

		tickingDelta = physicsTickTimer.maxTime;
	}

	private void Update()
	{
		physicsTicking = false;

		physicsTickTimer.Increment(Time.deltaTime);

		if (physicsTickTimer.Expired())
		{
			physicsTicking = true;

			physicsTickTimer.Reset();
		}

		foreach (Actor actor in actors)
			actor.Tick(physicsTickTimer.maxTime, physicsTickTimer.currentTime, physicsTicking);
	}

	public void Register(Actor actor)
	{
		actors.Add(actor);
	}

	public void Activate()
	{
		Debug.Log("Activating all");

		foreach (Actor actor in actors)
			actor.Init();
	}

	public static BlockCastHit BlockCast(Vector3 startPos, Vector3 dir, int steps)
	{
		Vector3Int blockPos = new Vector3Int(Mathf.FloorToInt(startPos.x), Mathf.FloorToInt(startPos.y), Mathf.FloorToInt(startPos.z));
		Vector3 direction = dir;

		float adj = 0.5f;

		for (int i = 1; i <= steps; i++)
		{
			bool occluded = !World.GetBlockFor(
				(int)(blockPos.x + direction.x * i + adj),
				(int)(blockPos.y + direction.y * i + adj),
				(int)(blockPos.z + direction.z * i + adj)
			).IsAir();

			if (occluded)
				return new BlockCastHit(new Vector3Int((int)(blockPos.x + direction.x * i + adj), (int)(blockPos.y + direction.y * i + adj), (int)(blockPos.z + direction.z * i + adj)));
		}

		return new BlockCastHit();
	}

	public static BlockCastHit BlockCast(Vector3 a, Vector3 b)
	{
		Vector3Int blockPos = new Vector3Int(Mathf.FloorToInt(a.x), Mathf.FloorToInt(a.y), Mathf.FloorToInt(a.z));
		Vector3 direction = (b - a).normalized;

		float adj = 0.5f;

		for (int i = 1; i <= (b - a).magnitude; i++)
		{
			bool occluded = !World.GetBlockFor(
				(int)(blockPos.x + direction.x * i + adj),
				(int)(blockPos.y + direction.y * i + adj),
				(int)(blockPos.z + direction.z * i + adj)
			).IsAir();

			if (occluded)
				return new BlockCastHit(new Vector3Int((int)(blockPos.x + direction.x * i + adj), (int)(blockPos.y + direction.y * i + adj), (int)(blockPos.z + direction.z * i + adj)));
		}

		return new BlockCastHit();
	}
}

public struct BlockCastHit
{
	public Vector3Int blockPos;
	public bool hit;

	public BlockCastHit(Vector3Int blockPos)
	{
		this.blockPos = blockPos;
		hit = true;
	}
}
