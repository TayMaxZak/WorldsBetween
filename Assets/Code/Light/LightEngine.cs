﻿using System.Collections;
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

	[SerializeField]
	private float progressStep = 1f;
	[SerializeField]
	private float splitPixelCutoff = 0.209f;
	[SerializeField]
	private float fixSplitOffset = 0.51f;

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
		int step = 2;
		int extent = World.GetWorldSize() / 2;


		sourceQueue.Clear();
		retrySourceQueue.Clear();

		for (float x = -extent; x <= extent; x += step)
		{
			for (float y = -extent; y <= extent; y += step)
			{
				for (float z = -extent; z <= extent; z += step)
				{
					if (Mathf.Abs(x) != extent && Mathf.Abs(y) != extent && Mathf.Abs(z) != extent)
						continue;

					Vector3 pos = new Vector3(x, y, z) + sun.GetDirection();

					if (Vector3.Dot(pos, sun.GetDirection()) >= 0)
						continue;

					sourceQueue.Enqueue(new LightRay() { source = pos, stepSize = step }, Vector3.SqrMagnitude(pos - World.GetRelativeOrigin()));
				}
			}
		}

		completedBlurryRays = 0;
		targetBlurryRays = sourceQueue.Count;
		blurryLightFinished = false;

		curProgress = 0;
		maxProgress = sourceQueue.Count * (step * step);
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
			//args.Result = new LightRayResult[] {
			//	SendLightRay(lightRay.source),
			//	SendLightRay(lightRay.source + sun.GetRight()),
			//	SendLightRay(lightRay.source + sun.GetUp()),
			//	SendLightRay(lightRay.source + sun.GetRight() + sun.GetUp())
			//};
			args.Result = new LightRayResult[] {
				SendLightRay(lightRay.source),
				SendLightRay(lightRay.source + Vector3.right),
				SendLightRay(lightRay.source + Vector3.forward),
				SendLightRay(lightRay.source + Vector3.right + Vector3.forward)
			};
		});

		// What to do when worker completes its task
		bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
		delegate (object o, RunWorkerCompletedEventArgs args)
		{
			// Free up space for new rays
			raysBusy--;

			RespondToRay(((LightRayResult[])args.Result)[0]);
			RespondToRay(((LightRayResult[])args.Result)[1]);
			RespondToRay(((LightRayResult[])args.Result)[2]);
			RespondToRay(((LightRayResult[])args.Result)[3]);
		});

		bw.RunWorkerAsync();
	}

	private void RespondToRay(LightRayResult result)
	{
		// Ray was successful
		if (result.success)
		{
			if (result.stepSize > 1)
				completedBlurryRays++;

			bool sendSubRays = false;

			// Has results to apply
			if (result.points != null)
			{
				while (result.points.Count > 0)
				{
					LightRayResultPoint point = result.points.Dequeue();

					// Fill in surrounding pixel(s)
					//Vector3 centerPos = point.pos;

					Vector3 offsetPos = point.pos;

					//bool onEdge = Mathf.Abs(offsetPos.x - Mathf.RoundToInt(offsetPos.x)) >= splitPixelCutoff || Mathf.Abs(offsetPos.y - Mathf.RoundToInt(offsetPos.y)) >= splitPixelCutoff || Mathf.Abs(offsetPos.z - Mathf.RoundToInt(offsetPos.z)) >= splitPixelCutoff;
					
					// Enough accuracy to write to a single pixel
					WorldLightAtlas.Instance.WriteToLightmap(new Vector3Int(Mathf.RoundToInt(offsetPos.x), Mathf.RoundToInt(offsetPos.y), Mathf.RoundToInt(offsetPos.z)), point.color, point.airLight);

					//// Possibly missed some pixels because it landed on an edge
					//else
					//{
					//	WorldLightAtlas.Instance.WriteToLightmap(new Vector3Int(Mathf.RoundToInt(offsetPos.x + fixSplitOffset), Mathf.RoundToInt(offsetPos.y + fixSplitOffset), Mathf.RoundToInt(offsetPos.z + fixSplitOffset)), point.color, point.airLight);
					//	WorldLightAtlas.Instance.WriteToLightmap(new Vector3Int(Mathf.RoundToInt(offsetPos.x + fixSplitOffset), Mathf.RoundToInt(offsetPos.y + fixSplitOffset), Mathf.RoundToInt(offsetPos.z - fixSplitOffset)), point.color, point.airLight);

					//	WorldLightAtlas.Instance.WriteToLightmap(new Vector3Int(Mathf.RoundToInt(offsetPos.x + fixSplitOffset), Mathf.RoundToInt(offsetPos.y - fixSplitOffset), Mathf.RoundToInt(offsetPos.z + fixSplitOffset)), point.color, point.airLight);
					//	WorldLightAtlas.Instance.WriteToLightmap(new Vector3Int(Mathf.RoundToInt(offsetPos.x + fixSplitOffset), Mathf.RoundToInt(offsetPos.y - fixSplitOffset), Mathf.RoundToInt(offsetPos.z - fixSplitOffset)), point.color, point.airLight);

					//	WorldLightAtlas.Instance.WriteToLightmap(new Vector3Int(Mathf.RoundToInt(offsetPos.x - fixSplitOffset), Mathf.RoundToInt(offsetPos.y + fixSplitOffset), Mathf.RoundToInt(offsetPos.z + fixSplitOffset)), point.color, point.airLight);
					//	WorldLightAtlas.Instance.WriteToLightmap(new Vector3Int(Mathf.RoundToInt(offsetPos.x - fixSplitOffset), Mathf.RoundToInt(offsetPos.y + fixSplitOffset), Mathf.RoundToInt(offsetPos.z - fixSplitOffset)), point.color, point.airLight);

					//	WorldLightAtlas.Instance.WriteToLightmap(new Vector3Int(Mathf.RoundToInt(offsetPos.x - fixSplitOffset), Mathf.RoundToInt(offsetPos.y - fixSplitOffset), Mathf.RoundToInt(offsetPos.z + fixSplitOffset)), point.color, point.airLight);
					//	WorldLightAtlas.Instance.WriteToLightmap(new Vector3Int(Mathf.RoundToInt(offsetPos.x - fixSplitOffset), Mathf.RoundToInt(offsetPos.y - fixSplitOffset), Mathf.RoundToInt(offsetPos.z - fixSplitOffset)), point.color, point.airLight);
					//}
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
			retrySourceQueue.Enqueue(
				new LightRay() { source = result.source, stepSize = result.stepSize },
				Vector3.SqrMagnitude(result.source - World.GetRelativeOrigin())
			);
		}

		Iterate();
	}

	private LightRayResult SendLightRay(Vector3 source)
	{
		Queue<LightRayResultPoint> rayPoints = null;

		float progress = 0;
		Vector3 cur = source;

		Vector3Int blockCur = new Vector3Int(
			Mathf.FloorToInt(cur.x),
			Mathf.FloorToInt(cur.y),
			Mathf.FloorToInt(cur.z)
		);

		int currentStep = 0;
		while (currentStep < 1000)
		{
			currentStep++;

			// Check world bounds here
			if (!World.Contains(cur))
			{
				Debug.DrawLine(source, cur, sun.lightColor, 0.5f);

				return new LightRayResult()
				{
					source = source,
					stepSize = 1,

					success = true,
					points = rayPoints
				};
			}
			Chunk chunk = World.GetChunkFor(blockCur);

			// Chunk is not ready
			while (chunk.procStage < Chunk.ProcStage.Done)
			{
				Debug.DrawLine(source, World.GetRelativeOrigin(), Color.red, 5);

				return new LightRayResult()
				{
					source = source,
					stepSize = 1,

					success = false,
					points = rayPoints
				};
			}

			// Should block light? Check if inside opaque block if at block resolution
			bool occupied = !World.GetBlockFor(blockCur.x, blockCur.y, blockCur.z).IsAir();

			// Remember this result
			if (rayPoints == null)
				rayPoints = new Queue<LightRayResultPoint>();
			// Only count result if not starting inside a corner
			if (currentStep != 0)
			{
				rayPoints.Enqueue(new LightRayResultPoint()
				{
					pos = cur,
					color = sun.lightColor,
					airLight = !occupied
				});
			}

			// Stop after we hit something
			if (occupied)
			{
				break;
			}

			// Move cursor
			//progress += 1;
			//progress += 0.5f;
			//progress += 0.7071067f;
			progress += progressStep;

			cur = new Vector3(
				source.x + (progress * sun.GetDirection().x),
				source.y + (progress * sun.GetDirection().y),
				source.z + (progress * sun.GetDirection().z)
			);

			blockCur = new Vector3Int(
				Mathf.RoundToInt(cur.x),
				Mathf.RoundToInt(cur.y),
				Mathf.RoundToInt(cur.z)
			);
		} // y

		Vector3 rand = SeedlessRandom.RandomPoint(0.1f);
		Debug.DrawLine(source, cur + rand, sun.lightColor, 0.5f);

		return new LightRayResult()
		{
			source = source,
			stepSize = 1,

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
