using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Priority_Queue;
using System.ComponentModel;

[System.Serializable]
public class LightEngine
{
	public struct LightRayResult
	{
		public Vector3Int coord;
		public bool value;
		public bool airLight;
	}

	private readonly SimplePriorityQueue<Vector3Int> sourceQueue = new SimplePriorityQueue<Vector3Int>();
	private readonly SimplePriorityQueue<Vector3Int> retrySourceQueue = new SimplePriorityQueue<Vector3Int>();

	private Sun sun;

	[SerializeField]
	private int raysPerBatch = 40;
	private int raysBusy = 0;

	private int raysDone;
	private int raysMax;

	public void Init(Sun sun)
	{
		this.sun = sun;
	}

	public void Begin()
	{
		int step = WorldLightAtlas.Instance.directScale;

		sourceQueue.Clear();
		retrySourceQueue.Clear();

		for (int x = Utils.ToInt(sun.sourcePoints.min.x) + (step / 2); x < Utils.ToInt(sun.sourcePoints.max.x); x += step)
		{
			for (int y = Utils.ToInt(sun.sourcePoints.min.y); y < Utils.ToInt(sun.sourcePoints.max.y); y += step)
			{
				for (int z = Utils.ToInt(sun.sourcePoints.min.z) + (step / 2); z < Utils.ToInt(sun.sourcePoints.max.z); z += step)
				{
					Vector3Int pos = new Vector3Int(x, y - (step / 2), z);
					sourceQueue.Enqueue(pos, Vector3.SqrMagnitude(pos - World.GetRelativeOrigin()));
				}
			}
		}

		raysDone = 0;
		raysMax = sourceQueue.Count;
		Debug.Log(raysMax + " light rays to be cast");

		Iterate();
	}

	public void Iterate()
	{
		if (!Application.isPlaying)
			return;

		if (raysBusy > 0)
			return;

		for (int i = 0; i < raysPerBatch; i++)
		{
			Vector3Int source;

			// Still have new rays to send
			if (sourceQueue.Count > 0)
			{
				source = sourceQueue.Dequeue();
			}
			// Retry previous rays
			else
			{
				// Retry a previous ray
				if (retrySourceQueue.Count > 0)
					source = retrySourceQueue.Dequeue();
				else
					break;
			}

			AsyncLightRay(source);
		}
	}

	public void AsyncLightRay(Vector3Int source)
	{
		BkgThreadLightRay(this, System.EventArgs.Empty, source);
	}

	private void BkgThreadLightRay(object sender, System.EventArgs e, Vector3Int source)
	{
		raysBusy++;

		BackgroundWorker bw = new BackgroundWorker();

		// What to do in the background thread
		bw.DoWork += new DoWorkEventHandler(
		delegate (object o, DoWorkEventArgs args)
		{
			args.Result = SendLightRay(source);
		});

		// What to do when worker completes its task
		bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
		delegate (object o, RunWorkerCompletedEventArgs args)
		{
			// Free up space for new rays
			raysBusy--;

			// Ray was successful
			if (((RayResult)args.Result).success)
			{
				raysDone++;
			}
			// Ray was unsuccessful, retry when possible
			else
			{
				retrySourceQueue.Enqueue(((RayResult)args.Result).source, Vector3.SqrMagnitude(source - World.GetRelativeOrigin()));
			}

			Iterate();
		});

		bw.RunWorkerAsync();
	}

	public struct RayResult
	{
		public bool success;
		public Vector3Int source;
	}

	private RayResult SendLightRay(Vector3Int source)
	{
		Vector3Int cur = source;

		bool firstPass = true;
		int steps = 0;
		while (steps < 1000)
		{
			steps++;

			// Get chunk (or it is out of the world)
			// TODO: Check world bounds here
			Chunk chunk = World.GetChunkFor(cur);
			if (chunk == null)
			{
				Debug.DrawLine(source, cur + Vector3.up * 100, Color.magenta, 1);

				return new RayResult() { success = true, source = source };
			}

			while (chunk.procStage < Chunk.ProcStage.Done)
				return new RayResult() { success = false, source = source };

			ChunkBitArray cornerBit = chunk.GetCorners();

			// Should block light?
			Vector3Int adjPos = cur - chunk.position;
			bool occupied = cornerBit.Get(adjPos.x, adjPos.y, adjPos.z);

			if (firstPass)
			{
				// Light starting inside corner
				if (occupied)
				{
					Debug.DrawLine(cur, cur - Vector3.up * 200, Color.black, 1);

					return new RayResult() { success = true, source = source };
				}
				firstPass = false;
			}

			// Remember this result
			WorldLightAtlas.Instance.WriteToLightmap(cur, sun.lightColor, !occupied);

			if (occupied)
			{
				Debug.DrawLine(cur, cur - Vector3.up * 200, Color.black, 1);
				break;
			}

			cur.y -= 1;
		} // y

		Debug.DrawLine(source, cur, sun.lightColor, 1);

		return new RayResult() { success = true, source = source };
	}

	public bool IsBusy()
	{
		return raysBusy > 0;
	}

	public int RaysCur()
	{
		return raysDone;
	}

	public int RaysMax()
	{
		return raysMax;
	}

	public float GetGenProgress()
	{
		if (raysMax > 0)
			return (float)raysDone / (raysMax + raysBusy); // In-progress rays counted as unfinished
		else
			return 0;
	}
}
