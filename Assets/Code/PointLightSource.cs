using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointLightSource : LightSource
{
	public PointLightSource(float brightness, float colorTemp, Vector3 pos) : base(brightness, colorTemp, pos) { }

	public PointLightSource(float brightness, float colorTemp) : base(brightness, colorTemp) { }

	public override List<Vector3Int> FindAffectedChunks()
	{
		// Remember and return old chunks
		oldAffectedChunks.Clear();

		foreach (Vector3Int chunk in affectedChunks)
		{
			oldAffectedChunks.Add(chunk);
		}

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

					// Only return changed chunks
					if (oldAffectedChunks.Contains(chunk))
						oldAffectedChunks.Remove(chunk);
				}
			}
		}

		return oldAffectedChunks;
	}

	protected override void OnDirty()
	{
		Chunk chunk = World.GetChunkFor(worldX, worldY, worldZ);

		if (chunk != null && !chunk.isProcessing && chunk.genStage > Chunk.GenStage.Meshed)
		{
			World.UpdateLight(this, true);
		}
	}

	public override float GetBrightnessAt(Chunk chunk, Vector3Int at, bool inWater)
	{
		return !inWater ?
			Mathf.Clamp01(brightness / Mathf.Max(1, Utils.DistanceSqr(worldX, worldY, worldZ, at.x, at.y, at.z))) : // Rapid decay, soft ambient
			Mathf.Clamp01(0.4f * brightness - 0.02f * Mathf.Sqrt(Utils.DistanceSqr(worldX, worldY, worldZ, at.x, at.y, at.z))); // Full decay faster, stays bright for longer
	}

	public override float GetColorTemperatureAt(Chunk chunk, float value, bool inWater)
	{
		return !inWater ?
			(colorTemp) :
			(Mathf.Lerp(0, colorTemp, 0.6f + value));
	}

	public override bool IsShadowed(Vector3Int blockPos)
	{
		float adj = 0.5f;

		Vector3 offset = new Vector3(worldX - blockPos.x, worldY - blockPos.y, worldZ - blockPos.z).normalized;
		bool occluded = World.GetBlockFor(
			(int)(blockPos.x + offset.x + adj),
			(int)(blockPos.y + offset.y + adj),
			(int)(blockPos.z + offset.z + adj)).opacity > 127;

		return occluded;
	}
}
