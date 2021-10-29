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
		chunkGenTimer = new Timer(interval, 5);
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
					World.QueueNextStage(chunk, Chunk.GenStage.Allocated);
				}
				break;
			case Chunk.GenStage.Allocated: // Generate terrain
				{
					// TODO
				}
				break;
			case Chunk.GenStage.Generated: // Cache data and build mesh
				{
					chunk.CacheNearAir();

					chunk.UpdateOpacityVisuals();

					chunk.genStage = Chunk.GenStage.Meshed;
					World.QueueNextStage(chunk, Chunk.GenStage.Meshed);
				}
				break;
			case Chunk.GenStage.Meshed: // Calculate lights and apply vertex colors
				{
					Debug.Log(chunk.name + " done");
					// TODO
				}
				break;
			case Chunk.GenStage.Lit:
				break;
		}
	}
}
