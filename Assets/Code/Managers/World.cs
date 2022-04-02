﻿using System.Collections;
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

	//[Header("World Settings")]
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

	public static int GetChunkSize()
	{
		return Instance.chunkSize;
	}

	public static Vector3Int GetRelativeOrigin()
	{
		return Instance.relativeOrigin;
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

		worldBuilder.DrawGizmo();
	}
}
