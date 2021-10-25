using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class LightSource : MonoBehaviour
{
	[HideInInspector]
	public int worldX, worldY, worldZ; // Coordinates in world space

	private int lastWorldX, lastWorldY, lastWorldZ; // Last coordinates in world space (to determine if dirty)

	public float brightness = 1; // From 0 to 1
	public float colorTemp = 0; // From -1 to 1

	public List<Chunk> affectedChunks = new List<Chunk>();
	private List<Chunk> oldAffectedChunks = new List<Chunk>();

	public bool dirty = true;

	public List<Chunk> FindAffectedChunks()
	{
		// Remember and return old chunks
		oldAffectedChunks.Clear();

		foreach (Chunk chunk in affectedChunks)
			oldAffectedChunks.Add(chunk);

		// Find new chunks in range
		affectedChunks.Clear();

		int mult = 8;
		int range = 1;
		for (int x = -range; x <= range; x++)
		{
			for (int y = -range; y <= range; y++)
			{
				for (int z = -range; z <= range; z++)
				{
					Chunk chunk = World.GetChunkFor(worldX + x * mult, worldY + y * mult, worldZ + z * mult);
					if (chunk && !affectedChunks.Contains(chunk))
					{
						affectedChunks.Add(chunk);

						chunk.lightsToHandle++;
					}
				}
			}
		}

		return oldAffectedChunks;
	}

	public void UpdatePosition()
	{
		worldX = Mathf.RoundToInt(transform.position.x);
		worldY = Mathf.RoundToInt(transform.position.y);
		worldZ = Mathf.RoundToInt(transform.position.z);

		if (worldX != lastWorldX || worldY != lastWorldY || worldZ != lastWorldZ)
			dirty = true;

		lastWorldX = worldX;
		lastWorldY = worldY;
		lastWorldZ = worldZ;
	}

	private void OnDrawGizmosSelected()
	{
		int mult = 8;
		int range = 1;

		Gizmos.color = Utils.colorDarkGrayBlue;
		Gizmos.DrawWireCube(transform.position, Vector3.one * mult * range * 2);
	}
}
