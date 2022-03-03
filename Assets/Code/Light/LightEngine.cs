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
		public Vector3Int source;
		public int stepSize;
	}

	public struct LightRayResult
	{
		public Vector3Int source;
		public int stepSize;
		public bool success;
		public Queue<LightRayResultPoint> points;
	}

	public struct LightRayResultPoint
	{
		public Vector3Int pos;
		public Color color;
		public bool airLight;
	}

	private readonly SimplePriorityQueue<LightRay> sourceQueue = new SimplePriorityQueue<LightRay>();
	private readonly SimplePriorityQueue<LightRay> retrySourceQueue = new SimplePriorityQueue<LightRay>();

	private Sun sun;

	[SerializeField]
	private int raysPerBatch = 40;
	private int raysBusy = 0;

	private int doneBlurryRays;
	private int targetBlurryRays;
	private bool blurryLightDone = false;

	private int curProgress;
	private int maxProgress;
	private bool allLightDone = false;

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

		doneBlurryRays = 0;
		targetBlurryRays = sourceQueue.Count;

		curProgress = 0;
		maxProgress = sourceQueue.Count * (stepSize * stepSize * stepSize);

		Debug.Log(maxProgress + " light rays to be cast");

		Iterate();
	}

	public void Iterate()
	{
		if (!Application.isPlaying)
			return;

		if (doneBlurryRays == targetBlurryRays && !blurryLightDone)
		{
			blurryLightDone = true;
			WorldLightAtlas.Instance.ApplyChanges();
		}

		if (curProgress == maxProgress && !allLightDone)
		{
			allLightDone = true;
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
				doneBlurryRays++;

				bool sendSubRays = false;

				// Has results to apply
				if (result.points != null)
				{
					while (result.points.Count > 0)
					{
						LightRayResultPoint point = result.points.Dequeue();

						// Fill in surrounding pixel(s)
						for (int x = 0; x < result.stepSize; x++)
						{
							for (int y = 0; y < result.stepSize; y++)
							{
								for (int z = 0; z < result.stepSize; z++)
								{
									// Light is 100% accurate here, can safely right straight to lightmap
									if (point.airLight || result.stepSize == 1)
									{
										Vector3Int pos = new Vector3Int(point.pos.x + x, point.pos.y + y, point.pos.z + z);

										WorldLightAtlas.Instance.WriteToLightmap(new Vector3Int(point.pos.x + x, point.pos.y + y, point.pos.z + z), point.color, point.airLight);
									}
									// Half accuracy (1 check per 8 blocks), send another ray from this position to verify what happened
									else
									{
										Vector3Int pos = new Vector3Int(point.pos.x + x, point.pos.y + y + 1, point.pos.z + z);

										sendSubRays = true;

										retrySourceQueue.Enqueue(
											new LightRay() { stepSize = 1, source = pos },
											Vector3.SqrMagnitude(pos - World.GetRelativeOrigin())
										);
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

	private LightRayResult SendLightRay(Vector3Int source, int stepSize)
	{
		bool hd = stepSize == 1;

		Vector3Int cur = source;

		Queue<LightRayResultPoint> rayPoints = null;

		int currentStep = 0;
		while (currentStep < 1000)
		{
			currentStep++;

			// Get chunk (or it is out of the world)
			// TODO: Check world bounds here
			Chunk chunk = World.GetChunkFor(cur);
			if (chunk == null)
			{
				Debug.DrawLine(source, cur, Color.magenta, hd ? 2 : 0.5f);

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
				Debug.DrawLine(source, World.GetRelativeOrigin(), Color.magenta, hd ? 20 : 5);

				return new LightRayResult()
				{
					source = source,
					stepSize = stepSize,

					success = false,
					points = rayPoints
				};
			}

			// Should block light?
			bool occupied = hd ? World.GetCorner(cur.x, cur.y, cur.z) : World.GetBlurredCorner(cur.x, cur.y, cur.z);

			// Remember this result
			if (rayPoints == null)
				rayPoints = new Queue<LightRayResultPoint>();
			// Only count result if not starting inside a corner
			//if (currentStep != 0)
				rayPoints.Enqueue(new LightRayResultPoint() { pos = cur, color = sun.lightColor, airLight = !occupied });

			// Stop after we hit something
			if (occupied)
			{
				Debug.DrawLine(cur, cur - Vector3.up * 200, Color.black, hd ? 2 : 0.5f);
				break;
			}

			cur.y -= stepSize;
		} // y

		Debug.DrawLine(source, cur, sun.lightColor, hd ? 2 : 0.5f);

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
		if (blurryLightDone)
			return 1;
		else if (maxProgress > 0)
			return (float)curProgress / (maxProgress + raysBusy); // In-progress rays counted as unfinished
		else
			return 0;
	}
}
