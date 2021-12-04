using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[System.Serializable]
public class WorldGenerator
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

	[Header("References")]
	[SerializeField]
	private ChunkGameObject chunkPrefab;

	[Header("Generation")]
	[SerializeField]
	private int genRange = 8;

	private Timer chunkGenTimer = new Timer(1);

	private int generatorsUsed = 0;
	private int chunksToGen = 0;

	private GameObject chunkRoot;
	private Dictionary<Chunk.GenStage, ChunkGenerator> chunkGenerators = new Dictionary<Chunk.GenStage, ChunkGenerator>();

	public void Init()
	{
		float delay = 0f;
		int queues = 16;
		chunkGenerators = new Dictionary<Chunk.GenStage, ChunkGenerator>()
		{
			{ Chunk.GenStage.Allocate, new ChunkGenerator(0, 1) },
			{ Chunk.GenStage.Generate, new ChunkGenerator(delay, queues) },
			{ Chunk.GenStage.MakeSurface, new ChunkGenerator(delay, queues) },
			{ Chunk.GenStage.CalcLight, new ChunkGenerator(delay, queues) },
			{ Chunk.GenStage.ApplyVertexColorsA, new ChunkGenerator(delay, queues) },
			{ Chunk.GenStage.AmbientLight, new ChunkGenerator(delay, queues) },
			{ Chunk.GenStage.ApplyVertexColorsB, new ChunkGenerator(delay, queues) },
		};

		chunkGenTimer.Reset();

		chunkRoot = new GameObject();
		chunkRoot.name = "Chunks";
	}

	public async void InitialGen()
	{
		genStage = GenStage.CreateChunks;

		// First batch of chunks
		CreateChunksNearPlayer(genRange);

		await Task.Delay(1000);

		// Enqueue all chunks afterwards
		EnqueueAllChunks();

		genStage = GenStage.GenerateChunks;

		await Task.Delay(1000);

		GameManager.Instance.MidLoading();
	}

	public void ContinueGenerating()
	{
		if (genStage < GenStage.GenerateChunks || !active)
			return;

		chunksToGen = 0;
		generatorsUsed = 0;


		foreach (KeyValuePair<Chunk.GenStage, ChunkGenerator> entry in chunkGenerators)
		{
			chunksToGen += entry.Value.GetSize();

			bool empty = entry.Key == Chunk.GenStage.Allocate;
			if (!empty && entry.Value.IsBusy())
				generatorsUsed++;

			entry.Value.Generate();
		}

		// Playable yet?
		if (GenProgress() >= 0.67f || Mathf.Approximately(GenProgress(), 1))
			GameManager.Instance.FinishLoading();
	}

	public void CreateChunksNearPlayer(int range)
	{
		// TODO: Reuse grid of chunks instead of instantiating new ones

		// Change range to actual distance
		int chunkSize = World.GetChunkSize();
		range *= chunkSize;

		Transform origin = World.GetRelativeOrigin();

		// Start pos in chunk coordinates
		Vector3Int startPos = new Vector3Int(
			Mathf.FloorToInt(origin.position.x / chunkSize) * chunkSize,
			Mathf.FloorToInt(origin.position.y / chunkSize) * chunkSize,
			Mathf.FloorToInt(origin.position.z / chunkSize) * chunkSize
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

					// Add chunk to generator
					//;
				}
			}
		}

		genStage = GenStage.EnqueueChunks;
	}

	public void EnqueueAllChunks()
	{
		foreach (KeyValuePair<Vector3Int, Chunk> entry in World.GetChunks())
			QueueNextStage(entry.Value);
	}

	private void UpdateChunkGameObjects()
	{
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
		QueueNextStage(chunk, false);
	}

	public void QueueNextStage(Chunk chunk, bool requeue)
	{
		chunkGenerators.TryGetValue(chunk.genStage, out ChunkGenerator generator);

		if (generator == null)
			return;

		// Add to appropriate queue. Closer chunks have higher priority (lower value)
		Transform origin = World.GetRelativeOrigin();
		if (!origin)
			return;
		float priority = (requeue ? -4 : 0) + Vector3.SqrMagnitude((chunk.position + Vector3.one * World.GetChunkSize() / 2f) - origin.position);
		generator.Enqueue(chunk, priority, multiQ);
	}

	public int GetRange()
	{
		return genRange;
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

	public float GenProgress()
	{
		int totalChunks = Mathf.Max(World.GetChunks().Count, 1);
		float progress = (totalChunks - chunksToGen) / (float)totalChunks;

		return progress;
	}

	public Dictionary<Chunk.GenStage, ChunkGenerator> GetChunkGenerators()
	{
		return chunkGenerators;
	}
}
