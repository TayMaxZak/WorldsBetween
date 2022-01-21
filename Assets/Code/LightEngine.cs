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

	private readonly SimplePriorityQueue<Vector3Int> sourceQueues = new SimplePriorityQueue<Vector3Int>();

	private Sun sun;

	private Timer iterateTimer = new Timer(0.001f);

	public void Init(Sun sun)
	{
		this.sun = sun;

		sourceQueues.Clear();
		for (int x = Utils.ToInt(sun.sourcePoints.min.x); x < Utils.ToInt(sun.sourcePoints.max.x); x++)
		{
			for (int y = Utils.ToInt(sun.sourcePoints.min.y); y < Utils.ToInt(sun.sourcePoints.max.y); y++)
			{
				for (int z = Utils.ToInt(sun.sourcePoints.min.z); z < Utils.ToInt(sun.sourcePoints.max.z); z++)
				{
					Vector3Int pos = new Vector3Int(x, y, z);
					sourceQueues.Enqueue(pos, Vector3.SqrMagnitude(pos - World.GetRelativeOrigin()));
				}
			}
		}
		Debug.Log(sourceQueues.Count + " light rays to be cast");
	}

	public void Iterate(float deltaTime)
	{
		iterateTimer.Increment(deltaTime);

		if (!iterateTimer.Expired())
			return;

		iterateTimer.Reset();

		for (int i = 0; i < 10; i++)
		{
			if (sourceQueues.Count == 0)
				break;

			Vector3Int source = sourceQueues.Dequeue();

			AsyncLightRay(source);
		}
	}

	public void AsyncLightRay(Vector3Int source)
	{
		BkgThreadLightRay(this, System.EventArgs.Empty, source);
	}

	private void BkgThreadLightRay(object sender, System.EventArgs e, Vector3Int source)
	{
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
			LinkedList<LightRayResult> results = (LinkedList<LightRayResult>)args.Result;

			foreach (LightRayResult t in results)
			{
				WorldLightAtlas.Instance.WriteToLightmap(WorldLightAtlas.LightMapSpace.WorldSpace, t.coord, t.value ? sun.lightColor : Color.magenta, t.airLight);
			}
		});

		bw.RunWorkerAsync();
	}

	private LinkedList<LightRayResult> SendLightRay(Vector3Int source)
	{
		Vector3Int cur = source;

		LinkedList<LightRayResult> results = new LinkedList<LightRayResult>();

		int chunkSize = World.GetChunkSize();

		bool firstPass = true;
		while (cur.y > -300)
		{
			// Get chunk info
			Chunk chunk = World.GetChunkFor(cur);
			if (chunk == null || chunk.procStage < Chunk.ProcStage.Done)
				return results;

			ChunkBitArray cornerBit = chunk.GetCorners();

			if (cornerBit == null)
				return results;

			// Should block light?
			Vector3Int adjPos = cur - chunk.position;
			bool occupied = cornerBit.Get(adjPos.x, adjPos.y, adjPos.z);

			if (firstPass)
			{
				// Light starting inside corner
				if (occupied)
					return results;
			}
			firstPass = false;

			// Remember this result
			results.AddLast(new LightRayResult() { coord = cur, value = true, airLight = !occupied });

			if (occupied)
				break;

			cur.y -= 1;
		} // y

		Debug.DrawLine(source, cur, sun.lightColor, 1);
		return results;
	}
}
