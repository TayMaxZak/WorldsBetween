using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

public class ChunkGenerator
{
	private SimplePriorityQueue<Chunk> chunkQueue = new SimplePriorityQueue<Chunk>();
	private Queue<Chunk> reQueue = new Queue<Chunk>();

	private int chunksToHandle = 3;
	private Timer chunkGenTimer;

	private int edgeChunks = 0;

	private static readonly List<Chunk.GenStage> requireAdjacents = new List<Chunk.GenStage> {
		Chunk.GenStage.Allocated,
		Chunk.GenStage.Generated,
		Chunk.GenStage.Meshed,
		Chunk.GenStage.Lit
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

		while (reQueue.Count > 0)
			World.QueueNextStage(reQueue.Dequeue(), true);
	}

	private void IterateQueue()
	{
		int accelMult = World.DoAccelerateGen() ? 40 : 1;

		int count = chunkQueue.Count;
		int baseAttempts = Mathf.Min(count, chunksToHandle * accelMult);
		int spareAttempts = Mathf.Min(count);

		for (int i = 0; i < baseAttempts; i++)
		{
			Chunk chunk = chunkQueue.Dequeue();

			bool validAdj = true;
			bool requiresAdj = requireAdjacents.Contains(chunk.genStage);

			// Check if neighboring chunks are ready yet
			if (requiresAdj)
			{
				for (int d = 0; d < directions.Length; d++)
				{
					// Try every orthagonal direction
					Vector3Int adjPos = chunk.position + directions[d] * World.GetChunkSize();
					Chunk adj = World.GetChunkFor(adjPos);
					if (adj == null || adj.genStage < chunk.genStage)
					{
						validAdj = false;
						break;
					}
				}
			}

			// Either doesn't care about adjacents or has adjacents
			if (!requiresAdj || validAdj)
			{
				// Update edge tracking
				if (chunk.atEdge)
					edgeChunks--;
				chunk.atEdge = false;

				ProcessChunk(chunk);
			}
			else
			{
				// Update edge tracking
				if (!chunk.atEdge)
				{
					edgeChunks++;
					chunk.atEdge = true;
				}

				reQueue.Enqueue(chunk);

				// Keep trying to find a non-edge chunk (if it makes sense to do so)
				// Do we have spare attempts left?
				// Is the queue still non empty after we do the remaining attempts? (remaining attempts = base attempts - current i)
				if (spareAttempts > 0 && chunkQueue.Count - (baseAttempts - i) >= 0)
				{
					spareAttempts--;
					i--;
				}
			}
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
					World.QueueNextStage(chunk);
				}
				break;
			case Chunk.GenStage.Allocated: // Generate terrain
				{
					List<Modifier> modifiers = World.GetModifiers();

					for (int i = 0; i < modifiers.Count; i++)
						chunk.ApplyModifier(modifiers[i], i == 0, i == modifiers.Count - 1);

					chunk.CacheNearAir();

					chunk.genStage = Chunk.GenStage.Generated;
					World.QueueNextStage(chunk);
				}
				break;
			case Chunk.GenStage.Generated: // Cache data and build mesh
				{
					chunk.UpdateOpacityVisuals();

					chunk.genStage = Chunk.GenStage.Meshed;
					World.QueueNextStage(chunk);
				}
				break;
			case Chunk.GenStage.Meshed: // Calculate lights
				{
					chunk.CalculateLight();

					chunk.genStage = Chunk.GenStage.Lit;
					World.QueueNextStage(chunk);
				}
				break;
			case Chunk.GenStage.Lit: // Light visuals, spawn entities, and other stuff
				{
					chunk.UpdateLightVisuals();

					chunk.genStage = Chunk.GenStage.Ready;
					World.QueueNextStage(chunk);
				}
				break;
		}
	}

	public int GetSize()
	{
		return chunkQueue.Count - edgeChunks;
	}

	public int GetEdgeChunks()
	{
		return edgeChunks;
	}
}
