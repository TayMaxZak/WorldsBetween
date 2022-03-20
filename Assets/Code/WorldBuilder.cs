﻿using System.Collections;
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

	private Dictionary<Chunk.ProcStage, ChunkGenerator> chunkGenerators = new Dictionary<Chunk.ProcStage, ChunkGenerator>();
	private Dictionary<Chunk.ProcStage, ChunkGenerator> fakeChunkGenerators = new Dictionary<Chunk.ProcStage, ChunkGenerator>();


	private Queue<KeyValuePair<Vector3Int, Chunk>> chunksToQueue = new Queue<KeyValuePair<Vector3Int, Chunk>>();

	public void Init()
	{
		float delay = 0.0f;

		chunkGenerators = new Dictionary<Chunk.ProcStage, ChunkGenerator>()
		{
			{ Chunk.ProcStage.Init, new ChunkGenerator(0, 1, enqueueTaskSize) },
			{ Chunk.ProcStage.Generate, new ChunkGenerator(delay, queues, generatorTaskSize) },
			{ Chunk.ProcStage.MakeMesh, new ChunkGenerator(delay, queues, generatorTaskSize) }
		};
		chunkRoot = new GameObject();
		chunkRoot.name = "Chunks";

		fakeChunkGenerators = new Dictionary<Chunk.ProcStage, ChunkGenerator>()
		{
			{ Chunk.ProcStage.Init, new ChunkGenerator(0, 1, enqueueTaskSize) },
			{ Chunk.ProcStage.Generate, new ChunkGenerator(delay, queues, generatorTaskSize) },
			{ Chunk.ProcStage.MakeMesh, new ChunkGenerator(delay, queues, generatorTaskSize) }
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
		await EnqueueAllChunks(Chunk.ProcStage.Init);

		genStage = GenStage.GenerateChunks;

		spawnFinder.Reset();
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

			bool empty = entry.Key == Chunk.ProcStage.Init;
			if (!empty && entry.Value.IsBusy())
				generatorsUsed++;

			if (empty || (prev.GetSize() == 0))
				entry.Value.Generate();
		}

		if (genStage >= GenStage.Ready)
			UpdateSpawnFinder();

		foreach (KeyValuePair<Chunk.ProcStage, ChunkGenerator> entry in fakeChunkGenerators)
		{
			fakeChunkGenerators.TryGetValue(entry.Key > 0 ? entry.Key - 1 : 0, out ChunkGenerator prev);

			chunksToGen += entry.Value.GetSize();

			bool empty = entry.Key == Chunk.ProcStage.Init;
			if (!empty && entry.Value.IsBusy())
				generatorsUsed++;

			if (empty || (prev.GetSize() == 0))
				entry.Value.Generate();
		}
	}

	public void InstantiateChunks()
	{
		int actualChunkSize = World.GetChunkSize();

		// Start pos in chunk coordinates
		Vector3Int origin = World.GetRelativeOrigin() / actualChunkSize;

		int range = genRangePlayable + genRangeFake;

		// Go through all nearby chunk positions
		for (int x = origin.x - range; x < origin.x + range; x++)
		{
			for (int y = origin.y - range; y < origin.y + range; y++)
			{
				for (int z = origin.z - range; z < origin.z + range; z++)
				{
					bool playable = Mathf.Abs(x + 0.5f) < genRangePlayable && Mathf.Abs(y + 0.5f) < genRangePlayable && Mathf.Abs(z + 0.5f) < genRangePlayable;

					Vector3Int chunkPos = new Vector3Int(x * actualChunkSize, y * actualChunkSize, z * actualChunkSize);

					// Instantiate chunk GameObject
					ChunkGameObject chunkGO = Object.Instantiate(playable ? chunkPrefab : fakeChunkPrefab, chunkPos, Quaternion.identity, playable ? chunkRoot.transform : fakeChunkRoot.transform);
					chunkGO.name = (playable ? "Chunk " : "FakeChunk ") + x + ", " + y + ", " + z;

					// Initialize and register chunk
					chunkGO.data = (playable ? new Chunk() : new FakeChunk());
					chunkGO.data.SetPos(chunkPos);

					chunkGO.data.chunkMesh.Init(chunkGO.data, chunkGO.filter);

					chunkGO.data.go = chunkGO;

					World.AddChunk(chunkPos, chunkGO.data);
				}
			}
		}

		genStage = GenStage.EnqueueChunks;
	}

	public async Task EnqueueAllChunks(Chunk.ProcStage curChunkStage)
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
		ChunkGenerator generator;

		if (chunk.isFake)
			fakeChunkGenerators.TryGetValue(chunk.procStage, out generator);
		else
			chunkGenerators.TryGetValue(chunk.procStage, out generator);

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

		foreach (KeyValuePair<Chunk.ProcStage, ChunkGenerator> entry in chunkGenerators)
		{
			if (entry.Value.IsBusy())
				busy++;
		}

		foreach (KeyValuePair<Chunk.ProcStage, ChunkGenerator> entry in fakeChunkGenerators)
		{
			if (entry.Value.IsBusy())
				busy++;
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

	public Dictionary<Chunk.ProcStage, ChunkGenerator> GetChunkGenerators()
	{
		return chunkGenerators;
	}

	public void DrawGizmo()
	{
		spawnFinder.DrawGizmo();
	}
}
