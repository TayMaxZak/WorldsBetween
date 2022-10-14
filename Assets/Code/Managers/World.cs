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

	private Dictionary<Vector3Int, List<BlockLight>> blockLights = new Dictionary<Vector3Int, List<BlockLight>>();

	[SerializeField]
	private List<Modifier> modifiers = new List<Modifier>();
	[SerializeField]
	private List<SurfaceShaper> surfaceShapers = new List<SurfaceShaper>();
	private StructureModifier structure;

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
	private int seed = 0;
	private readonly int chunkSize = 16;

	public bool isInfinite = true;

	[Header("World Settings")]
	[SerializeField]
	private GameObject encounterObject;
	private Vector3Int relativeOrigin;
	private Vector3Int pointA;
	private Vector3Int pointB;
	private Vector3 goalPoint;
	private Vector3 encounterPoint;
	public bool hasEncounter;

	//private int baseWorldHeight = 99999;
	//private int waterHeight = 0;
	private int deadFallHeight = -199;

	private WorldProperties worldProperties;

	private BoundsInt worldBounds;

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
		LightEngine.Init();
	}

	public struct WorldProperties
	{
		public enum Surface
		{
			NoTop, // Underground
			SurfaceHeight, // Surface defined by surface height
			NoBottom // Free floating
		}

		public Surface surface;
		public int surfaceHeight;

		public bool hasWater;
		public int waterHeight;

		public float mossChance;
	}

	private void WorldInit()
	{
		// Is there prior seed data?
		PersistentData data = PersistentData.GetInstanceForRead();
		if (data)
			seed = data.GetSeed();
		// Generate a random seed
		else
		{
			if (randomizeSeed)
				seed = SeedlessRandom.NextIntInRange(int.MinValue, int.MaxValue);

			data = PersistentData.GetInstanceForWrite();
			data.SetSeed(seed);
			data.SetDebugMode(true);
		}

		Random.InitState(seed); // TODO: Use separate class for consistent gen RNG

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
		//sunObject.transform.localEulerAngles = new Vector3(90f, 0, 0);
		//sunObject.OnEnable();

		// Set world properties
		worldProperties = new WorldProperties
		{
			// Has surface/sky, or entirely underground
			surface = WorldProperties.Surface.NoTop,
			surfaceHeight = 999/*(int)(Random.value * Random.value * 999)*/,

			hasWater = Random.value < 1 / 4f,
			waterHeight = (int)(1 + Random.value * Random.value * -16),

			mossChance = Random.value * Random.value * Random.value * Random.value * Random.value * Random.value
		};
		if (!worldProperties.hasWater)
			worldProperties.waterHeight = -999;

		// Init water
		waterSystem.SetActive(worldProperties.hasWater);

		// Points A and B
		pointA = Vector3Int.zero;
		pointB = Vector3Int.zero;

		// Modifiers
		MakeModifiers();
		foreach (Modifier mod in modifiers)
			mod.Init();
		foreach (SurfaceShaper shaper in surfaceShapers)
			shaper.Init();

		pointB = structure.furthestRoom.genData.pos;
		encounterPoint = structure.encounterRoom.genData.pos;

		// Enough room for an encounter
		hasEncounter = structure.GetFillPercent() > 0.6f;

		// Determine world bounds
		worldBounds.min = new Vector3Int(
			chunkSize * Mathf.FloorToInt((float)structure.structureBounds.min.x / chunkSize),
			chunkSize * Mathf.FloorToInt((float)structure.structureBounds.min.y / chunkSize),
			chunkSize * Mathf.FloorToInt((float)structure.structureBounds.min.z / chunkSize)
		);
		worldBounds.max = new Vector3Int(
			chunkSize * Mathf.CeilToInt((float)structure.structureBounds.max.x / chunkSize),
			chunkSize * Mathf.CeilToInt((float)structure.structureBounds.max.y / chunkSize),
			chunkSize * Mathf.CeilToInt((float)structure.structureBounds.max.z / chunkSize)
		);

		// Fit static objects to bounds
		WaterFollow(worldBounds.center);
	}

	private void MakeModifiers()
	{
		modifiers.Clear();

		Modifier.Mask fillMask = new Modifier.Mask() { fill = true, replace = false };
		Modifier.Mask replaceMask = new Modifier.Mask() { fill = false, replace = true };
		Modifier.Mask anyMask = new Modifier.Mask() { fill = true, replace = true };

		modifiers.Add(structure = new StructureModifier(50, 100));

		modifiers.Add(new StructureFixer(structure));

		modifiers.Add(new MossAttributor(worldProperties.mossChance, 0.1f, 1));
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

		WorldBuilder.ResetFinders();

		RecalcLight();
	}

	[ContextMenu("Recalculate Light")]
	public void RecalcLight()
	{
		WorldLightAtlas.Instance.ClearAtlas(false);

		LightEngine.Begin();
	}

	public static void RecalculateLight()
	{
		Instance.RecalcLight();
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
			Instance.waterSystem.transform.position = new Vector3(pos.x, Instance.worldProperties.waterHeight, pos.z);
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
			return BlockList.RIGID_BORDER;

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

	public static Color GetLighting(int x, int y, int z)
	{
		Chunk chunk = GetChunk(x, y, z);

		if (chunk == null)
			return Color.black;

		return chunk.GetLighting(
			x - chunk.position.x,
			y - chunk.position.y,
			z - chunk.position.z
		);
	}

	public static Color GetLighting(Vector3Int pos)
	{
		return GetLighting(pos.x, pos.y, pos.z);
	}

	public static void SetLighting(int x, int y, int z, Color c)
	{
		Chunk chunk = GetChunk(x, y, z);

		if (chunk == null || chunk.buildStage == Chunk.BuildStage.Init)
			return;

		chunk.SetLighting(
			x - chunk.position.x,
			y - chunk.position.y,
			z - chunk.position.z,
			c
		);
	}

	public static void SetLighting(Vector3Int pos, Color c)
	{
		SetLighting(pos.x, pos.y, pos.z, c);
	}

	public static BlockAttributes GetAttributes(int x, int y, int z)
	{
		Chunk chunk = GetChunk(x, y, z);

		if (chunk == null)
			return BlockAttributes.empty;

		return chunk.GetAttributes(
			x - chunk.position.x,
			y - chunk.position.y,
			z - chunk.position.z
		);
	}

	public static BlockAttributes GetAttributes(Vector3Int pos)
	{
		return GetAttributes(pos.x, pos.y, pos.z);
	}

	public static void SetAttributes(int x, int y, int z, BlockAttributes c)
	{
		Chunk chunk = GetChunk(x, y, z);

		if (chunk == null || chunk.buildStage == Chunk.BuildStage.Init)
			return;

		chunk.SetAttributes(
			x - chunk.position.x,
			y - chunk.position.y,
			z - chunk.position.z,
			c
		);
	}

	public static void SetAttributes(Vector3Int pos, BlockAttributes a)
	{
		SetAttributes(pos.x, pos.y, pos.z, a);
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

	public static Vector3Int GetHalfwayPoint()
	{
		return Instance.structure.encounterRoom.genData.pos;
	}

	public static void SetGoalPoint(Vector3 pos)
	{
		Instance.goalPoint = pos;
		Shader.SetGlobalVector("GoalPosition", pos);
	}

	public static Vector3 GetGoalPoint()
	{
		return Instance.goalPoint;
	}

	public static bool HasEncounter()
	{
		return Instance.hasEncounter;
	}

	public static void SpawnEncounter()
	{
		Instantiate(Instance.encounterObject, Instance.encounterPoint, Quaternion.identity);
	}

	public static void SetEncounterPoint(Vector3 pos)
	{
		Instance.encounterPoint = pos;
	}

	public static Vector3 GetEncounterPoint()
	{
		return Instance.encounterPoint;
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

	public static void SetLightsAt(Vector3Int pos, List<BlockLight> chunkLights)
	{
		Instance.blockLights[pos] = chunkLights;
	}

	public static Dictionary<Vector3Int, List<BlockLight>> GetAllLights()
	{
		return Instance.blockLights;
	}

	public static bool IsInfinite()
	{
		return Instance.isInfinite;
	}

	public static int GetWaterHeight()
	{
		return Instance.worldProperties.waterHeight;
	}

	public static int GetWorldHeight(Vector3Int input)
	{
		float height = Instance.worldProperties.surfaceHeight;

		foreach (SurfaceShaper shaper in Instance.surfaceShapers)
			height += shaper.GetHeight(input);

		return Mathf.RoundToInt(height);
	}

	//public static int GetWorldSizeScenic()
	//{
	//	return (WorldBuilder.GetGenRangePlayable() + WorldBuilder.GetGenRangeFake()) * 2 * Instance.chunkSize;
	//}

	public static BoundsInt GetWorldBounds()
	{
		return Instance.worldBounds;
	}

	//public static bool Contains(Vector3 pos)
	//{
	//	int extent = WorldBuilder.GetGenRangePlayable() * Instance.chunkSize;

	//	if (Mathf.Abs(pos.x) < extent && Mathf.Abs(pos.y) < extent && Mathf.Abs(pos.z) < extent)
	//		return true;
	//	else
	//		return false;
	//}

	// TODO: Add extension method that lets a BoundsInt check if it contains a Vector3 (float)
	public static bool Contains(Vector3Int pos)
	{
		return Instance.worldBounds.Contains(pos);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Utils.colorBlue;
		Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);

		//Gizmos.color = Color.white;
		//Gizmos.DrawLine(pointA, pointB);
		//Vector3 dif = ((Vector3)(pointA - pointB)).normalized * 2;
		//Gizmos.DrawLine(pointB, pointB + SeedlessRandom.RandomPoint(1) + dif);

		worldBuilder.DrawGizmo();

		//if (structure != null)
		//	structure.DrawGizmo();
	}
}
