using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class World : MonoBehaviour
{
	private static World Instance;

	[Header("References")]
	[SerializeField]
	private Transform player;

	[SerializeField]
	private Chunk chunkPrefab;
	private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

	private List<Modifier> modifiers = new List<Modifier>();

	[SerializeField]
	private Transform lightRoot;
	[SerializeField]
	private LightSource prefabLight;

	[Header("World Settings")]
	[SerializeField]
	private bool randomizeSeed = false;
	[SerializeField]
	private int seed = 0;
	[SerializeField]
	private int chunkSize = 8;

	[Header("Performance")]
	[SerializeField]
	private Timer chunkGenTimer = null;

	private bool firstChunks = true;
	private int generatorsUsed = 0;

	private Dictionary<Chunk.GenStage, ChunkGenerator> chunkGenerators = new Dictionary<Chunk.GenStage, ChunkGenerator>();
	private Dictionary<Vector3Int, LinkedList<LightSource>> lightSources = new Dictionary<Vector3Int, LinkedList<LightSource>>();

	[Header("Generation")]
	[SerializeField]
	private int initialGenRange = 11;
	[SerializeField]
	private int nearPlayerGenRange = 4;
	[SerializeField]
	private float initialGenTime = 10;

	[Header("Level Settings")]
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

		// Pick a seed, then use it to initialize RNG
		if (randomizeSeed)
			seed = Random.Range(int.MinValue, int.MaxValue);
		Random.InitState(seed);

		// Init dictionaries
		chunkGenerators = new Dictionary<Chunk.GenStage, ChunkGenerator>()
		{
			{ Chunk.GenStage.Empty, new ChunkGenerator(800, 0.25f) },
			{ Chunk.GenStage.Allocated, new ChunkGenerator(15, 0.01f) },
			{ Chunk.GenStage.Generated, new ChunkGenerator(5, 0.01f) },
			{ Chunk.GenStage.Meshed, new ChunkGenerator(10, 0.01f) },
			{ Chunk.GenStage.Lit, new ChunkGenerator(30, 0.01f) },
		};

		// Init timers
		chunkGenTimer.Reset(1);
	}

	private void Start()
	{
		// First batch of chunks
		CreateChunksNearPlayer(initialGenRange);

		firstChunks = false;
	}

	private void CreateChunksNearPlayer(int range)
	{
		// Change range to actual distance
		range *= chunkSize;

		// Start pos in chunk coordinates
		Vector3Int startPos = new Vector3Int(
			Mathf.FloorToInt(player.position.x / chunkSize) * chunkSize,
			Mathf.FloorToInt(player.position.y / chunkSize) * chunkSize,
			Mathf.FloorToInt(player.position.z / chunkSize) * chunkSize
		);

		// Go through all nearby chunk positions
		for (int x = startPos.x - range; x <= startPos.x + range; x += chunkSize)
		{
			for (int y = startPos.y - range; y <= startPos.y + range; y += chunkSize) // TODO: Remove testing code
			{
				for (int z = startPos.z - range; z <= startPos.z + range; z += chunkSize)
				{
					Vector3Int chunkPos = new Vector3Int(x, y, z);

					// Chunk already exists at this location
					if (chunks.ContainsKey(chunkPos))
						continue;

					// Create and register chunk
					Chunk chunk = Instantiate(chunkPrefab, chunkPos, Quaternion.identity, transform);
					chunk.UpdatePos();
					chunk.name = "Chunk " + x + ", " + y + ", " + z;
					chunks.Add(chunkPos, chunk);

					// Add a random light to this chunk
					if (Random.value > 0.15f)
					{
						LightSource light = Instantiate(prefabLight, new Vector3(
							chunkPos.x + Random.value * chunkSize,
							chunkPos.y + Random.value * chunkSize,
							chunkPos.z + Random.value * chunkSize),
						Quaternion.identity, lightRoot);

						light.colorTemp = Random.Range(-10, 10);
					}

					// Add chunk to generator
					QueueNextStage(chunk);
				}
			}
		}
	}

	public static void RegisterModifier(Modifier modifier)
	{
		Instance.modifiers.Add(modifier);
	}

	public static void RemoveModifier(Modifier modifier)
	{
		Instance.modifiers.Remove(modifier);
	}

	public static List<Modifier> GetModifiers()
	{
		return Instance.modifiers;
	}

	private void Update()
	{
		if (firstChunks)
			return;

		generatorsUsed = 0;
		foreach (KeyValuePair<Chunk.GenStage, ChunkGenerator> entry in chunkGenerators)
		{
			Instance.chunkGenerators.TryGetValue(entry.Key > 0 ? entry.Key - 1 : 0, out ChunkGenerator prev);

			// Only make new empty chunks if empty queue is empty
			if (entry.Key == Chunk.GenStage.Empty && entry.Value.GetSize() == 0)
				UpdateChunkCreation();

			// Wait until previous queue is wrapped up
			if (entry.Key == Chunk.GenStage.Empty || (prev.GetSize() < entry.Value.GetSize()))
			{
				entry.Value.Generate(Time.deltaTime);

				generatorsUsed++;
			}
		}
	}

	private void UpdateChunkCreation()
	{
		//// Already max render distance (9 chunks out in each direction + 2 for world edge)
		//// TODO: Active chunk count > than this, not just total chunks
		//if (chunks.Count > 12000)
		//	return;

		chunkGenTimer.Increment(Time.deltaTime);

		if (chunkGenTimer.Expired())
			chunkGenTimer.Reset();
		else
			return;

		CreateChunksNearPlayer(nearPlayerGenRange);
	}

	public static void QueueNextStage(Chunk chunk)
	{
		QueueNextStage(chunk, false);
	}

	public static void QueueNextStage(Chunk chunk, bool penalize)
	{
		Instance.chunkGenerators.TryGetValue(chunk.genStage, out ChunkGenerator generator);

		if (generator == null)
			return;

		// Add to appropriate queue. Closer chunks have higher priority (lower value)
		generator.Enqueue(chunk, (penalize ? 128 : 0) + Vector3.SqrMagnitude((chunk.position + Vector3.one * Instance.chunkSize / 2f) - Instance.player.transform.position));
	}

	public static void RegisterLight(LightSource light)
	{
		light.FindAffectedChunks();

		foreach (Vector3Int chunk in light.affectedChunks)
		{
			Instance.lightSources.TryGetValue(chunk, out LinkedList<LightSource> ls);

			if (ls == null)
				Instance.lightSources.Add(chunk, ls = new LinkedList<LightSource>());

			ls.AddLast(light);
		}
	}

	public static void RemoveLight(LightSource light)
	{
		foreach (Vector3Int chunk in light.affectedChunks)
		{
			Instance.lightSources.TryGetValue(chunk, out LinkedList<LightSource> ls);

			if (ls != null)
				ls.Remove(light);
		}
	}

	public static LinkedList<LightSource> GetLightsFor(Chunk chunk)
	{
		Instance.lightSources.TryGetValue(chunk.position, out LinkedList<LightSource> ls);

		return ls;
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

		if (chunk == null || chunk.genStage == Chunk.GenStage.Empty)
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

	public static int GetChunkSize()
	{
		return Instance.chunkSize;
	}

	public static bool DoAccelerateGen()
	{
		return Time.time < Instance.initialGenTime;
	}

	public static bool IsGen()
	{
		return Instance.generatorsUsed > 1;
	}

	public static int GetWaterHeight()
	{
		return Instance.waterHeight;
	}
}
