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
	private Vector3Int relativeOrigin;

	[SerializeField]
	private GameObject waterSystem;

	[SerializeField]
	private Sun sunObject;

	private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
	private int realChunkCount = 0;
	private int fakeChunkCount = 0;

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
	[SerializeField]
	private int waterHeight = 0;
	[SerializeField]
	private int deadFallHeight = -999;

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


		// Random generation starts here //

		// Sun angle
		int tiltAmtOptions = 9;
		int tiltDirOptions = 8;
		sunObject.transform.eulerAngles = new Vector3(
			Mathf.Max(Random.Range(1, tiltAmtOptions - 1) * (90f / tiltAmtOptions), Random.Range(1, tiltAmtOptions - 1) * (90f / tiltAmtOptions)),
			Random.Range(0, tiltDirOptions) * (360f / tiltDirOptions),
			0
		);
		//sunObject.transform.localEulerAngles = new Vector3(70f, 67.5f, 0);
		sunObject.OnEnable();

		// Water/no water, water height
		bool hasWater = Random.value < 0.1f;
		waterSystem.SetActive(hasWater);
		if (hasWater)
		{
			waterHeight = (int)(Random.value * Random.value * Random.value * -200);
			WaterFollow(relativeOrigin);
		}
		else
			waterHeight = -99999;

		// Modifiers
		MakeModifiers();
		foreach (Modifier mod in modifiers)
			mod.Init();
	}

	private void MakeModifiers()
	{
		modifiers.Clear();

		// Main boxes
		modifiers.Add(new BlockyNoiseModifier(false, 0.9f, new Vector3(0.04f, 0.03f, 0.04f),
			0.35f, 3, 16,
			0.2f, new Vector3(0.15f, 0.4f, 0.15f)));

		// Shards
		modifiers.Add(new BlockyNoiseModifier(true, 0.65f, new Vector3(0.03f, 0.01f, 0.03f),
			0.25f, 5, 10,
			0.15f, new Vector3(0.3f, 0.0f, 0.3f)));

		// Etches
		modifiers.Add(new BlockyNoiseModifier(false, 0.65f, new Vector3(0.04f, 0.24f, 0.04f),
			0.04f, 2, 2,
			0.01f, new Vector3(2, 2, 2)));

		// Weird blobs
		modifiers.Add(new BlockyNoiseModifier(true, 0.65f, new Vector3(0.01f, 0.01f, 0.01f),
			1, 1, 4,
			1, new Vector3(0.5f, 0.5f, 0.5f))
		{ ribbonCount = 1 });
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
	public void RecalcLight()
	{
		WorldLightAtlas.Instance.ClearAtlas();

		LightEngine.Begin();
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
			return BlockList.FILLED;

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

	//public static void FillCorner(int x, int y, int z)
	//{
	//	Chunk chunk = GetChunkFor(x, y, z);

	//	if (chunk == null || chunk.procStage < Chunk.ProcStage.Generate)
	//		return;

	//	ChunkBitArray corners = ((RealChunk)chunk).GetCorners();
	//	corners.Set(true,
	//		x - chunk.position.x,
	//		y - chunk.position.y,
	//		z - chunk.position.z
	//	);

	//	ChunkBitArray blurredCorners = ((RealChunk)chunk).GetBlurredCorners();
	//	blurredCorners.Set(true,
	//		(x - chunk.position.x) / 2,
	//		(y - chunk.position.y) / 2,
	//		(z - chunk.position.z) / 2
	//	);
	//}

	//public static bool GetCorner(int x, int y, int z)
	//{
	//	Chunk chunk = GetChunkFor(x, y, z);

	//	if (chunk == null || chunk.procStage < Chunk.ProcStage.Generate)
	//		return true;

	//	ChunkBitArray corners = ((RealChunk)chunk).GetCorners();
	//	return corners.Get(
	//		x - chunk.position.x,
	//		y - chunk.position.y,
	//		z - chunk.position.z
	//	);
	//}

	//public static bool GetBlurredCorner(int x, int y, int z)
	//{
	//	Chunk chunk = GetChunkFor(x, y, z);

	//	if (chunk == null || chunk.procStage < Chunk.ProcStage.Generate)
	//		return true;

	//	ChunkBitArray corners = ((RealChunk)chunk).GetBlurredCorners();
	//	return corners.Get(
	//		(x - chunk.position.x) / 2,
	//		(y - chunk.position.y) / 2,
	//		(z - chunk.position.z) / 2
	//	);
	//}

	public static int GetChunkSize()
	{
		return Instance.chunkSize;
	}

	public static Vector3Int GetRelativeOrigin()
	{
		return Instance.relativeOrigin;
	}

	//public static Dictionary<Vector3Int, Chunk> GetChunks()
	//{
	//	return Instance.chunks;
	//}

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

	public static int GetWorldSize()
	{
		return WorldBuilder.GetGenRangePlayable() * 2 * Instance.chunkSize;
	}

	public static int GetWorldSizeScenic()
	{
		return (WorldBuilder.GetGenRangePlayable() + WorldBuilder.GetGenRangeFake()) * 2 * Instance.chunkSize;
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
		Gizmos.color = Utils.colorDarkGrayBlue;
		Gizmos.DrawWireCube(Vector3.zero, 2 * chunkSize * worldBuilder.GetGenRangePlayable() * Vector3.one);

		Gizmos.color = Utils.colorPurple;
		Gizmos.DrawWireCube(Vector3.zero, 2 * chunkSize * (worldBuilder.GetGenRangePlayable() + worldBuilder.GetGenRangeFake()) * Vector3.one);

		worldBuilder.DrawGizmo();
	}
}
