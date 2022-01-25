using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
	[FormerlySerializedAs("modifiers")]
	private List<NoiseModifier> noiseModifiers = new List<NoiseModifier>();

	[SerializeField]
	private List<RoomModifier> roomModifiers = new List<RoomModifier>();

	//private Dictionary<Vector3Int, LinkedList<LightSource>> lightSources = new Dictionary<Vector3Int, LinkedList<LightSource>>();

	[SerializeField]
	private WorldGenerator generator;
	public static WorldGenerator Generator;

	[SerializeField]
	private LightEngine lighter;
	public static LightEngine Lighter;

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

		Generator = generator;
		Generator.Init();

		WorldInit();

		Lighter = lighter;
		sunObject.sourcePoints.center = new Vector3(chunkSize / 2, chunkSize * (1 + generator.GetGenRange()) - 0.5f, chunkSize / 2);
		sunObject.sourcePoints.extents = new Vector3(chunkSize * (0.5f + generator.GetGenRange()), 0.5f, chunkSize * (0.5f + generator.GetGenRange()));
		Lighter.Init(sunObject);
	}

	private void WorldInit()
	{
		// Pick a seed, then use it to initialize RNG
		if (randomizeSeed)
			seed = SeedlessRandom.NextIntInRange(int.MinValue, int.MaxValue);
		Random.InitState(seed);

		foreach (NoiseModifier mod in noiseModifiers)
			mod.Init();

		// Scene setup
		WaterFollow(relativeOrigin);
	}

	private void Start()
	{
		// First batch of chunks
		Generator.StartGen();
	}

	private void Update()
	{
		Generator.ContinueGenerating();
	}

	[ContextMenu("Restart Gen")]
	public void RestartGen()
	{
		//lightSources.Clear();
		chunks.Clear();

		randomizeSeed = true;
		WorldInit();

		Generator.StartGen();
	}

	[ContextMenu("Cancel Gen")]
	public void CancelGen()
	{
		Generator.StopGen();
	}

	public static void RegisterModifier(NoiseModifier modifier)
	{
		Instance.noiseModifiers.Add(modifier);
	}

	public static void RemoveModifier(NoiseModifier modifier)
	{
		Instance.noiseModifiers.Remove(modifier);
	}

	public static List<NoiseModifier> GetNoiseModifiers()
	{
		return Instance.noiseModifiers;
	}

	public static List<RoomModifier> GetRoomModifiers()
	{
		return Instance.roomModifiers;
	}

	public static void WaterFollow(Vector3 pos)
	{
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

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Utils.colorDarkGrayBlue;

		Gizmos.DrawWireCube(transform.position + Vector3.one * chunkSize / 2, Vector3.one * (1 + generator.GetGenRange() * 2) * chunkSize);


		Gizmos.color = Color.white;

		foreach (RoomModifier room in roomModifiers)
			Gizmos.DrawWireCube(room.bounds.center, room.bounds.extents * 2);
	}
}
