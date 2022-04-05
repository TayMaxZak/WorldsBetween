using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Threading.Tasks;

public partial class World : MonoBehaviour
{
	private static World Instance;

	[Header("References")]
	[SerializeField]
	private GameObject waterSystem;

	[SerializeField]
	private Sun sunObject;

	private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

	private int realChunkCount = 0;
	private int fakeChunkCount = 0;

	private Dictionary<Vector3Int, List<LightSource>> pointLights = new Dictionary<Vector3Int, List<LightSource>>();

	[SerializeField]
	private List<Modifier> modifiers = new List<Modifier>();

	//private Dictionary<Vector3Int, LinkedList<LightSource>> lightSources = new Dictionary<Vector3Int, LinkedList<LightSource>>();

	[SerializeField]
	private WorldBuilder worldBuilder;
	public static WorldBuilder WorldBuilder;

	[SerializeField]
	private LightEngine lightEngine;
	public static LightEngine LightEngine;

	[Header("General Settings")]
	[SerializeField]
	private bool randomizeSeed = false;
	[SerializeField]
	private long seed = 0;
	[SerializeField]
	private int chunkSize = 8;

	public bool isInfinite = true;

	[Header("World Settings")]
	private Vector3Int relativeOrigin;
	private Vector3Int pointA;
	private Vector3Int pointB;

	private int worldHeight = 99999;
	private int waterHeight = 0;
	private int deadFallHeight = -199;

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

		WorldBuilder = worldBuilder;
		WorldBuilder.Init();

		WorldInit();

		LightEngine = lightEngine;
		LightEngine.Init(sunObject);
	}

	private void WorldInit()
	{
		// Is there prior seed data?
		PersistentData data = PersistentData.GetInstanceForRead();
		if (data)
			seed = data.GetNumericSeed();
		// Generate a random seed
		else if (randomizeSeed)
			seed = SeedlessRandom.NextLongInRange(0, long.MaxValue);

		Debug.Log("seed = " + seed + " vs (int)seed = " + (int)seed + " vs string = " + (data ? data.GetStringSeed() : ""));

		Random.InitState((int)seed); // TODO: Use separate class for consistent gen RNG

		int depth = 0;
		if (data)
			depth = data.GetDepth();

		// Random generation starts here //

		// Sun angle
		//int tiltAmtOptions = 9;
		//int tiltDirOptions = 8;
		//sunObject.transform.eulerAngles = new Vector3(
		//	Mathf.Max(Random.Range(1, tiltAmtOptions - 1) * (90f / tiltAmtOptions), Random.Range(1, tiltAmtOptions - 1) * (90f / tiltAmtOptions)),
		//	Random.Range(0, tiltDirOptions) * (360f / tiltDirOptions),
		//	0
		//);
		sunObject.transform.localEulerAngles = new Vector3(90f, 0, 0);
		sunObject.OnEnable();

		// Water/no water, water height
		bool hasWater = Random.value < 0.25f;
		waterSystem.SetActive(hasWater);
		if (hasWater)
		{
			waterHeight = (int)(Random.value * Random.value * -200) + depth * 5;
			WaterFollow(relativeOrigin);
		}
		else
			waterHeight = -99999;

		// Points A and B
		Vector3 floatA = Random.onUnitSphere * (GetWorldSize() / 2 - 24);
		pointA = new Vector3Int(Mathf.FloorToInt(floatA.x), Mathf.FloorToInt(floatA.y), Mathf.FloorToInt(floatA.z));
		if (pointA.y < 0)
			pointA.y = -pointA.y;
		pointB = -pointA;

		// Modifiers
		MakeModifiers();
		foreach (Modifier mod in modifiers)
			mod.Init();
	}

	private void MakeModifiers()
	{
		modifiers.Clear();

		Modifier.Mask fillMask = new Modifier.Mask() { fill = true, replace = false };
		Modifier.Mask replaceMask = new Modifier.Mask() { fill = false, replace = true };


		// Big cave
		modifiers.Add(new TunnelModifier(Random.Range(4f, 12f), pointA, pointB, 2, new Vector3(0.03f, 0.03f, 0.03f), new Vector3(32, 32, 32), new Vector3(0.02f, 0.02f, 0.02f)));

		// Buildings
		modifiers.Add(new BlockyNoiseModifier(BlockList.ARTIFICAL, fillMask, 0.6f, new Vector3(0.04f, 0.06f, 0.04f),
			0.35f, 3, 16,
			0.25f, new Vector3(0.15f, 0.4f, 0.15f))
		);

		// Pillars
		modifiers.Add(new BlockyNoiseModifier(BlockList.ARTIFICAL, fillMask, 0.55f, new Vector3(0.2f, 0.005f, 0.2f),
			0.35f, 4, 10,
			0.05f, new Vector3(0.15f, 0.15f, 0.15f))
		);

		// Leaning pillars
		modifiers.Add(new BlockyNoiseModifier(BlockList.CRYSTAL, fillMask, 0.51f, new Vector3(0.25f, 0.005f, 0.25f),
			0.35f, 3, 6,
			0.4f, new Vector3(0.2f, 0.04f, 0.2f))
		);

		modifiers.Add(new BlockyNoiseModifier(BlockList.CRYSTAL, fillMask, 0.51f, new Vector3(0.25f, 0.005f, 0.25f),
			0.35f, 3, 6,
			0.6f, new Vector3(0.2f, 0.04f, 0.2f))
		);

		// Meandering tunnel
		modifiers.Add(new TunnelModifier(4, pointA, pointB, 1f, Vector3.one, new Vector3(48, 48, 48), new Vector3(0.01f, 0.01f, 0.01f)));
		
		// Pillars
		modifiers.Add(new BlockyNoiseModifier(BlockList.ARTIFICAL, fillMask, 0.55f, new Vector3(0.1f, 0.005f, 0.1f),
			0.35f, 4, 10,
			0.05f, new Vector3(0.15f, 0.4f, 0.15f))
		);

		modifiers.Add(new BlockyNoiseModifier(BlockList.ARTIFICAL, fillMask, 0.55f, new Vector3(0.1f, 0.1f, 0.1f),
			0.35f, 2, 4,
			0.25f, new Vector3(0.1f, 0.1f, 0.1f))
		);

		// Last resort tunnel
		modifiers.Add(new TunnelModifier(3, pointA, pointB, 1f, Vector3.one, new Vector3(8, 8, 8), new Vector3(0.1f, 0.1f, 0.1f)));

		// Decorators
		modifiers.Add(new Decorator(BlockList.GLOWSHROOMS, BlockList.LUREWORMS, fillMask, 1, 50));
	}

	private void Start()
	{
		// First batch of chunks
		WorldBuilder.StartGen(true);
	}

	private void Update()
	{
		WorldBuilder.ContinueGenerating();
	}

	[ContextMenu("Restart Gen")]
	public async void RestartGen()
	{
		WorldInit();

		await WorldBuilder.EnqueueAllChunks(Chunk.BuildStage.Init);

		// Recalc light after world builder is finished
		while (WorldBuilder.IsGenerating())
			await Task.Delay(20);

		WorldBuilder.ResetSpawnFinder();

		RecalcLight();
	}

	[ContextMenu("Recalculate Light")]
	public async void RecalcLight()
	{
		WorldLightAtlas.Instance.ClearAtlas();

		await LightEngine.Begin();
	}

	public static bool Exists()
	{
		return Instance;
	}

	public static List<Modifier> GetModifiers()
	{
		return Instance.modifiers;
	}

	public static void WaterFollow(Vector3 pos)
	{
		if (Instance.waterSystem)
			Instance.waterSystem.transform.position = new Vector3(pos.x, Instance.waterHeight, pos.z);
	}

	public static Chunk GetChunk(int x, int y, int z)
	{
		float chunkSize = Instance.chunkSize;

		Instance.chunks.TryGetValue(new Vector3Int(
			(x > 0 ? (int)(x / chunkSize) : Mathf.FloorToInt(x / chunkSize)) * (int)chunkSize,
			(y > 0 ? (int)(y / chunkSize) : Mathf.FloorToInt(y / chunkSize)) * (int)chunkSize,
			(z > 0 ? (int)(z / chunkSize) : Mathf.FloorToInt(z / chunkSize)) * (int)chunkSize
		),
		out Chunk chunk);

		return chunk;
	}

	public static Chunk GetChunk(Vector3Int pos)
	{
		return GetChunk(pos.x, pos.y, pos.z);
	}

	public static Block GetBlock(int x, int y, int z)
	{
		if (y < Instance.deadFallHeight)
			return BlockList.NATURAL;

		Chunk chunk = GetChunk(x, y, z);

		if (chunk == null)
			return BlockList.BORDER;

		return chunk.GetBlock(
			x - chunk.position.x,
			y - chunk.position.y,
			z - chunk.position.z
		);
	}

	public static Block GetBlock(Vector3Int pos)
	{
		return GetBlock(pos.x, pos.y, pos.z);
	}

	public static void SetBlock(int x, int y, int z, Block b)
	{
		Chunk chunk = GetChunk(x, y, z);

		if (chunk == null || chunk.buildStage == Chunk.BuildStage.Init)
			return;

		chunk.SetBlock(
			x - chunk.position.x,
			y - chunk.position.y,
			z - chunk.position.z,
			b
		);
	}

	public static void SetBlock(Vector3Int pos, Block b)
	{
		SetBlock(pos.x, pos.y, pos.z, b);
	}

	public static int GetChunkSize()
	{
		return Instance.chunkSize;
	}

	public static Vector3Int GetRelativeOrigin()
	{
		return Instance.relativeOrigin;
	}

	public static Vector3Int GetPointA()
	{
		return Instance.pointA;
	}

	public static Vector3Int GetPointB()
	{
		return Instance.pointB;
	}

	public static void AddChunk(Vector3Int pos, Chunk chunk)
	{
		Instance.chunks.Add(pos, chunk);

		if (chunk.chunkType == Chunk.ChunkType.Close)
			Instance.realChunkCount++;
		else
			Instance.fakeChunkCount++;
	}

	public static int GetRealChunkCount()
	{
		return Instance.realChunkCount;
	}

	public static Dictionary<Vector3Int, Chunk> GetAllChunks()
	{
		return Instance.chunks;
	}

	public static bool IsInfinite()
	{
		return Instance.isInfinite;
	}

	public static int GetWaterHeight()
	{
		return Instance.waterHeight;
	}

	public static int GetWorldHeight()
	{
		return Instance.worldHeight;
	}

	public static int GetWorldSize()
	{
		return WorldBuilder.GetGenRangePlayable() * 2 * Instance.chunkSize;
	}

	public static int GetWorldSizeScenic()
	{
		return (WorldBuilder.GetGenRangePlayable() + WorldBuilder.GetGenRangeFake()) * 2 * Instance.chunkSize;
	}

	public static void SetLightsAt(Vector3Int pos, List<LightSource> chunkLights)
	{
		Instance.pointLights[pos] = chunkLights;
	}

	public static Dictionary<Vector3Int, List<LightSource>> GetAllLights()
	{
		return Instance.pointLights;
	}

	public static bool Contains(Vector3 pos)
	{
		int extent = WorldBuilder.GetGenRangePlayable() * Instance.chunkSize;

		if (Mathf.Abs(pos.x) < extent && Mathf.Abs(pos.y) < extent && Mathf.Abs(pos.z) < extent)
			return true;
		else
			return false;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Utils.colorOrange;
		Gizmos.DrawWireCube(Vector3.zero, 2 * chunkSize * worldBuilder.GetGenRangePlayable() * Vector3.one);

		Gizmos.color = Utils.colorPurple;
		Gizmos.DrawWireCube(Vector3.zero, 2 * chunkSize * (worldBuilder.GetGenRangePlayable() + worldBuilder.GetGenRangeFake()) * Vector3.one);

		Gizmos.color = Color.white;
		Gizmos.DrawLine(pointA, pointB);
		Vector3 dif = ((Vector3)(pointA - pointB)).normalized * 2;
		Gizmos.DrawLine(pointB, pointB + SeedlessRandom.RandomPoint(1) + dif);

		worldBuilder.DrawGizmo();
	}
}
