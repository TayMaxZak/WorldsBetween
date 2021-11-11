using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class World : MonoBehaviour
{
	private static World Instance;

	public bool disable = false;
	public bool infinite = true;

	[Header("References")]
	[SerializeField]
	private Transform player;

	[SerializeField]
	private ChunkGameObject chunkPrefab;
	private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

	[SerializeField]
	private List<Noise> modifiers = new List<Noise>();

	[SerializeField]
	private GameObject waterSystem;

	[Header("World Settings")]
	[SerializeField]
	private bool randomizeSeed = false;
	[SerializeField]
	private int seed = 0;
	[SerializeField]
	private int chunkSize = 8;

	[Header("Generation")]
	[SerializeField]
	private int initialGenRange = 11;
	[SerializeField]
	private int nearPlayerGenRange = 4;
	[SerializeField]
	private float initialGenTime = 10;

	[SerializeField]
	private Timer chunkGenTimer = null;

	private bool firstChunks = true;
	private int generatorsUsed = 0;
	private int chunksToGen = 0;

	private Dictionary<Chunk.GenStage, ChunkGenerator> chunkGenerators = new Dictionary<Chunk.GenStage, ChunkGenerator>();
	private Dictionary<Vector3Int, LinkedList<LightSource>> lightSources = new Dictionary<Vector3Int, LinkedList<LightSource>>();

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
			{ Chunk.GenStage.Allocated, new ChunkGenerator(50, 0.01f) },
			{ Chunk.GenStage.Generated, new ChunkGenerator(50, 0.01f) },
			{ Chunk.GenStage.Meshed, new ChunkGenerator(50, 0.01f) },
			{ Chunk.GenStage.Lit, new ChunkGenerator(50, 0.01f) },
		};

		foreach (Noise mod in modifiers)
			mod.Init();

		// Init timers
		chunkGenTimer.Reset();

		waterSystem.transform.position = new Vector3(player.transform.position.x, waterHeight, player.transform.position.z);

		if (disable)
			enabled = false;
	}

	private void Start()
	{
		// First batch of chunks
		CreateChunksNearPlayer(initialGenRange);

		firstChunks = false;
	}

	private void CreateChunksNearPlayer(int range)
	{
		// TODO: Reuse grid of chunks instead of instantiating new ones

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

					// Instantiate chunk GameObject
					ChunkGameObject chunkGO = Instantiate(chunkPrefab, chunkPos, Quaternion.identity, transform);
					chunkGO.name = "Chunk " + x + ", " + y + ", " + z;

					// Initialize and register chunk
					chunkGO.data = new Chunk();
					chunkGO.data.SetPos(chunkPos);

					chunkGO.data.chunkMesh.Init(chunkGO.data, chunkGO.filter, chunkGO.blockMesh);

					chunks.Add(chunkPos, chunkGO.data);

					// Add a random light to this chunk
					if (Random.value < 0.2f)
					{
						for (int r = 0; r <= 5 + Random.value * 45; r++)
						{
							RegisterLight(new PointLightSource(
								Random.Range(0.4f, 0.7f),
								Random.Range(-2f, 1f) + Random.Range(0f, 3f),
								new Vector3(
									chunkPos.x + Random.value * chunkSize,
									chunkPos.y + Random.value * chunkSize,
									chunkPos.z + Random.value * chunkSize)
								)
							);
						}
					}
					else
					if (Random.value < 0.1f)
					{
						for (int r = 0; r <= 1 + Random.value * 2; r++)
						{
							RegisterLight(new PointLightSource(
								1.7f,
								-0.7f,
								new Vector3(
									chunkPos.x + Random.value * chunkSize,
									chunkPos.y + Random.value * chunkSize,
									chunkPos.z + Random.value * chunkSize)
								)
							);
						}
					}

					// Add chunk to generator
					QueueNextStage(chunkGO.data);
				}
			}
		}
	}

	public static void RegisterModifier(Noise modifier)
	{
		Instance.modifiers.Add(modifier);
	}

	public static void RemoveModifier(Noise modifier)
	{
		Instance.modifiers.Remove(modifier);
	}

	public static List<Noise> GetModifiers()
	{
		return Instance.modifiers;
	}

	private void Update()
	{
		if (firstChunks)
			return;

		generatorsUsed = 0;
		chunksToGen = 0;

		// Update chunk generators
		if (DoAccelerateGen())
			ChunkGenerator.SetThreadingMode(ChunkGenerator.ThreadingMode.FullUsage);
		else
			ChunkGenerator.SetThreadingMode(ChunkGenerator.ThreadingMode.Background);

		foreach (KeyValuePair<Chunk.GenStage, ChunkGenerator> entry in chunkGenerators)
		{
			Instance.chunkGenerators.TryGetValue(entry.Key > 0 ? entry.Key - 1 : 0, out ChunkGenerator prev);

			bool empty = entry.Key == Chunk.GenStage.Empty;

			// Only make new empty chunks if empty queue is empty
			if (empty && entry.Value.GetSize() == 0)
				UpdateChunkCreation();

			chunksToGen += entry.Value.GetSize();

			// Wait until previous queue is wrapped up
			if (empty || (prev.GetSize() < entry.Value.GetSize()))
			{
				if (!empty || entry.Value.IsBusy())
					generatorsUsed++;

				// Don't overload number of generators
				if (generatorsUsed <= 2)
				{
					entry.Value.Generate(Time.deltaTime);

					entry.Value.SetWait(false);
				}
				else
				{
					entry.Value.SetWait(true);
				}
			}
		}
	}

	private void UpdateChunkCreation()
	{
		if (!infinite)
			return;

		chunkGenTimer.Increment(Time.deltaTime);

		if (chunkGenTimer.Expired())
			chunkGenTimer.Reset();
		else
			return;

		if (DoAccelerateGen())
			CreateChunksNearPlayer(initialGenRange);
		else
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

		foreach (Vector3Int chunk in light.GetAffectedChunks())
		{
			Instance.lightSources.TryGetValue(chunk, out LinkedList<LightSource> ls);

			// First light added to this chunk
			if (ls == null)
				Instance.lightSources.Add(chunk, ls = new LinkedList<LightSource>());

			if (!ls.Contains(light))
				ls.AddLast(light);
		}
	}

	public static void RemoveLight(LightSource light)
	{
		foreach (Vector3Int chunk in light.GetAffectedChunks())
		{
			Instance.lightSources.TryGetValue(chunk, out LinkedList<LightSource> ls);

			if (ls != null)
				ls.Remove(light);
		}
	}

	public static void UpdateLight(LightSource light, bool recalcLight)
	{
		List<Vector3Int> oldChunks = light.FindAffectedChunks();

		foreach (Vector3Int chunk in oldChunks)
		{
			Instance.lightSources.TryGetValue(chunk, out LinkedList<LightSource> ls);

			if (ls != null)
				ls.Remove(light);

			if (recalcLight)
			{
				Chunk c = GetChunkFor(chunk);
				if (c != null)
				{
					c.QueueLightUpdate();
					c.NeedsLightDataRecalc(light);
				}
			}
		}

		foreach (Vector3Int chunk in light.GetAffectedChunks())
		{
			Instance.lightSources.TryGetValue(chunk, out LinkedList<LightSource> ls);

			// First light added to this chunk
			if (ls == null)
				Instance.lightSources.Add(chunk, ls = new LinkedList<LightSource>());

			if (!ls.Contains(light))
				ls.AddLast(light);

			if (recalcLight)
			{
				Chunk c = GetChunkFor(chunk);
				if (c != null)
				{
					c.QueueLightUpdate();
					c.NeedsLightDataRecalc(light);
				}
			}
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

	public static bool IsInfinite()
	{
		return Instance.infinite;
	}

	public static bool IsGen()
	{
		return Instance.generatorsUsed > 0;
	}

	public static int ChunksToGen()
	{
		return Instance.chunksToGen;
	}

	public static int GetWaterHeight()
	{
		return Instance.waterHeight;
	}
}
