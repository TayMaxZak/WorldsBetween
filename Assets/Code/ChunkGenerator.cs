using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

public class ChunkGenerator
{
	private int chunksToHandle = 3;
	private Timer chunkGenTimer;

	private static readonly List<Chunk.GenStage> requireAdjacents = new List<Chunk.GenStage> {
		//Chunk.GenStage.Allocated,
		//Chunk.GenStage.Generated,
		//Chunk.GenStage.Meshed,
		//Chunk.GenStage.Lit
	};

	private static readonly Vector3Int[] directions = new Vector3Int[] {
		new Vector3Int(1, 0, 0),
		new Vector3Int(-1, 0, 0),
		new Vector3Int(0, 1, 0),
		new Vector3Int(0, -1, 0),
		new Vector3Int(0, 0, 1),
		new Vector3Int(0, 0, -1)
	};

	public ChunkGenerator(int toHandle, float interval)
	{
		chunksToHandle = toHandle;
		chunkGenTimer = new Timer(interval, 1);
	}

	private SimplePriorityQueue<Chunk> chunkQueue = new SimplePriorityQueue<Chunk>();

	public void Enqueue(Chunk chunk, float priority)
	{
		chunkQueue.Enqueue(chunk, priority);
	}

	public void Generate(float deltaTime)
	{
		chunkGenTimer.Increment(deltaTime);

		bool doChunkGen = chunkGenTimer.Expired();

		if (doChunkGen)
			chunkGenTimer.Reset();

		if (!doChunkGen)
			return;

		IterateQueue();
	}

	private void IterateQueue()
	{
		int mult = World.AccelerateGen() ? 40 : 1;

		int count = chunkQueue.Count;
		for (int i = 0; i < Mathf.Min(count, chunksToHandle * mult); i++)
		{
			// TODO: Peek before dequeueing when meshing and lighting to prevent weird chunk borders

			Chunk chunk = chunkQueue.Dequeue();

			// Check if neighboring chunks are ready yet
			if (requireAdjacents.Contains(chunk.genStage))
			{
				bool adjGenerated = true;

				for (int d = 0; d < directions.Length; d++)
				{
					// Try every orthagonal direction
					Vector3Int adjPos = chunk.position + directions[d] * World.GetChunkSize();
					Chunk adj = World.GetChunkFor(adjPos);
					if (adj == null || adj.genStage < chunk.genStage)
					{
						adjGenerated = false;
						break;
					}
				}

				if (!adjGenerated)
				{
					// Re add to queue, at a lower priority
					World.QueueNextStage(chunk, chunk.genStage, true);
					continue;
				}
			}

			ProcessChunk(chunk);
		}
	}

	private void ProcessChunk(Chunk chunk)
	{
		switch (chunk.genStage)
		{
			case Chunk.GenStage.Empty: // Create blocks
				{
					chunk.Init(World.GetChunkSize());

					chunk.genStage = Chunk.GenStage.Allocated;
					World.QueueNextStage(chunk, chunk.genStage);
				}
				break;
			case Chunk.GenStage.Allocated: // Generate terrain
				{
					List<Modifier> modifiers = World.GetModifiers();

					for (int i = 0; i < modifiers.Count; i++)
						chunk.ApplyModifier(modifiers[i], i == 0, i == modifiers.Count - 1);

					chunk.CacheNearAir();

					chunk.genStage = Chunk.GenStage.Generated;
					World.QueueNextStage(chunk, chunk.genStage);
				}
				break;
			case Chunk.GenStage.Generated: // Cache data and build mesh
				{
					chunk.UpdateOpacityVisuals();

					chunk.genStage = Chunk.GenStage.Meshed;
					World.QueueNextStage(chunk, chunk.genStage);
				}
				break;
			case Chunk.GenStage.Meshed: // Calculate lights
				{
					chunk.CalculateLight();

					chunk.genStage = Chunk.GenStage.Lit;
					World.QueueNextStage(chunk, chunk.genStage);
				}
				break;
			case Chunk.GenStage.Lit: // Light visuals, spawn entities, and other stuff
				{
					chunk.UpdateLightVisuals();

					chunk.genStage = Chunk.GenStage.Ready;
					World.QueueNextStage(chunk, chunk.genStage);
				}
				break;
		}
	}

	public int GetSize()
	{
		return chunkQueue.Count;
	}
}
