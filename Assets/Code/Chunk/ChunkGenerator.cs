﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;
using System.Threading.Tasks;
using System.Linq;

public class ChunkGenerator
{
	private readonly List<SimplePriorityQueue<Chunk>> chunkQueues = new List<SimplePriorityQueue<Chunk>>();
	private readonly Dictionary<SimplePriorityQueue<Chunk>, bool> busy = new Dictionary<SimplePriorityQueue<Chunk>, bool>();

	private readonly Queue<Chunk> reQueue = new Queue<Chunk>();

	private readonly float cycleDelay;
	private readonly int penaltyDelay = 5; // In ms

	private int edgeChunks = 0;

	private static readonly List<Chunk.ProcStage> requireAdjacents = new List<Chunk.ProcStage> {
		Chunk.ProcStage.Generate,
		Chunk.ProcStage.MakeSurface,
	};

	private static readonly Vector3Int[] directions = new Vector3Int[] {
		new Vector3Int(1, 0, 0),
		new Vector3Int(-1, 0, 0),
		new Vector3Int(0, 1, 0),
		new Vector3Int(0, -1, 0),
		new Vector3Int(0, 0, 1),
		new Vector3Int(0, 0, -1)
	};

	public ChunkGenerator(float cycleDelay, int queueCount)
	{
		this.cycleDelay = cycleDelay;

		for (int i = 0; i < queueCount; i++)
		{
			SimplePriorityQueue<Chunk> spq = new SimplePriorityQueue<Chunk>();

			chunkQueues.Add(spq);
			busy.Add(spq, false);
		}
	}

	// Fix extra queues not being used
	public void Enqueue(Chunk chunk, float priority, bool useMultiQueue)
	{
		foreach (int i in Enumerable.Range(0, useMultiQueue ? chunkQueues.Count : 1).OrderBy(x => SeedlessRandom.NextInt()))
		{
			SimplePriorityQueue<Chunk> spq = chunkQueues[i];

			// Check for duplicates, and try next queues if necessary
			if (!spq.Contains(chunk))
			{
				spq.Enqueue(chunk, priority);
				return;
			}

			// Only try first queue
			if (!useMultiQueue)
				return;
		}
	}

	public void Generate()
	{
		// Only allow generate non-busy queues
		foreach (SimplePriorityQueue<Chunk> spq in chunkQueues)
		{
			if (!busy[spq])
				BackgroundIterate(spq, 32);
		}

		while (reQueue.Count > 0)
			World.Generator.QueueNextStage(reQueue.Dequeue(), true);
	}

	private async void BackgroundIterate(SimplePriorityQueue<Chunk> queue, int taskSize)
	{
		busy[queue] = true;

		int t = taskSize;
		while (GetSize() > 0 && t > 0)
		{
			await Task.Delay(Mathf.CeilToInt(cycleDelay * 1000));

			if (queue.Count <= 0)
				break;

			t--;

			Chunk chunk = queue.Dequeue();

			bool validAdj = true;
			bool requiresAdj = requireAdjacents.Contains(chunk.procStage);

			// Check if neighboring chunks are ready yet
			if (requiresAdj)
			{
				for (int d = 0; d < directions.Length; d++)
				{
					// Try every orthagonal direction
					Vector3Int adjPos = chunk.position + directions[d] * World.GetChunkSize();
					Chunk adj = World.GetChunkFor(adjPos);
					if (adj == null || adj.procStage < chunk.procStage || adj.isProcessing)
					{
						// Wait for threaded processing
						while (adj != null && (adj.isProcessing || adj.procStage < chunk.procStage))
							await Task.Delay(10);
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

				// Not already processing? Then start
				if (!chunk.isProcessing)
				{
					ProcessChunk(chunk);

					// Does this process lead to a 'processing' state?
					while (chunk.isProcessing == true)
					{
						await Task.Yield(); // Wait before continuing
					}
				}
				else
				{
					while (chunk.isProcessing == true)
					{
						await Task.Yield(); // Wait before continuing
					}
				}
			}
			// Unable to complete this chunk
			else
			{
				// Update edge tracking
				if (!chunk.atEdge)
				{
					edgeChunks++;
					chunk.atEdge = true;
				}

				reQueue.Enqueue(chunk);

				// Sit tight until other generators have made some progress
				await Task.Delay(Mathf.CeilToInt(penaltyDelay));
			}
		}

		busy[queue] = false;
	}

	private void ProcessChunk(Chunk chunk)
	{
		switch (chunk.procStage)
		{
			case Chunk.ProcStage.Allocate: // Create blocks
				{
					chunk.Init(World.GetChunkSize());

					chunk.procStage = Chunk.ProcStage.Generate;
					World.Generator.QueueNextStage(chunk);
				}
				break;
			case Chunk.ProcStage.Generate: // Generate terrain
				{
					chunk.AsyncGenerate();
				}
				break;
			case Chunk.ProcStage.MakeSurface: // Cache data and build mesh
				{
					chunk.AsyncMakeMesh();
				}
				break;
		}
	}

	public int GetSize()
	{
		int total = 0;

		foreach (SimplePriorityQueue<Chunk> spq in chunkQueues)
			total += spq.Count;

		return total - edgeChunks;
	}

	public bool IsBusy()
	{
		foreach (SimplePriorityQueue<Chunk> spq in chunkQueues)
		{
			if (busy[spq])
				return true;
		}
		return false;
	}

	public int GetEdgeChunks()
	{
		return edgeChunks;
	}
}
