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
	private int seed = 0;
	[SerializeField]
	private int chunkSize = 8;

	public bool isInfinite = true;

	[Header("World Settings")]
	[SerializeField]
	private int waterHeight = 0;

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
		sunObject.sourcePoints.center = new Vector3(chunkSize / 2, chunkSize * (1 + worldBuilder.GetGenRange()) - 0.5f, chunkSize / 2);
		sunObject.sourcePoints.extents = new Vector3(chunkSize * (0.5f + worldBuilder.GetGenRange()), 0.5f, chunkSize * (0.5f + worldBuilder.GetGenRange()));
		LightEngine.Init(sunObject);
	}

	private void WorldInit()
	{
		// Pick a seed, then use it to initialize RNG
		if (randomizeSeed)
			seed = SeedlessRandom.NextIntInRange(int.MinValue, int.MaxValue);
		Random.InitState(seed);

		MakeModifiers();

		foreach (Modifier mod in modifiers)
			mod.Init();

		// Scene setup
		WaterFollow(relativeOrigin);
	}

	private void MakeModifiers()
	{
		//modifiers.Add(new NoiseModifier(false, 0.1f, new Vector3(0.05f, 0.05f, 0.05f)));

		// Main boxes
		modifiers.Add(new BlockyNoiseModifier(false, 0.9f, new Vector3(0.04f, 0.03f, 0.04f),
			0.35f, 3, 16,
			0.15f, new Vector3(0.15f, 0.4f, 0.15f)));

		// Shards
		modifiers.Add(new BlockyNoiseModifier(true, 0.6f, new Vector3(0.03f, 0.01f, 0.03f),
			0.25f, 5, 10,
			0.3f, new Vector3(0.2f, 0.0f, 0.2f)));

		// Etches
		modifiers.Add(new BlockyNoiseModifier(false, 0.6f, new Vector3(0.04f, 0.24f, 0.04f),
			0.04f, 2, 2,
			0.01f, new Vector3(2, 2, 2)));

		// Weird blobs
		modifiers.Add(new BlockyNoiseModifier(true, 0.55f, new Vector3(0.01f, 0.01f, 0.01f),
			1, 1, 4,
			1, new Vector3(0.5f, 0.5f, 0.5f)));
	}

	private void Start()
	{
		// First batch of chunks
		WorldBuilder.StartGen();
	}

	private void Update()
	{
		WorldBuilder.ContinueGenerating();
	}

	[ContextMenu("Restart Gen")]
	public async void RestartGen()
	{
		if (randomizeSeed)
			seed = SeedlessRandom.NextIntInRange(int.MinValue, int.MaxValue);
		Random.InitState(seed);

		foreach (Modifier mod in modifiers)
			mod.Init();

		await WorldBuilder.EnqueueAllChunks(Chunk.ProcStage.Allocate);

		// Recalc light after world builder is finished
		while (WorldBuilder.IsGenerating())
			await Task.Delay(20);

		RecalcLight();
	}

	[ContextMenu("Recalculate Light")]
	public void RecalcLight()
	{
		WorldLightAtlas.Instance.ClearAtlas();

		LightEngine.Begin();
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

	public static Chunk GetChunkFor(int x, int y, int z)
	{
		float chunkSize = Instance.chunkSize;

		Instance.chunks.TryGetValue(new Vector3Int(
			Mathf.FloorToInt(x / chunkSize) * (int)chunkSize,
			Mathf.FloorToInt(y / chunkSize) * (int)chunkSize,
			Mathf.FloorToInt(z / chunkSize) * (int)chunkSize
		),
		out Chunk chunk);

		return chunk;
	}

	public static Chunk GetChunkFor(Vector3Int pos)
	{
		return GetChunkFor(pos.x, pos.y, pos.z);
	}

	public static Block GetBlockFor(int x, int y, int z)
	{
		Chunk chunk = GetChunkFor(x, y, z);

		if (chunk == null || chunk.procStage == Chunk.ProcStage.Allocate)
			return Block.empty;

		return chunk.GetBlock(
			x - chunk.position.x,
			y - chunk.position.y,
			z - chunk.position.z
		);
	}

	public static Block GetBlockFor(Vector3Int pos)
	{
		return GetBlockFor(pos.x, pos.y, pos.z);
	}

	public static void SetCorner(bool value, int x, int y, int z)
	{
		Chunk chunk = GetChunkFor(x, y, z);

		if (chunk == null || chunk.procStage < Chunk.ProcStage.Generate)
			return;

		ChunkBitArray corners = chunk.GetCorners();
		corners.Set(value,
			x - chunk.position.x,
			y - chunk.position.y,
			z - chunk.position.z
		);
	}

	public static bool GetCorner(int x, int y, int z)
	{
		Chunk chunk = GetChunkFor(x, y, z);

		if (chunk == null || chunk.procStage < Chunk.ProcStage.Generate)
			return true;

		ChunkBitArray corners = chunk.GetCorners();
		return corners.Get(
			x - chunk.position.x,
			y - chunk.position.y,
			z - chunk.position.z
		);
	}

	public static int GetChunkSize()
	{
		return Instance.chunkSize;
	}

	public static Vector3Int GetRelativeOrigin()
	{
		return Instance.relativeOrigin;
	}

	public static Dictionary<Vector3Int, Chunk> GetChunks()
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

	private void OnDrawGizmos()
	{
		Gizmos.color = Utils.colorDarkGrayBlue;

		Gizmos.DrawWireCube(transform.position + Vector3.one * chunkSize / 2, Vector3.one * (1 + worldBuilder.GetGenRange() * 2) * chunkSize);
	}
}
