using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Threading.Tasks;

public partial class PhysicsManager : MonoBehaviour
{
	public static PhysicsManager Instance;

	[SerializeField]
	private List<Actor> actors = new List<Actor>();

	[SerializeField]
	private Timer physicsTickTimer = new Timer(0.2f);
	public float epsilon = 0.1f;

	public bool ticking = false;
	[System.NonSerialized]
	public float tickingDelta;

	public Vector3 gravity = new Vector3(0, -20, 0);

	public Color randomColor = new Color(1, 1, 1, 1);

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
		ticking = false;

		physicsTickTimer.Increment(Time.deltaTime);

		if (physicsTickTimer.Expired())
		{
			ticking = true;

			physicsTickTimer.Reset();
		}


		randomColor = Color.HSVToRGB(SeedlessRandom.NextFloat(), 1, 1);

		foreach (Actor actor in actors)
			actor.Tick(physicsTickTimer.maxTime, physicsTickTimer.currentTime, ticking);
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

	private enum AxialOrder
	{
		None,
		XYZ,
		XZY,
		YXZ,
		YZX,
		ZXY,
		ZYX
	}

	public static BlockCastHit BlockCastAxial(Vector3 a, Vector3 b)
	{
		// Find both ends of check
		Vector3Int blockPosA = new Vector3Int(Mathf.FloorToInt(a.x), Mathf.FloorToInt(a.y), Mathf.FloorToInt(a.z));
		Vector3Int blockPosB = new Vector3Int(Mathf.FloorToInt(b.x), Mathf.FloorToInt(b.y), Mathf.FloorToInt(b.z));
		Vector3 diff = (b - a);

		AxialOrder order = FindAxialOrder(diff);

		// Cursor
		Vector3 testPos = a;
		Vector3Int testBlockPos = blockPosA;

		// How to move the cursor for each axis on each step
		Vector3 stepsByAxis = diff.Equals(Vector3.zero) ? Vector3.zero : (new Vector3(diff.x, diff.y, diff.z) / Mathf.Max(1, Mathf.Abs(diff.x), Mathf.Abs(diff.y), Mathf.Abs(diff.z)));

		float i = 0;

		Vector3 lastDir = Vector3.one;

		Debug.DrawLine(a, a + SeedlessRandom.RandomPoint(0.08f), Instance.randomColor, 0.5f);
		Debug.DrawLine(a, b, Instance.randomColor, 0.5f);

		while (i < 100)
		{
			string strOrder = System.Enum.GetName(typeof(AxialOrder), order);

			i++;

			for (int j = 0; j < 3; j++)
			{
				char toCmp = strOrder[j];

				if (toCmp == 'X')
				{
					testPos.x += stepsByAxis.x;
					lastDir = new Vector3(Utils.SoftSign(stepsByAxis.x), 0, 0);
				}
				else if (toCmp == 'Y')
				{
					testPos.y += stepsByAxis.y;
					lastDir = new Vector3(0, Utils.SoftSign(stepsByAxis.y), 0);
				}
				else if (toCmp == 'Z')
				{
					testPos.z += stepsByAxis.z;
					lastDir = new Vector3(0, 0, Utils.SoftSign(stepsByAxis.z));
				}

				Vector3 oldBP = testBlockPos;
				testBlockPos = new Vector3Int(Utils.ToInt(testPos.x), Utils.ToInt(testPos.y), Utils.ToInt(testPos.z));

				bool occluded = !World.GetBlockFor(testBlockPos).IsAir();

				Debug.DrawLine(oldBP + Vector3.one * 0.5f, testBlockPos + Vector3.one * 0.5f, occluded ? Color.green : Instance.randomColor, occluded ? 2 : 1);

				if (occluded)
				{
					return new BlockCastHit(testBlockPos, -lastDir);
				}

				if (Vector3.SqrMagnitude(blockPosA - testBlockPos) >= Vector3.SqrMagnitude(blockPosA - blockPosB))
				{
					return new BlockCastHit();
				}
			}
		}

		return new BlockCastHit();
	}

	private static AxialOrder FindAxialOrder(Vector3 diff)
	{
		Vector3 abs = new Vector3(Mathf.Abs(diff.x), Mathf.Abs(diff.y), Mathf.Abs(diff.z));

		float xGy = abs.x.CompareTo(abs.y);
		float xGz = abs.x.CompareTo(abs.z);

		float yGx = abs.y.CompareTo(abs.x);
		float yGz = abs.y.CompareTo(abs.z);

		float zGx = abs.z.CompareTo(abs.x);
		float zGy = abs.z.CompareTo(abs.y);

		if (xGy > 0 && xGz > 0)
		{
			if (yGz > 0)
				return AxialOrder.XYZ;
			else
				return AxialOrder.XZY;
		}
		else if (yGx > 0 && yGz > 0)
		{
			if (xGz > 0)
				return AxialOrder.YXZ;
			else
				return AxialOrder.YZX;
		}
		else if (zGx > 0 && zGy > 0)
		{
			if (xGy > 0)
				return AxialOrder.ZXY;
			else
				return AxialOrder.ZYX;
		}

		return AxialOrder.None;
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

	public BlockCastHit(Vector3Int blockPos, Vector3 normal)
	{
		this.blockPos = blockPos;
		this.worldPos = blockPos;
		this.normal = normal;
		hit = true;
	}
}
