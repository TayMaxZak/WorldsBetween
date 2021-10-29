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

	public List<Vector3Int> affectedChunks = new List<Vector3Int>();
	private List<Vector3Int> oldAffectedChunks = new List<Vector3Int>();

	public bool dirty = true;

	private void Awake()
	{
		UpdatePosition();
		World.RegisterLight(this);
	}

	public List<Vector3Int> FindAffectedChunks()
	{
		// Remember and return old chunks
		oldAffectedChunks.Clear();

		foreach (Vector3Int chunk in affectedChunks)
			oldAffectedChunks.Add(chunk);

		affectedChunks.Clear();

		// Find new chunks in range
		float maxDistance = Mathf.Sqrt(brightness * 250); // i.e., brightness / distance^2 = 0.004

		int chunkSize = World.GetChunkSize();

		int range = Mathf.CeilToInt((maxDistance * 0.5f) / chunkSize);

		for (int x = -range; x <= range; x++)
		{
			for (int y = -range; y <= range; y++)
			{
				for (int z = -range; z <= range; z++)
				{
					Vector3Int chunk = new Vector3Int(
						Mathf.FloorToInt((x * chunkSize + worldX) / (float)chunkSize) * chunkSize,
						Mathf.FloorToInt((y * chunkSize + worldY) / (float)chunkSize) * chunkSize,
						Mathf.FloorToInt((z * chunkSize + worldZ) / (float)chunkSize) * chunkSize
					);

					affectedChunks.Add(chunk);
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

	public float GetBrightnessAt(Vector3Int at)
	{
		return brightness / Mathf.Max(1, Utils.DistanceSqr(worldX, worldY, worldZ, at.x, at.y, at.z));
	}

	private void OnDrawGizmosSelected()
	{
		float maxDistance = Mathf.Sqrt(brightness * 250); // i.e., brightness / distance^2 = 0.004

		Gizmos.color = Utils.colorYellow;
		Gizmos.DrawWireSphere(transform.position, maxDistance);

		Gizmos.color = Utils.colorOrange;
		Gizmos.DrawWireSphere(transform.position, maxDistance * 0.5f);

		int chunkSize = 8;

		int range = Mathf.CeilToInt((maxDistance * 0.5f) / chunkSize);

		Gizmos.color = Utils.colorDarkGrayBlue;
		Gizmos.DrawWireCube(transform.position, Vector3.one * chunkSize * range * 2);
	}
}
