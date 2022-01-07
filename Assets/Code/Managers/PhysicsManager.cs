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

	public static BlockCastHit BlockCastAxial(Vector3 a, Vector3 b)
	{
		// Find both ends of check
		Vector3Int blockPosA = new Vector3Int(Mathf.FloorToInt(a.x), Mathf.FloorToInt(a.y), Mathf.FloorToInt(a.z));
		Vector3Int blockPosB = new Vector3Int(Mathf.FloorToInt(b.x), Mathf.FloorToInt(b.y), Mathf.FloorToInt(b.z));
		Vector3 diff = (blockPosB - blockPosA);

		// Cursor
		Vector3 testPos = a;
		Vector3Int testBlockPos = blockPosA;

		// How many times to move the cursor for each axis on each step
		Vector3 stepsByAxis = diff / (float)Mathf.Max(1, Mathf.Min(Mathf.Abs(diff.x), Mathf.Abs(diff.y), Mathf.Abs(diff.z)));
		stepsByAxis = new Vector3(Mathf.Abs(stepsByAxis.x), Mathf.Abs(stepsByAxis.y), Mathf.Abs(stepsByAxis.z));
		Vector3 curSteps = stepsByAxis;

		float i = 0;

		Debug.DrawLine(a, b, Color.magenta, 10);
		while (i < 100)
		{
			// Reset
			if (curSteps.x <= 0 && curSteps.y <= 0 && curSteps.z <= 0)
			{
				curSteps += stepsByAxis;
			}
			Debug.Log("i " + i + ": " + stepsByAxis);

			Vector3 oldTP = testPos;
			Vector3 oldTBP = testBlockPos;

			// Move the test pos over
			float max = Mathf.Max(curSteps.x, curSteps.y, curSteps.z);
			if (curSteps.x == max)
			{
				curSteps.x -= 1;
				testPos.x += Utils.SoftSign(diff.x);
			}
			else if (curSteps.y == max)
			{
				curSteps.y -= 1;
				testPos.y += Utils.SoftSign(diff.y);
			}
			else if (curSteps.z == max)
			{
				curSteps.z -= 1;
				testPos.z += Utils.SoftSign(diff.z);
			}

			i++;

			testBlockPos = new Vector3Int(
				Mathf.FloorToInt(testPos.x),
				Mathf.FloorToInt(testPos.y),
				Mathf.FloorToInt(testPos.z)
			);
			if (i < 200)
			{
				Debug.DrawLine(oldTP, testPos, Color.cyan, 10);
				Debug.DrawLine(oldTBP + Vector3.one * 0.5f, testBlockPos + Vector3.one * 0.5f, Utils.colorBlue, 10);
			}

			bool occluded = !World.GetBlockFor(testBlockPos).IsAir();

			if (occluded)
			{
				Debug.Log(i);
				return new BlockCastHit(testBlockPos);
			}

			if (testBlockPos.Equals(blockPosB))
				break;
		}

		Debug.Log(i);
		return new BlockCastHit();
	}
}

public struct BlockCastHit
{
	public Vector3Int blockPos;
	public Vector3 worldPos;
	public Vector3 normal;
	public bool hit;

	public BlockCastHit(Vector3Int blockPos)
	{
		this.blockPos = blockPos;
		this.worldPos = blockPos;
		this.normal = Vector3.one;
		hit = true;
	}
}
