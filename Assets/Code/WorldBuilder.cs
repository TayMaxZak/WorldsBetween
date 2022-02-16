using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[System.Serializable]
public class WorldBuilder
{
	public enum GenStage
	{
		AwaitInitialGen,
		CreateChunks,
		EnqueueChunks,
		GenerateChunks,
		Ready
	}
	public GenStage genStage = GenStage.AwaitInitialGen;

	public bool active = true;
	public bool multiQ = false;
	public int queues = 32;

	[Header("References")]
	[SerializeField]
	private ChunkGameObject chunkPrefab;

	[Header("Generation")]
	[SerializeField]
	[Range(0, 15)]
	private int genRange = 8;

	[SerializeField]
	[Range(0, 10)]
	private int spawnGenRange = 3;

	private Timer chunkGenTimer = new Timer(1);

	public float targetProgress = 0.67f;

	private int generatorsUsed = 0;
	private int chunksToGen = 0;

	private GameObject chunkRoot;
	private Dictionary<Chunk.ProcStage, ChunkGenerator> chunkGenerators = new Dictionary<Chunk.ProcStage, ChunkGenerator>();
	private Queue<KeyValuePair<Vector3Int, Chunk>> chunksToQueue = new Queue<KeyValuePair<Vector3Int, Chunk>>();

	public void Init()
	{
		float delay = 0.0f;
		chunkGenerators = new Dictionary<Chunk.ProcStage, ChunkGenerator>()
		{
			{ Chunk.ProcStage.Allocate, new ChunkGenerator(0, 1) },
			{ Chunk.ProcStage.Generate, new ChunkGenerator(delay, queues) },
			{ Chunk.ProcStage.MakeMesh, new ChunkGenerator(delay, queues) }
		};

		chunkGenTimer.Reset();

		chunkRoot = new GameObject();
		chunkRoot.name = "Chunks";
	}

	public async void StartGen()
	{
		genStage = GenStage.CreateChunks;

		// Create chunk data and GameObjects
		InstantiateChunks(genRange);

		await Task.Delay(10);

		// Enqueue chunks
		await EnqueueAllChunks(Chunk.ProcStage.Allocate);

		genStage = GenStage.GenerateChunks;
	}

	public async void StopGen()
	{
		Debug.Log("Cancel start");

		active = false;

		while (GeneratorsBusy() > 0) await Task.Delay(100);

		Debug.Log("Cancel complete");
	}

	public void ContinueGenerating()
	{
		if (genStage < GenStage.EnqueueChunks || !active)
			return;

		chunksToGen = 0;
		generatorsUsed = 0;

		foreach (KeyValuePair<Chunk.ProcStage, ChunkGenerator> entry in chunkGenerators)
		{
			chunkGenerators.TryGetValue(entry.Key > 0 ? entry.Key - 1 : 0, out ChunkGenerator prev);

			chunksToGen += entry.Value.GetSize();

			bool empty = entry.Key == Chunk.ProcStage.Allocate;
			if (!empty && entry.Value.IsBusy())
				generatorsUsed++;

			if (empty || (entry.Value.GetSize() >= prev.GetSize()))
				entry.Value.Generate();
		}

		// Playable yet?
		if (genStage >= GenStage.GenerateChunks)
		{
			if (GenProgress() >= targetProgress / 2)
				GameManager.Instance.MidLoading();

			if (GenProgress() >= targetProgress || Mathf.Approximately(GenProgress(), 1))
				GameManager.Instance.FinishLoading(1000);
		}
	}

	public void InstantiateChunks(int range)
	{
		// Change range to actual distance
		int chunkSize = World.GetChunkSize();
		range *= chunkSize;

		Vector3Int origin = World.GetRelativeOrigin();

		// Start pos in chunk coordinates
		Vector3Int startPos = new Vector3Int(
			Mathf.FloorToInt(origin.x / chunkSize) * chunkSize,
			Mathf.FloorToInt(origin.y / chunkSize) * chunkSize,
			Mathf.FloorToInt(origin.z / chunkSize) * chunkSize
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
					if (World.GetChunks().ContainsKey(chunkPos))
						continue;

					// Instantiate chunk GameObject
					ChunkGameObject chunkGO = Object.Instantiate(chunkPrefab, chunkPos, Quaternion.identity, chunkRoot.transform);
					chunkGO.name = "Chunk " + x + ", " + y + ", " + z;

					// Initialize and register chunk
					chunkGO.data = new Chunk();
					chunkGO.data.SetPos(chunkPos);

					chunkGO.data.chunkMesh.Init(chunkGO.data, chunkGO.filter);

					chunkGO.data.go = chunkGO;

					World.GetChunks().Add(chunkPos, chunkGO.data);
				}
			}
		}

		genStage = GenStage.EnqueueChunks;
	}

	public async Task EnqueueAllChunks(Chunk.ProcStage procStage)
	{
		// First get all chunks
		foreach (var entry in World.GetChunks())
			chunksToQueue.Enqueue(entry);

		// Then go through a few at a time
		int taskSize = 32;
		while (chunksToQueue.Count > 0)
		{
			for (int i = taskSize; i > 0 && chunksToQueue.Count > 0; i--)
			{
				Chunk c = chunksToQueue.Dequeue().Value;
				c.procStage = procStage;
				QueueNextStage(c);
			}

			await Task.Delay(1);
		}
	}

	private void UpdateChunkGameObjects()
	{
		// TODO: Reuse grid of chunks instead of instantiating new ones

		if (!World.IsInfinite())
			return;

		chunkGenTimer.Increment(Time.deltaTime);

		if (chunkGenTimer.Expired())
			chunkGenTimer.Reset();
		else
			return;
	}

	public void QueueNextStage(Chunk chunk)
	{
		chunkGenerators.TryGetValue(chunk.procStage, out ChunkGenerator generator);

		if (generator == null)
			return;

		// Add to appropriate queue. Closer chunks have higher priority (lower value)
		Vector3Int origin = World.GetRelativeOrigin();
		float priority = Vector3.SqrMagnitude((chunk.position + Vector3.one * World.GetChunkSize() / 2f) - origin);
		generator.Enqueue(chunk, priority, multiQ);
	}

	public int GetGenRange()
	{
		return genRange;
	}

	public int GetSpawnGenRange()
	{
		return spawnGenRange;
	}

	public bool IsGenerating()
	{
		return generatorsUsed > 0;
	}

	public int ChunksToGen()
	{
		return chunksToGen;
	}

	public int GeneratorsUsed()
	{
		return generatorsUsed;
	}

	private int GeneratorsBusy()
	{
		int busy = 0;

		foreach (KeyValuePair<Chunk.ProcStage, ChunkGenerator> entry in chunkGenerators)
		{
			if (entry.Value.IsBusy())
				busy++;
		}

		return busy;
	}

	public float GenProgress()
	{
		int totalChunks = Mathf.Max(World.GetChunks().Count, 1);
		float progress = (totalChunks - chunksToGen) / (float)totalChunks;

		return progress;
	}

	public Dictionary<Chunk.ProcStage, ChunkGenerator> GetChunkGenerators()
	{
		return chunkGenerators;
	}
}
