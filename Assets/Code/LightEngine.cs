using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Priority_Queue;

[System.Serializable]
public class LightEngine
{
	public struct Tuple
	{
		public Vector3Int coord;
		public bool value;
	}

	private readonly SimplePriorityQueue<Vector3Int> sourceQueues = new SimplePriorityQueue<Vector3Int>();

	private Transform toUse;

	private Timer iterateTimer = new Timer(0.2f);

	public void Init(Transform toUse)
	{
		this.toUse = toUse;
	}

	public void Iterate(float deltaTime)
	{
		iterateTimer.Increment(deltaTime);

		if (!iterateTimer.Expired())
			return;

		iterateTimer.Reset();

		sourceQueues.Enqueue(new Vector3Int(Utils.IntVal(toUse.position.x + SeedlessRandom.NextFloatInRange(-16,16)), Utils.IntVal(toUse.position.y), Utils.IntVal(toUse.position.z + SeedlessRandom.NextFloatInRange(-16, 16))), 0.5f);

		if (sourceQueues.Count == 0)
			return;

		Vector3Int source = sourceQueues.Dequeue();

		LinkedList<Tuple> results = SendLightRay(source);

		foreach (Tuple t in results)
		{
			WorldLightAtlas.Instance.WriteToLightmap(WorldLightAtlas.LightMapSpace.WorldSpace, t.coord, t.value ? Color.yellow : Color.magenta);
		}
	}

	private LinkedList<Tuple> SendLightRay(Vector3Int source)
	{
		Vector3Int cur = source;

		LinkedList<Tuple> results = new LinkedList<Tuple>();

		int chunkSize = World.GetChunkSize();

		bool firstPass = true;
		while (cur.y > -300)
		{
			// Get chunk info
			Chunk chunk = World.GetChunkFor(cur);
			if (chunk == null)
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
			results.AddLast(new Tuple() { coord = cur, value = true });

			if (occupied)
				break;

			cur.y -= 1;
		} // y

		return results;
	}

	public void CalcMaskFor(ChunkBitArray cornerBit, ChunkBitArray shadowBit)
	{
		int chunkSize = World.GetChunkSize();

		// Cast light rays down
		for (int x = 0; x < chunkSize; x++)
		{
			for (int z = 0; z < chunkSize; z++)
			{
				int y = chunkSize - 1;

				// Light starting inside corner
				if (cornerBit.Get(x, y, z))
					continue;

				while (y > 0)
				{
					shadowBit.Set(true, x, y, z);

					// Should block light?
					bool occupied = cornerBit.Get(x, y, z);

					if (occupied)
						break;

					y--;
				} // y
			} // z
		} // x
	}
}
