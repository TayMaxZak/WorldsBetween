using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Priority_Queue;
using System.ComponentModel;

[System.Serializable]
public class LightEngine
{
	private readonly Queue<Chunk> chunkQueue = new Queue<Chunk>();
	private readonly Dictionary<Vector3Int, BlockLight> lightList = new Dictionary<Vector3Int, BlockLight>();

	private float lengthPerRay = 16;

	[SerializeField]
	private int chunksPerBatch = 1;

	[SerializeField]
	private float rayProgressStep = 0.5f;

	private int numChunksCompleted;
	private int numChunksTarget = -1;
	private bool allChunksFinished;

	private int chunksBusy;

	[SerializeField]
	private Color waterBounceTint = Color.cyan;

	public void Init()
	{

	}

	public async Task Begin()
	{
		chunkQueue.Clear();
		lightList.Clear();

		int sourceCount = 0;
		foreach (var chunk in World.GetAllChunks())
		{
			sourceCount++;
		}

		numChunksCompleted = 0;
		numChunksTarget = sourceCount;
		allChunksFinished = false;

		Debug.Log(numChunksTarget + " chunks to be lit");

		foreach (var chunk in World.GetAllChunks())
		{
			chunkQueue.Enqueue(chunk.Value);
			// Record block lights
			foreach (BlockLight light in chunk.Value.GetBlockLights())
			{
				// For distance searching later, index lights by position
				lightList.Add(light.blockPos, light);

				Debug.DrawRay(light.blockPos + Vector3.one * 0.5f, Vector3.up, light.GetLightColor(1), 150);
			}

			//Iterate();
			//await Task.Delay(1);
		}

		Iterate();
	}

	private void Iterate()
	{
		if (!Application.isPlaying)
			return;

		if (numChunksCompleted == numChunksTarget && !allChunksFinished)
		{
			allChunksFinished = true;
			// Transfer lighting voxels from individual arrays to one shared array
			WorldLightAtlas.Instance.AggregateChunkLighting();
			// Write to one shared texture for shaders
			WorldLightAtlas.Instance.UpdateLightTextures();
		}

		//// Already busy?
		//if (chunksBusy > 0)
		//	return;

		// Otherwise, start lighting more chunks
		for (int i = 0; i < chunksPerBatch; i++)
		{
			// Still have new chunks to light?
			if (chunkQueue.Count > 0 && chunksBusy < chunksPerBatch)
			{
				Chunk chunk = chunkQueue.Dequeue();
				AsyncLightChunk(chunk);
			}
			// None left
			else break;
		}
	}

	public void AsyncLightChunk(Chunk chunk)
	{
		BkgThreadLightChunk(this, System.EventArgs.Empty, chunk);
	}

	private void BkgThreadLightChunk(object sender, System.EventArgs e, Chunk chunk)
	{
		chunksBusy++;

		BackgroundWorker bw = new BackgroundWorker();

		// What to do in the background thread
		bw.DoWork += new DoWorkEventHandler(
		delegate (object o, DoWorkEventArgs args)
		{
			// Threaded chunk lighting function
			LightChunk(chunk);
		});

		// What to do when worker completes its task
		bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
		delegate (object o, RunWorkerCompletedEventArgs args)
		{
			chunksBusy--;

			numChunksCompleted++;

			Iterate();
		});

		bw.RunWorkerAsync();
	}

	private void LightChunk(Chunk chunk)
	{
		for (int x = 0; x < World.GetChunkSize(); x++)
		{
			for (int y = 0; y < World.GetChunkSize(); y++)
			{
				for (int z = 0; z < World.GetChunkSize(); z++)
				{
					// Opaque blocks are unlit
					if (chunk.GetBlock(x, y, z).IsOpaque())
						continue;

					chunk.SetLighting(x, y, z, LightBlock(new Vector3Int(chunk.position.x + x, chunk.position.y + y, chunk.position.z + z)));
				}
			}
		}
	}

	private Color LightBlock(Vector3Int pos)
	{
		Color output = Color.black;

		foreach (var light in lightList)
		{
			// Only nearby lights
			if (Utils.DistSquared(pos, light.Key) <= lengthPerRay * lengthPerRay)
			{
				float lightStrength = Mathf.Clamp01(1f - (1f / lengthPerRay) * Mathf.Sqrt(Utils.DistSquared(pos, light.Key)));
				lightStrength *= lightStrength;

				Color result = (1 / 2f) * lightStrength * light.Value.GetLightColor(lightStrength);
				result *= ShadowAtten(pos, light.Key);
				output += result;
			}
		}

		return output;
	}

	private float ShadowAtten(Vector3Int startBlockPos, Vector3Int lightBlockPos)
	{
		bool showDebug = SeedlessRandom.NextFloat() < 0.001f;

		Vector3 dir = ((Vector3)(lightBlockPos - startBlockPos)).normalized;
		Vector3 curPos = startBlockPos + Vector3.one * 0.5f;

		Vector3Int curBlockPos = new Vector3Int(
			Mathf.FloorToInt(curPos.x),
			Mathf.FloorToInt(curPos.y),
			Mathf.FloorToInt(curPos.z)
		);

		float progress = 0;
		int curStep = 0;
		float maxStep = lengthPerRay / rayProgressStep;
		while (curStep < maxStep)
		{
			// Reached light, return 1
			if (Vector3.Dot(dir, (lightBlockPos + Vector3.one * 0.5f) - curPos) < 0)
			{
				if (showDebug)
					Debug.DrawLine(startBlockPos + Vector3.one * 0.5f, lightBlockPos + Vector3.one * 0.5f, Color.white, 10);
				return 1;
			}

			// These should be impossible. Just in case
			if (!World.Contains(curPos))
			{
				return -10;
			}
			Chunk chunk = World.GetChunk(curBlockPos);
			if (chunk == null)
			{
				return -10;
			}
			if (chunk.buildStage < Chunk.BuildStage.Done)
			{
				return -10;
			}

			// Should block light? Check if inside opaque block that isn't the light source
			if (!curBlockPos.Equals(lightBlockPos) && World.GetBlock(curBlockPos.x, curBlockPos.y, curBlockPos.z).IsOpaque())
			{
				//if (showDebug)
				//	Debug.DrawLine(startPos + Vector3.one * 0.5f, lightPos + Vector3.one * 0.5f, Color.black, 10);
				return 0;
			}

			// Move cursor
			curStep++;

			progress += rayProgressStep;

			curPos = new Vector3(
				startBlockPos.x + 0.5f + (progress * dir.x),
				startBlockPos.y + 0.5f + (progress * dir.y),
				startBlockPos.z + 0.5f + (progress * dir.z)
			);

			curBlockPos = new Vector3Int(
				Mathf.FloorToInt(curPos.x),
				Mathf.FloorToInt(curPos.y),
				Mathf.FloorToInt(curPos.z)
			);
		} // y

		// Should not happen
		return 0;
	}

	public bool IsBusy()
	{
		return chunksBusy > 0;
	}

	public int ChunksCur()
	{
		return numChunksCompleted;
	}

	public int RaysMax()
	{
		return numChunksTarget;
	}

	public float GetGenProgress()
	{
		if (numChunksTarget > 0)
			return (float)numChunksCompleted / (numChunksTarget + chunksBusy); // In-progress rays counted as unfinished // TODO: Is this correct?
		else
			return 0;
	}
}
