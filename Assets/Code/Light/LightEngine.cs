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
	private readonly Dictionary<Vector3Int, BlockLight> lightIndex = new Dictionary<Vector3Int, BlockLight>();

	private float lengthPerRay = 24;

	[SerializeField]
	private int chunksPerBatch = 1;

	[SerializeField]
	private float rayProgressStep = 0.707f;

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
				lightIndex.Add(light.blockPos, light);

				Debug.DrawRay(light.blockPos + Vector3.one * 0.5f, Vector3.up, light.GetLightColor(1), 15);
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

		// Already busy?
		if (chunksBusy > 0)
			return;

		// Otherwise, start lighting more chunks
		for (int i = 0; i < chunksPerBatch; i++)
		{
			// Still have new chunks to light?
			if (chunkQueue.Count > 0)
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

		foreach (var light in lightIndex)
		{
			// Only nearby lights
			if (Utils.DistSquared(pos, light.Key) <= lengthPerRay * lengthPerRay)
			{
				float lightStrength = Mathf.Clamp01(1f - (1f / lengthPerRay) * Mathf.Sqrt(Utils.DistSquared(pos, light.Key)));
				lightStrength *= lightStrength;

				output += lightStrength * light.Value.GetLightColor(lightStrength);
				// TODO: Shadow rays
			}
		}

		return output;
	}

	//private LightRayResult SendLightRay(LightRay ray)
	//{
	//	Queue<LightRayResultPoint> rayPoints = null;

	//	float progress = 0;
	//	Vector3 cur = ray.source;

	//	Vector3Int blockCur = new Vector3Int(
	//		Mathf.FloorToInt(cur.x),
	//		Mathf.FloorToInt(cur.y),
	//		Mathf.FloorToInt(cur.z)
	//	);

	//	int curStep = 0;
	//	float maxStep = lengthPerRay;
	//	while (curStep < maxStep)
	//	{
	//		curStep++;

	//		// Check world bounds here
	//		if (!World.Contains(cur))
	//		{
	//			//if (SeedlessRandom.NextFloat() < intensityPerRay)
	//			//	Debug.DrawLine(ray.source, cur, ray.lightColor.colorClose, 1f);

	//			return new LightRayResult()
	//			{
	//				source = ray.source,
	//				stepSize = 1,

	//				success = true,
	//				points = rayPoints
	//			};
	//		}
	//		Chunk chunk = World.GetChunk(blockCur);
	//		if (chunk == null)
	//		{
	//			//if (SeedlessRandom.NextFloat() < intensityPerRay)
	//			//	Debug.DrawLine(ray.source, cur, ray.lightColor.colorClose, 1f);

	//			return new LightRayResult()
	//			{
	//				source = ray.source,
	//				stepSize = 1,

	//				success = true,
	//				points = rayPoints
	//			};
	//		}

	//		// Chunk is not ready
	//		if (chunk.buildStage < Chunk.BuildStage.Done)
	//		{
	//			Debug.DrawLine(ray.source, World.GetRelativeOrigin(), Color.red, 5);

	//			return new LightRayResult()
	//			{
	//				source = ray.source,
	//				stepSize = 1,

	//				success = false,
	//				points = rayPoints
	//			};
	//		}

	//		// Should block light? Check if inside opaque block if at block resolution
	//		bool occupied = World.GetBlock(blockCur.x, blockCur.y, blockCur.z).IsOpaque() && curStep > 1;
	//		// Stop after we hit something
	//		if (occupied)
	//		{
	//			break;
	//		}

	//		// Should reflect light? Check if inside water
	//		bool reflect = World.GetWaterHeight() >= cur.y;
	//		if (reflect && !ray.hasBounced)
	//		{
	//			if (SeedlessRandom.NextFloat() < intensityPerRay / 10f)
	//				Debug.DrawLine(ray.source, cur, ray.colorFalloff.colorClose, 5f);

	//			ray.hasBounced = true;

	//			ray.dir.y *= -1;
	//			//ray.dir = (ray.dir + SeedlessRandom.RandomPoint().normalized).normalized;
	//			ray.colorFalloff.colorClose *= waterBounceTint;
	//			ray.colorFalloff.colorFar *= waterBounceTint;
	//		}

	//		// Remember this result
	//		if (rayPoints == null)
	//			rayPoints = new Queue<LightRayResultPoint>();
	//		// Only count result if not starting inside a corner
	//		//if (curStep != 0)
	//		{
	//			float falloff = (1 - (curStep / maxStep) * (curStep / maxStep));
	//			rayPoints.Enqueue(new LightRayResultPoint()
	//			{
	//				pos = cur,
	//				color = intensityPerRay * falloff * Color.Lerp(ray.colorFalloff.colorClose, ray.colorFalloff.colorFar, 1 - falloff),
	//				airLight = !occupied
	//			});
	//		}

	//		// Move cursor
	//		//progress += 1;
	//		//progress += 0.5f;
	//		//progress += 0.7071067f;
	//		progress += progressStep;

	//		cur = new Vector3(
	//			ray.source.x + (progress * ray.dir.x),
	//			ray.source.y + (progress * ray.dir.y),
	//			ray.source.z + (progress * ray.dir.z)
	//		);

	//		blockCur = new Vector3Int(
	//			Mathf.RoundToInt(cur.x),
	//			Mathf.RoundToInt(cur.y),
	//			Mathf.RoundToInt(cur.z)
	//		);
	//	} // y

	//	if (SeedlessRandom.NextFloat() < intensityPerRay / 10f)
	//		Debug.DrawLine(ray.source + Vector3.one * 0.5f, cur + Vector3.one * 0.5f, ray.colorFalloff.colorClose, 5f);

	//	return new LightRayResult()
	//	{
	//		source = ray.source,
	//		stepSize = 1,

	//		success = true,
	//		points = rayPoints
	//	};
	//}

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
