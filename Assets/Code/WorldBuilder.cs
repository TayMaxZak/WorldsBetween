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
	public int enqueueTaskSize = 32;
	public int generatorTaskSize = 32;

	private float progress = 0;

	[Header("References")]
	[SerializeField]
	private ChunkGameObject chunkPrefab;

	[SerializeField]
	private SpawnFinder spawnFinder;

	[Header("Generation")]
	[SerializeField]
	[Range(0, 10)]
	private int genRangePlayable = 5;
	[SerializeField]
	[Range(0, 10)]
	private int genRangeScenic = 2;

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
			{ Chunk.ProcStage.Allocate, new ChunkGenerator(0, 1, enqueueTaskSize) },
			{ Chunk.ProcStage.Generate, new ChunkGenerator(delay, queues, generatorTaskSize) },
			{ Chunk.ProcStage.MakeMesh, new ChunkGenerator(delay, queues, generatorTaskSize) }
		};

		chunkRoot = new GameObject();
		chunkRoot.name = "Chunks";
	}

	public async void StartGen(bool instantiate)
	{
		if (instantiate)
		{
			genStage = GenStage.CreateChunks;

			// Create chunk data and GameObjects
			InstantiateChunks(genRangePlayable);

			await Task.Delay(10);
		}

		// Enqueue chunks
		await EnqueueAllChunks(Chunk.ProcStage.Allocate);

		genStage = GenStage.GenerateChunks;

		spawnFinder.Reset();
	}

	public async void StopGen()
	{
		Debug.Log("Cancel start");

		active = false;

		while (GeneratorsBusy() > 0) await Task.Delay(100);

		Debug.Log("Cancel complete");
	}

	public void ResetSpawnFinder()
	{
		spawnFinder.Reset();
	}


	public void UpdateSpawnFinder()
	{
		if (spawnFinder.IsBusy() || spawnFinder.IsSuccessful())
			return;

		spawnFinder.Tick();
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

			if (empty || (prev.GetSize() == 0))
				entry.Value.Generate();
		}

		if (genStage >= GenStage.Ready)
			UpdateSpawnFinder();
	}

	public void InstantiateChunks(int range)
	{
		// Change range to actual distance
		int chunkSize = World.GetChunkSize();

		Vector3Int origin = World.GetRelativeOrigin();

		// Start pos in chunk coordinates
		Vector3Int startPos = new Vector3Int(
			Mathf.FloorToInt(origin.x / chunkSize) * chunkSize,
			Mathf.FloorToInt(origin.y / chunkSize) * chunkSize,
			Mathf.FloorToInt(origin.z / chunkSize) * chunkSize
		);

		// Go through all nearby chunk positions
		for (int x = startPos.x - range; x < startPos.x + range; x ++)
		{
			for (int y = startPos.y - range; y < startPos.y + range; y ++)
			{
				for (int z = startPos.z - range; z < startPos.z + range; z ++)
				{
					Vector3Int chunkPos = new Vector3Int(x * chunkSize, y * chunkSize, z * chunkSize);

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

	public async Task EnqueueAllChunks(Chunk.ProcStage curChunkStage)
	{
		// First get all chunks
		foreach (var entry in World.GetChunks())
		{
			//// Default blocks if needed (restarted gen)
			//if (entry.Value.didInit && curChunkStage == Chunk.ProcStage.Allocate)
			//	entry.Value.SetBlocksToDefault();

			chunksToQueue.Enqueue(entry);
		}

		await Task.Delay(10);

		// Then go through a few at a time
		while (chunksToQueue.Count > 0)
		{
			for (int i = enqueueTaskSize; i > 0 && chunksToQueue.Count > 0; i--)
			{
				Chunk c = chunksToQueue.Dequeue().Value;
				c.procStage = curChunkStage;

				QueueNextStage(c);
			}

			await Task.Delay(1);
		}
	}

	public void QueueNextStage(Chunk chunk)
	{
		QueueNextStage(chunk, false);
	}

	public void QueueNextStage(Chunk chunk, bool requeue)
	{
		chunkGenerators.TryGetValue(chunk.procStage, out ChunkGenerator generator);

		if (generator == null)
			return;

		// Add to appropriate queue. Closer chunks have higher priority (lower value)
		Vector3Int origin = World.GetRelativeOrigin();
		float priority = (requeue ? 10 : 0) + Vector3.SqrMagnitude((chunk.position + Vector3.one * World.GetChunkSize() / 2f) - origin);
		generator.Enqueue(chunk, priority, multiQ);
	}

	public int GetGenRange()
	{
		return genRangePlayable;
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

	public void ChunkFinishedProcStage()
	{
		float firstTimeMult = Mathf.Approximately(progress, 0) ? 2 : 1;

		progress += firstTimeMult * (1f / World.GetChunks().Count) / chunkGenerators.Count;
	}

	public float GetGenProgress()
	{
		return progress;
	}

	public Dictionary<Chunk.ProcStage, ChunkGenerator> GetChunkGenerators()
	{
		return chunkGenerators;
	}

	public void DrawGizmo()
	{
		spawnFinder.DrawGizmo();
	}
}
