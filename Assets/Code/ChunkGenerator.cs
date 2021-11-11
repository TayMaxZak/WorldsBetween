using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;
using System.Threading.Tasks;

public class ChunkGenerator
{
	public enum ThreadingMode
	{
		Background,
		FullUsage
	}

	private static ThreadingMode threadingMode = ThreadingMode.Background;
	private bool busy = false;
	private bool wait = false;

	private readonly SimplePriorityQueue<Chunk> chunkQueue = new SimplePriorityQueue<Chunk>();

	private readonly Queue<Chunk> reQueue = new Queue<Chunk>();

	private readonly int chunksToHandle = 3;
	private readonly Timer chunkGenTimer;

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
		if (!chunkQueue.Contains(chunk)) // Avoid duplicates
			chunkQueue.Enqueue(chunk, priority);
	}

	public void Generate(float deltaTime)
	{
		if (threadingMode == ThreadingMode.Background && busy)
		{
			return;
		}

		chunkGenTimer.Increment(deltaTime);

		bool doChunkGen = chunkGenTimer.Expired();

		if (doChunkGen)
			chunkGenTimer.Reset(Mathf.Clamp(deltaTime, chunkGenTimer.maxTime, 1));

		if (!doChunkGen)
			return;

		IterateQueue();

		while (reQueue.Count > 0)
			World.QueueNextStage(reQueue.Dequeue(), true);
	}

	private void IterateQueue()
	{
		if (threadingMode == ThreadingMode.FullUsage)
		{
			// Never busy in this mode
			busy = false;

			FullUsageIterate();
		}
		else if (threadingMode == ThreadingMode.Background)
		{
			BackgroundIterate();
		}
	}

	private void FullUsageIterate()
	{
		float accelMult = 50;
		int count = chunkQueue.Count;
		int baseAttempts = Mathf.Min(count, Mathf.CeilToInt(chunksToHandle * accelMult));
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
					if (adj == null || adj.genStage < chunk.genStage || chunk.isProcessing)
					{
						if (adj != null || World.IsInfinite())
						{
							validAdj = false;
							break;
						}
						else
						{
							continue;
						}
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

				if (!chunk.isProcessing)
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

	private async void BackgroundIterate()
	{
		busy = true;

		while (GetSize() > 0)
		{
			// Set externally; sit tight until other queues have made some progress
			while (wait == true)
			{
				await Task.Yield();
			}

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
					if (adj == null || adj.genStage < chunk.genStage || chunk.isProcessing)
					{
						if (adj != null || World.IsInfinite())
						{
							validAdj = false;
							break;
						}
						else
						{
							continue;
						}
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
			else
			{
				// Update edge tracking
				if (!chunk.atEdge)
				{
					edgeChunks++;
					chunk.atEdge = true;
				}

				reQueue.Enqueue(chunk);
			}
		}

		busy = false;
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
					chunk.AsyncGenerate();
				}
				break;
			case Chunk.GenStage.Generated: // Cache data and build mesh
				{
					chunk.AsyncMakeMesh();
				}
				break;
			case Chunk.GenStage.Meshed: // Calculate lights
				{
					chunk.AsyncCalcLight();
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

	public bool IsBusy()
	{
		return threadingMode == ThreadingMode.Background && busy;
	}

	public void SetWait(bool newWait)
	{
		wait = newWait;
	}

	public int GetEdgeChunks()
	{
		return edgeChunks;
	}

	public static void SetThreadingMode(ThreadingMode newMode)
	{
		threadingMode = newMode;
	}
}
