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

	private Sun sun;

	[SerializeField]
	private int raysPerStepHi = 300;
	[SerializeField]
	private int raysPerStepLo = 30;
	private int raysBusy = 0;

	public void Init(Sun sun)
	{
		this.sun = sun;
	}

	public void Begin()
	{
		int step = WorldLightAtlas.Instance.directScale;

		sourceQueue.Clear();
		for (int x = Utils.ToInt(sun.sourcePoints.min.x); x < Utils.ToInt(sun.sourcePoints.max.x); x += step)
		{
			for (int y = Utils.ToInt(sun.sourcePoints.min.y); y < Utils.ToInt(sun.sourcePoints.max.y); y += step)
			{
				for (int z = Utils.ToInt(sun.sourcePoints.min.z); z < Utils.ToInt(sun.sourcePoints.max.z); z += step)
				{
					Vector3Int pos = new Vector3Int(x, y, z);
					sourceQueue.Enqueue(pos, Vector3.SqrMagnitude(pos - World.GetRelativeOrigin()));
				}
			}
		}
		Debug.Log(sourceQueue.Count + " light rays to be cast");

		//while (World.Generator.IsGenerating())
		//	await Task.Delay(10);

		Iterate();
	}

	public void Iterate()
	{
		//iterateTimer.Increment(deltaTime);

		//if (!iterateTimer.Expired())
		//	return;

		//iterateTimer.Reset();

		if (!Application.isPlaying)
			return;

		if (raysBusy > 0)
			return;

		for (int i = 0; i < (World.WorldBuilder.IsGenerating() ? raysPerStepHi : raysPerStepLo); i++)
		{
			// Done early
			if (sourceQueue.Count == 0)
				break;

			Vector3Int source = sourceQueue.Dequeue();

			AsyncLightRay(source);
		}

		// Finished, apply changes
		if (sourceQueue.Count == 0)
		{
			WorldLightAtlas.Instance.ApplyChanges();
		}
	}

	public void AsyncLightRay(Vector3Int source)
	{
		raysBusy++;
		BkgThreadLightRay(this, System.EventArgs.Empty, source);
	}

	private void BkgThreadLightRay(object sender, System.EventArgs e, Vector3Int source)
	{
		BackgroundWorker bw = new BackgroundWorker();

		// What to do in the background thread
		bw.DoWork += new DoWorkEventHandler(
		delegate (object o, DoWorkEventArgs args)
		{
			SendLightRay(source);
		});

		// What to do when worker completes its task
		bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
		delegate (object o, RunWorkerCompletedEventArgs args)
		{
			raysBusy--;

			Iterate();
		});

		bw.RunWorkerAsync();
	}

	private async void SendLightRay(Vector3Int source)
	{
		Vector3Int cur = source;

		bool firstPass = true;
		int steps = 0;
		while (steps < 1000)
		{
			steps++;

			// Get chunk (or wait its ready)
			Chunk chunk = World.GetChunkFor(cur);
			if (chunk == null)
				return;

			while (chunk.procStage < Chunk.ProcStage.Done)
				await Task.Delay(10);

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
					return;
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
		return;
	}

	public bool IsBusy()
	{
		return raysBusy > 0;
	}
}
