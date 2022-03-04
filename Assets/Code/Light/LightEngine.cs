using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Priority_Queue;
using System.ComponentModel;

[System.Serializable]
public class LightEngine
{
	public struct LightRay
	{
		public Vector3 source;
		public int stepSize;
	}

	public struct LightRayResult
	{
		public Vector3 source;
		public int stepSize;
		public bool success;
		public Queue<LightRayResultPoint> points;
	}

	public struct LightRayResultPoint
	{
		public Vector3 pos;
		public Color color;
		public bool airLight;
	}

	private readonly SimplePriorityQueue<LightRay> sourceQueue = new SimplePriorityQueue<LightRay>();
	private readonly SimplePriorityQueue<LightRay> retrySourceQueue = new SimplePriorityQueue<LightRay>();

	private Sun sun;

	[SerializeField]
	private int raysPerBatch = 40;
	private int raysBusy = 0;

	private int completedBlurryRays;
	private int targetBlurryRays;
	private bool blurryLightFinished;

	private int curProgress;
	private int maxProgress;
	private bool allLightFinished;

	public void Init(Sun sun)
	{
		this.sun = sun;
	}

	public void Begin()
	{
		//int step = WorldLightAtlas.Instance.directScale;
		int stepSize = 2;

		sourceQueue.Clear();
		retrySourceQueue.Clear();

		for (int x = Utils.ToInt(sun.sourcePoints.min.x); x < Utils.ToInt(sun.sourcePoints.max.x); x += stepSize)
		{
			for (int y = Utils.ToInt(sun.sourcePoints.min.y); y < Utils.ToInt(sun.sourcePoints.max.y); y += stepSize)
			{
				for (int z = Utils.ToInt(sun.sourcePoints.min.z); z < Utils.ToInt(sun.sourcePoints.max.z); z += stepSize)
				{
					Vector3Int pos = new Vector3Int(x, y - (stepSize / 2), z);
					sourceQueue.Enqueue(new LightRay() { source = pos, stepSize = stepSize }, Vector3.SqrMagnitude(pos - World.GetRelativeOrigin()));
				}
			}
		}

		completedBlurryRays = 0;
		targetBlurryRays = sourceQueue.Count;
		blurryLightFinished = false;

		curProgress = 0;
		maxProgress = sourceQueue.Count * (stepSize * stepSize * stepSize);
		allLightFinished = false;

		Debug.Log(maxProgress + " light rays to be cast");

		Iterate();
	}

	public void Iterate()
	{
		if (!Application.isPlaying)
			return;

		if (completedBlurryRays == targetBlurryRays && !blurryLightFinished)
		{
			blurryLightFinished = true;
			WorldLightAtlas.Instance.ApplyChanges();
		}

		if (curProgress == maxProgress && !allLightFinished)
		{
			allLightFinished = true;
			WorldLightAtlas.Instance.ApplyChanges();
		}

		if (raysBusy > 0)
			return;

		for (int i = 0; i < raysPerBatch; i++)
		{
			LightRay ray;

			// Still have new rays to send
			if (sourceQueue.Count > 0)
			{
				ray = sourceQueue.Dequeue();
			}
			// Retry previous rays
			else
			{
				// Retry a previous ray
				if (retrySourceQueue.Count > 0)
					ray = retrySourceQueue.Dequeue();
				else
					break;
			}

			AsyncLightRay(ray);
		}
	}

	public void AsyncLightRay(LightRay lightRay)
	{
		BkgThreadLightRay(this, System.EventArgs.Empty, lightRay);
	}

	private void BkgThreadLightRay(object sender, System.EventArgs e, LightRay lightRay)
	{
		raysBusy++;

		BackgroundWorker bw = new BackgroundWorker();

		// What to do in the background thread
		bw.DoWork += new DoWorkEventHandler(
		delegate (object o, DoWorkEventArgs args)
		{
			args.Result = SendLightRay(lightRay.source, lightRay.stepSize);
		});

		// What to do when worker completes its task
		bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
		delegate (object o, RunWorkerCompletedEventArgs args)
		{
			// Free up space for new rays
			raysBusy--;

			LightRayResult result = (LightRayResult)args.Result;

			// Ray was successful
			if (result.success)
			{
				completedBlurryRays++;

				bool sendSubRays = false;

				// Has results to apply
				if (result.points != null)
				{
					while (result.points.Count > 0)
					{
						LightRayResultPoint point = result.points.Dequeue();

						// Fill in surrounding pixel(s)
						Vector3 centerPos = new Vector3(
							point.pos.x + (result.stepSize - 1) / 2f,
							point.pos.y + (result.stepSize - 1) / 2f,
							point.pos.z + (result.stepSize - 1) / 2f
						);

						for (int x = 0; x < result.stepSize; x++)
						{
							for (int y = 0; y < result.stepSize; y++)
							{
								for (int z = 0; z < result.stepSize; z++)
								{
									Vector3 pos = new Vector3(
										point.pos.x + x,
										point.pos.y + y,
										point.pos.z + z
									);

									// Light is 100% accurate here: can safely write straight to lightmap
									if (point.airLight || result.stepSize == 1)
									{
										WorldLightAtlas.Instance.WriteToLightmap(new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z)), point.color, point.airLight);
									}
									// Half accuracy (1 check per 8 blocks): send another ray from this position to verify what happened
									else
									{
										float directionDot = Vector3.Dot((pos - centerPos).normalized, sun.GetDirection());

										sendSubRays = true;

										// Only send necessary rays
										if (directionDot < 0f)
										{
											retrySourceQueue.Enqueue(
												new LightRay() { stepSize = 1, source = pos },
												Vector3.SqrMagnitude(pos - World.GetRelativeOrigin())
											);
										}
										// Make up for missing rays
										else
											curProgress += 1;
									}
								} // z
							} // y
						} // x
					} // count > 0
				} // !null

				// Count appropriate amount towards total
				if (!sendSubRays)
				{
					curProgress += (result.stepSize * result.stepSize * result.stepSize);
				}
			}
			// Ray was unsuccessful, retry when possible
			else
			{
				retrySourceQueue.Enqueue(new LightRay() { source = result.source, stepSize = result.stepSize }, Vector3.SqrMagnitude(lightRay.source - World.GetRelativeOrigin()));
			}

			Iterate();
		});

		bw.RunWorkerAsync();
	}

	private LightRayResult SendLightRay(Vector3 source, int stepSize)
	{
		bool hd = stepSize == 1;

		float progress = 0;
		Vector3 cur = source;

		Vector3Int intCur = new Vector3Int(
			Mathf.RoundToInt(cur.x),
			Mathf.RoundToInt(cur.y),
			Mathf.RoundToInt(cur.z)
		);

		Queue<LightRayResultPoint> rayPoints = null;

		int currentStep = 0;
		while (currentStep < 1000)
		{
			currentStep++;

			// Get chunk (or it is out of the world)
			// TODO: Check world bounds here
			Chunk chunk = World.GetChunkFor(intCur);
			if (chunk == null)
			{
				Debug.DrawLine(source, cur, sun.lightColor, hd ? 2 : 0.5f);

				return new LightRayResult()
				{
					source = source,
					stepSize = stepSize,

					success = true,
					points = rayPoints
				};
			}

			// Chunk is not ready
			while (chunk.procStage < Chunk.ProcStage.Done)
			{
				Debug.DrawLine(source, World.GetRelativeOrigin(), Color.red, hd ? 20 : 5);

				return new LightRayResult()
				{
					source = source,
					stepSize = stepSize,

					success = false,
					points = rayPoints
				};
			}

			// Should block light?
			bool occupied = hd ? World.GetCorner(intCur.x, intCur.y, intCur.z) : World.GetBlurredCorner(intCur.x, intCur.y, intCur.z);

			// Remember this result
			if (rayPoints == null)
				rayPoints = new Queue<LightRayResultPoint>();
			// Only count result if not starting inside a corner
			//if (currentStep != 0)
			rayPoints.Enqueue(new LightRayResultPoint() {
				pos = cur,
				color = sun.lightColor,
				airLight = !occupied
			});

			// Stop after we hit something
			if (occupied)
			{
				break;
			}

			// Move cursor
			progress += stepSize * 0.5f;
			//progress += stepSize * 0.7071067f;

			cur = new Vector3(
				source.x + (progress * sun.GetDirection().x),
				source.y + (progress * sun.GetDirection().y),
				source.z + (progress * sun.GetDirection().z)
			);

			intCur = new Vector3Int(
				Mathf.RoundToInt(cur.x),
				Mathf.RoundToInt(cur.y),
				Mathf.RoundToInt(cur.z)
			);
		} // y

		Vector3 rand = SeedlessRandom.RandomPoint(0.1f);
		Debug.DrawLine(source, cur + rand, sun.lightColor, hd ? 2 : 0.5f);
		Debug.DrawLine(source, cur + rand, sun.lightColor / 2, hd ? 2 : 0.5f);

		return new LightRayResult()
		{
			source = source,
			stepSize = stepSize,

			success = true,
			points = rayPoints
		};
	}

	public bool IsBusy()
	{
		return raysBusy > 0;
	}

	public int RaysCur()
	{
		return curProgress;
	}

	public int RaysMax()
	{
		return maxProgress;
	}

	public float GetGenProgress()
	{
		/*if (blurryLightFinished)
			return 1;
		else */
		if (maxProgress > 0)
			return (float)curProgress / (maxProgress + raysBusy); // In-progress rays counted as unfinished
		else
			return 0;
	}
}
