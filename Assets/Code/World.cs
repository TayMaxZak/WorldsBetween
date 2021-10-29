using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

public class World : MonoBehaviour
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
	private List<LightSource> lightSources = new List<LightSource>();

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
	[SerializeField]
	private Timer lightUpdateTimer = null;
	[SerializeField]
	private int lightUpdateSize = 32;

	private bool initialWorldGen = true;

	private Dictionary<Chunk.GenStage, ChunkGenerator> chunkGenerators = new Dictionary<Chunk.GenStage, ChunkGenerator>();

	[Header("Generation")]
	[SerializeField]
	private int nearPlayerGenRange = 4;

	// Extras
	private List<Chunk> chunksToLightUpdate = new List<Chunk>();
	private List<Chunk> chunksToLightCleanup = new List<Chunk>();

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
			{ Chunk.GenStage.Empty, new ChunkGenerator(64, 0.25f) },
			{ Chunk.GenStage.Allocated, new ChunkGenerator(8, 0.05f) },
			{ Chunk.GenStage.Generated, new ChunkGenerator(8, 0.05f) },
			{ Chunk.GenStage.Meshed, new ChunkGenerator(8, 0.05f) },
			{ Chunk.GenStage.Lit, new ChunkGenerator(10, 1) },
		};

		// Init timers
		chunkGenTimer.Reset();
	}

	private void Start()
	{
		// First batch of chunks
		CreateChunksNearPlayer(8);

		CalculateLighting();

		initialWorldGen = false;
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
					chunk.name = "Chunk " + x + ", " + y + ", " + z;
					chunks.Add(chunkPos, chunk);

					// Add a random light to this chunk
					if (Random.value > 0.75f)
					{
						LightSource light = Instantiate(prefabLight, new Vector3(
							chunkPos.x + Random.value * chunkSize,
							chunkPos.y + Random.value * chunkSize,
							chunkPos.z + Random.value * chunkSize),
						Quaternion.identity, lightRoot);

						light.colorTemp = Random.Range(-10, 10);
					}

					// Add chunk to generator
					QueueNextStage(chunk, Chunk.GenStage.Empty);
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
		if (initialWorldGen)
			return;

		UpdateChunkCreation();

		foreach (KeyValuePair<Chunk.GenStage, ChunkGenerator> entry in chunkGenerators)
			entry.Value.Generate(Time.deltaTime);

		CalculateLighting();
	}

	private void UpdateChunkCreation()
	{
		chunkGenTimer.Increment(Time.deltaTime);

		if (chunkGenTimer.Expired())
			chunkGenTimer.Reset();
		else
			return;

		CreateChunksNearPlayer(nearPlayerGenRange);
	}

	public static void QueueNextStage(Chunk chunk, Chunk.GenStage stage)
	{
		QueueNextStage(chunk, stage, 1);
	}

	public static void QueueNextStage(Chunk chunk, Chunk.GenStage stage, float prioPenalty)
	{
		Instance.chunkGenerators.TryGetValue(stage, out ChunkGenerator generator);

		if (generator == null)
			return;

		// Add to appropriate queue. Closer chunks have higher priority
		generator.Enqueue(chunk, prioPenalty * Vector3.SqrMagnitude((chunk.position + Vector3.one * Instance.chunkSize / 2f) - Instance.player.transform.position));
	}

	public static void RegisterLight(LightSource light)
	{
		Instance.lightSources.Add(light);
	}

	public static void RemoveLight(LightSource light)
	{
		Instance.lightSources.Remove(light);
	}

	// TODO: Overhaul!
	private void CalculateLighting()
	{
		lightUpdateTimer.Increment(Time.deltaTime);

		// Is this a major light update?
		bool doLightUpdate = lightUpdateTimer.Expired();

		// Reset timer for next update
		if (doLightUpdate)
			lightUpdateTimer.Reset();

		// Find partial time for blending light
		float partialTime = Mathf.Clamp01(1 - lightUpdateTimer.currentTime / lightUpdateTimer.maxTime);
		Shader.SetGlobalFloat("PartialTime", partialTime);

		if (!doLightUpdate)
			return;

		//chunksToLightUpdate.Clear();

		// Apply lights
		for (int i = 0; i < lightSources.Count; i++)
		{
			lightSources[i].UpdatePosition();

			if (!lightSources[i].dirty)
				continue;

			chunksToLightCleanup = lightSources[i].FindAffectedChunks();

			//foreach (Chunk chunk in chunksToLightCleanup)
			//{
			//	if (!lightSources[i].affectedChunks.Contains(chunk))
			//		chunk.CleanupLight();
			//}

			// Should this light activate?
			bool notReady = false;

			foreach (Chunk chunk in lightSources[i].affectedChunks)
			{
				if (chunk.genStage != Chunk.GenStage.Meshed && chunk.genStage != Chunk.GenStage.Lit)
				{
					notReady = true;
					break;
				}
			}

			if (notReady)
				continue;

			// Go through each affected chunk
			foreach (Chunk chunk in lightSources[i].affectedChunks)
			{
				if (chunk.genStage != Chunk.GenStage.Meshed && chunk.genStage != Chunk.GenStage.Lit)
					continue;

				// First pass on this chunk. Reset "canvas" and apply light from scratch
				bool firstPass = !chunksToLightUpdate.Contains(chunk);

				if (firstPass)
					chunksToLightUpdate.Add(chunk);

				// Mark chunk as dirty
				if (chunk.lightsToHandle > 0)
				{
					if (firstPass)
						chunk.MarkAsDirtyForLight();

					// This chunk can consider this light handled
					chunk.lightsToHandle--;

					// Last pass on this chunk? If so, begin applying vertex colors
					bool lastPass = chunk.lightsToHandle == 0;

					chunk.AddLight(lightSources[i], firstPass, lastPass);

					if (lastPass)
						chunk.genStage = Chunk.GenStage.Lit;
				}
			}

			if (lightSources[i].dirty && !notReady)
				lightSources[i].dirty = false;
		}

		foreach (Chunk chunk in chunksToLightUpdate)
		{
			chunk.UpdateLightVisuals();
		}
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

	public static int GetLightUpdateSize()
	{
		return Instance.lightUpdateSize;
	}
}
