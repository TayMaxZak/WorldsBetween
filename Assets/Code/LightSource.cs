using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class LightSource : MonoBehaviour
{
	[HideInInspector]
	public int worldX, worldY, worldZ; // Coordinates in world space

	public float brightness = 1; // From 0 to 1
	public float colorTemp = 0; // From -1 to 1

	public List<Chunk> affectedChunks;

	public bool dirty = true;

	public void UpdatePos()
	{
		worldX = Mathf.RoundToInt(transform.position.x);
		worldY = Mathf.RoundToInt(transform.position.y);
		worldZ = Mathf.RoundToInt(transform.position.z);

		affectedChunks.Clear();

		int mult = 8;
		for (int x = -1; x < 2; x++)
		{
			for (int y = -1; y < 2; y++)
			{
				for (int z = -1; z < 2; z++)
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
	}
}
