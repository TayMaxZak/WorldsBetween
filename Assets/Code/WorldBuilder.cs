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
	private ChunkGameObject fakeChunkPrefab;

	[SerializeField]
	private SpawnFinder spawnFinder;
	[SerializeField]
	private GoalFinder goalFinder;

	[Header("Generation")]
	[SerializeField]
	[Range(0, 10)]
	private int genRangePlayable = 5;
	[SerializeField]
	[Range(0, 10)]
	private int genRangeFake = 2;

	private int generatorsUsed = 0;
	private int chunksToGen = 0;

	private GameObject chunkRoot;
	private GameObject fakeChunkRoot;

	private Dictionary<Chunk.BuildStage, ChunkGenerator> chunkGenerators = new Dictionary<Chunk.BuildStage, ChunkGenerator>();
	private Dictionary<Chunk.BuildStage, ChunkGenerator> fakeChunkGenerators = new Dictionary<Chunk.BuildStage, ChunkGenerator>();


	private Queue<KeyValuePair<Vector3Int, Chunk>> chunksToQueue = new Queue<KeyValuePair<Vector3Int, Chunk>>();

	public void Init()
	{
		float delay = 0.0f;

		chunkGenerators = new Dictionary<Chunk.BuildStage, ChunkGenerator>()
		{
			{ Chunk.BuildStage.Init, new ChunkGenerator(0, 1, enqueueTaskSize) },
			{ Chunk.BuildStage.GenerateTerrain, new ChunkGenerator(delay, queues, generatorTaskSize) },
			{ Chunk.BuildStage.GenerateFeature, new ChunkGenerator(delay, queues, generatorTaskSize) },
			{ Chunk.BuildStage.GenerateDecorator, new ChunkGenerator(delay, queues, generatorTaskSize) },
			{ Chunk.BuildStage.MakeMesh, new ChunkGenerator(delay, queues, generatorTaskSize) }
		};
		chunkRoot = new GameObject();
		chunkRoot.name = "Chunks";

		fakeChunkGenerators = new Dictionary<Chunk.BuildStage, ChunkGenerator>()
		{
			{ Chunk.BuildStage.Init, new ChunkGenerator(0, 1, enqueueTaskSize) },
			{ Chunk.BuildStage.GenerateTerrain, new ChunkGenerator(delay, queues, generatorTaskSize) },
			{ Chunk.BuildStage.GenerateFeature, new ChunkGenerator(delay, queues, generatorTaskSize) },
			{ Chunk.BuildStage.GenerateDecorator, new ChunkGenerator(delay, queues, generatorTaskSize) },
			{ Chunk.BuildStage.MakeMesh, new ChunkGenerator(delay, queues, generatorTaskSize) }
		};
		fakeChunkRoot = new GameObject();
		fakeChunkRoot.name = "Fake Chunks";
	}

	public async void StartGen(bool instantiate)
	{
		if (instantiate)
		{
			genStage = GenStage.CreateChunks;

			// Create chunk data and GameObjects
			InstantiateChunks();

			await Task.Delay(10);
		}

		// Enqueue chunks
		await EnqueueAllChunks(Chunk.BuildStage.Init);

		genStage = GenStage.GenerateChunks;

		spawnFinder.Reset();
		goalFinder.Reset();
	}

	public void ResetSpawnFinder()
	{
		spawnFinder.Reset();
		goalFinder.Reset();
	}

	public void UpdateSpawnFinder()
	{
		if (spawnFinder.IsBusy() || spawnFinder.IsSuccessful())
			return;

		spawnFinder.Tick();
	}

	public void UpdateGoalFinder()
	{
		if (goalFinder.IsBusy() || goalFinder.IsSuccessful())
			return;

		goalFinder.Tick();
	}

	public void ContinueGenerating()
	{
		if (genStage < GenStage.EnqueueChunks || !active)
			return;

		chunksToGen = 0;
		generatorsUsed = 0;

		foreach (KeyValuePair<Chunk.BuildStage, ChunkGenerator> entry in chunkGenerators)
		{
			chunkGenerators.TryGetValue(entry.Key > 0 ? entry.Key - 1 : 0, out ChunkGenerator prev);

			chunksToGen += entry.Value.GetSize();

			bool empty = entry.Key == Chunk.BuildStage.Init;
			if (!empty && entry.Value.IsBusy())
				generatorsUsed++;

			if (empty || (prev.GetSize() == 0))
				entry.Value.Generate();
		}

		if (genStage >= GenStage.Ready)
		{
			UpdateSpawnFinder();
			UpdateGoalFinder();
		}

		foreach (KeyValuePair<Chunk.BuildStage, ChunkGenerator> entry in fakeChunkGenerators)
		{
			fakeChunkGenerators.TryGetValue(entry.Key > 0 ? entry.Key - 1 : 0, out ChunkGenerator prev);

			bool empty = entry.Key == Chunk.BuildStage.Init;

			if (empty || (prev.GetSize() == 0))
				entry.Value.Generate();
		}
	}

	public void InstantiateChunks()
	{
		int actualChunkSize = World.GetChunkSize();

		int range = genRangePlayable;

		// Go through all nearby chunk positions
		for (int x = -range; x < range; x++)
		{
			for (int y = -range; y < range; y++)
			{
				for (int z = -range; z < range; z++)
				{
					Vector3Int chunkPos = new Vector3Int(x * actualChunkSize, y * actualChunkSize, z * actualChunkSize);

					// Instantiate chunk GameObject
					ChunkGameObject chunkGO = Object.Instantiate(chunkPrefab, chunkPos, Quaternion.identity, chunkRoot.transform);
					chunkGO.name = "Chunk " + x + ", " + y + ", " + z;

					// Initialize and register chunk
					chunkGO.data = new Chunk();
					chunkGO.data.SetPos(chunkPos);

					chunkGO.data.chunkMesh.Init(chunkGO.data, chunkGO.meshVisual, chunkGO.meshPhysics);

					chunkGO.data.go = chunkGO;

					World.AddChunk(chunkPos, chunkGO.data);
				}
			}
		}

		int fakeChunkScale = 2;

		//actualChunkSize = World.GetChunkSize() * fakeChunkScale;

		range = genRangePlayable + genRangeFake;

		// Fake chunks are further away
		for (int x = -range; x < range; x += fakeChunkScale)
		{
			for (int y = -range; y < range; y += fakeChunkScale)
			{
				for (int z = -range; z < range; z += fakeChunkScale)
				{
					bool playable =
						x >= -genRangePlayable && x < genRangePlayable &&
						y >= -genRangePlayable && y < genRangePlayable &&
						z >= -genRangePlayable && z < genRangePlayable;

					if (playable)
						continue;

					Vector3Int chunkPos = new Vector3Int(x * actualChunkSize, y * actualChunkSize, z * actualChunkSize);

					// Instantiate chunk GameObject
					ChunkGameObject chunkGO = Object.Instantiate(fakeChunkPrefab, chunkPos, Quaternion.identity, fakeChunkRoot.transform);
					chunkGO.name = "FakeChunk " + x + ", " + y + ", " + z;

					// Initialize and register chunk
					chunkGO.data = new FakeChunk();
					chunkGO.data.SetPos(chunkPos);

					chunkGO.data.chunkMesh.Init(chunkGO.data, chunkGO.meshVisual, chunkGO.meshPhysics);

					chunkGO.data.go = chunkGO;

					World.AddChunk(chunkPos, chunkGO.data);
				}
			}
		}

		genStage = GenStage.EnqueueChunks;
	}

	public async Task EnqueueAllChunks(Chunk.BuildStage curChunkStage)
	{
		// First get all chunks
		foreach (var entry in World.GetAllChunks())
		{
			chunksToQueue.Enqueue(entry);
		}

		await Task.Delay(10);

		// Then go through a few at a time
		while (chunksToQueue.Count > 0)
		{
			for (int i = enqueueTaskSize; i > 0 && chunksToQueue.Count > 0; i--)
			{
				Chunk c = chunksToQueue.Dequeue().Value;
				c.buildStage = curChunkStage;

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
		ChunkGenerator generator;

		if (chunk.chunkType != Chunk.ChunkType.Close)
			fakeChunkGenerators.TryGetValue(chunk.buildStage, out generator);
		else
			chunkGenerators.TryGetValue(chunk.buildStage, out generator);

		if (generator == null)
			return;

		// Add to appropriate queue. Closer chunks have higher priority (lower value)
		Vector3Int origin = World.GetRelativeOrigin();
		float priority = (requeue ? 10 : 0) + Vector3.SqrMagnitude((chunk.position + Vector3.one * World.GetChunkSize() / 2f) - origin);
		generator.Enqueue(chunk, priority, multiQ);
	}

	public int GetGenRangePlayable()
	{
		return genRangePlayable;
	}

	public int GetGenRangeFake()
	{
		return genRangeFake;
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

		foreach (KeyValuePair<Chunk.BuildStage, ChunkGenerator> entry in chunkGenerators)
		{
			if (entry.Value.IsBusy())
				busy++;
		}

		foreach (KeyValuePair<Chunk.BuildStage, ChunkGenerator> entry in fakeChunkGenerators)
		{
		}

		return busy;
	}

	public void ChunkFinishedProcStage()
	{
		float firstTimeMult = Mathf.Approximately(progress, 0) ? 2 : 1;

		progress += firstTimeMult * (1f / World.GetRealChunkCount()) / (chunkGenerators.Count);
	}

	public float GetGenProgress()
	{
		return progress;
	}

	public Dictionary<Chunk.BuildStage, ChunkGenerator> GetChunkGenerators()
	{
		return chunkGenerators;
	}

	public void DrawGizmo()
	{
		spawnFinder.DrawGizmo();
		goalFinder.DrawGizmo();
	}
}
