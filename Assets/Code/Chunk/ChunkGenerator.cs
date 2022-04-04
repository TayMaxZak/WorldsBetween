using System.Collections;
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

	public readonly int throughput = 4;
	private readonly float cycleDelay;
	private readonly int penaltyDelay = 100; // In ms

	private int edgeChunks = 0;

	private static readonly List<Chunk.BuildStage> requireAdjacents = new List<Chunk.BuildStage> {
		Chunk.BuildStage.Generate,
		Chunk.BuildStage.MakeMesh,
	};

	public ChunkGenerator(float cycleDelay, int queueCount, int throughput)
	{
		this.cycleDelay = cycleDelay;

		for (int i = 0; i < queueCount; i++)
		{
			SimplePriorityQueue<Chunk> spq = new SimplePriorityQueue<Chunk>();

			chunkQueues.Add(spq);
			busy.Add(spq, false);
		}

		this.throughput = throughput;
	}

	private Color GetDebugColor(int queueID)
	{
		queueID = Mathf.Abs(queueID);
		if (queueID % 3 == 0)
			return Utils.colorCyan;
		if (queueID % 3 == 1)
			return Utils.colorBlue;
		if (queueID % 3 == 2)
			return Utils.colorDarkGrayBlue;
		else
			return Color.white;
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
				BackgroundIterate(spq, throughput);
		}

		while (reQueue.Count > 0)
			World.WorldBuilder.QueueNextStage(reQueue.Dequeue(), true);
	}

	private async void BackgroundIterate(SimplePriorityQueue<Chunk> queue, int taskSize)
	{
		busy[queue] = true;

		Vector3 prevChunkPos = Vector3.zero;

		int t = taskSize;
		while (GetSize() > 0 && t > 0)
		{
			await Task.Delay(Mathf.CeilToInt(cycleDelay * 1000));

			if (queue.Count <= 0)
				break;

			t--;

			Chunk chunk = queue.Dequeue();

			bool validAdj = true;
			bool requiresAdj = requireAdjacents.Contains(chunk.buildStage);

			// Check if neighboring chunks are ready yet
			if (requiresAdj)
			{
				for (int i = -1; i <= 1; i++)
				{
					for (int j = -1; j <= 1; j++)
					{
						for (int k = -1; k <= 1; k++)
						{
							if (i == 0 && j == 0 && k == 0)
								continue;

							//if (Mathf.Abs(i) != Mathf.Abs(j) || Mathf.Abs(j) != Mathf.Abs(k) || Mathf.Abs(k) != Mathf.Abs(i))
							//	continue;

							// Try every orthagonal and diagonal direction
							Vector3Int adjPos = chunk.position + new Vector3Int(i, j, k) * World.GetChunkSize();
							Chunk adj = World.GetChunk(adjPos);
							if (adj == null || adj.buildStage < chunk.buildStage || adj.isProcessing)
							{
								// Wait for threaded processing
								while (adj != null && (adj.isProcessing || adj.buildStage < chunk.buildStage))
									await Task.Delay(penaltyDelay);
							}
						}
					}
				}
			}

			// Either doesn't care about adjacents or has adjacents
			if (!requiresAdj || validAdj)
			{
				// Update edge tracking
				//if (chunk.atEdge)
				//	edgeChunks--;
				//chunk.atEdge = false;

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
				//if (!chunk.atEdge)
				//{
				//	edgeChunks++;
				//	chunk.atEdge = true;
				//}

				reQueue.Enqueue(chunk);

				// Sit tight until other generators have made some progress
				await Task.Delay(Mathf.CeilToInt(penaltyDelay));
			}

			Vector3 midPos1 = Vector3.Lerp(chunk.position + Vector3.one * 8, prevChunkPos, 0.67f) + prevChunkPos.normalized * 3;
			Vector3 midPos2 = Vector3.Lerp(chunk.position + Vector3.one * 8, prevChunkPos, 0.33f) + prevChunkPos.normalized * 1.5f;

			Color debugColor = GetDebugColor(queue.GetHashCode());

			Debug.DrawLine(chunk.position + Vector3.one * 8, midPos2, debugColor, 0.5f);

			Debug.DrawLine(midPos2, midPos1, debugColor, 0.3f);

			Debug.DrawLine(midPos1, prevChunkPos, debugColor, 0.1f);

			prevChunkPos = chunk.position + Vector3.one * 8;
		}

		busy[queue] = false;
	}

	private void ProcessChunk(Chunk chunk)
	{
		switch (chunk.buildStage)
		{
			case Chunk.BuildStage.Init: // Create blocks
				{
					chunk.Init(World.GetChunkSize());

					chunk.buildStage = Chunk.BuildStage.Generate;
					World.WorldBuilder.QueueNextStage(chunk);

					chunk.OnFinishProcStage();
				}
				break;
			case Chunk.BuildStage.Generate: // Generate terrain
				{
					chunk.AsyncGenerate(Modifier.ModifierStage.Terrain);
				}
				break;
			case Chunk.BuildStage.MakeMesh: // Cache data and build mesh
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
