using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

public class ChunkGenerator
{
	private int chunksToHandle = 3;
	private Timer chunkGenTimer;

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
		int count = chunkQueue.Count;
		for (int i = 0; i < Mathf.Min(count, chunksToHandle); i++)
		{
			// TODO: Peek before dequeueing when meshing and lighting to prevent weird chunk borders

			Chunk chunk = chunkQueue.Dequeue();

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

					chunk.genStage = Chunk.GenStage.Generated;
					World.QueueNextStage(chunk, chunk.genStage);
				}
				break;
			case Chunk.GenStage.Generated: // Cache data and build mesh
				{
					chunk.CacheNearAir();

					chunk.UpdateOpacityVisuals();

					chunk.genStage = Chunk.GenStage.Meshed;
					World.QueueNextStage(chunk, chunk.genStage);
				}
				break;
			case Chunk.GenStage.Meshed: // Calculate lights and apply vertex colors
				{
					// TODO
				}
				break;
			case Chunk.GenStage.Lit: // Spawn entities and other stuff
				break;
		}
	}
}
