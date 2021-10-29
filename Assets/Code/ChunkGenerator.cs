using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

public class ChunkGenerator
{
	private Timer chunkGenTimer = new Timer(1, 1);
	private int chunksToHandle = 1;

	private SimplePriorityQueue<Chunk> chunkQueue = new SimplePriorityQueue<Chunk>();

	public void GenChunks(float deltaTime)
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
			case Chunk.GenStage.Empty:
				{
					chunk.Init(World.GetChunkSize());

					chunk.genStage = Chunk.GenStage.Allocated;

					World.QueueNextStage(chunk, Chunk.GenStage.Allocated);
				}
				break;
			case Chunk.GenStage.Allocated:
				break;
			case Chunk.GenStage.Generated:
				break;
			case Chunk.GenStage.Meshed:
				break;
			case Chunk.GenStage.Lit:
				break;
			case Chunk.GenStage.Ready:
				break;
		}
	}
}
