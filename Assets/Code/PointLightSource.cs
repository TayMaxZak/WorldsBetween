using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointLightSource : LightSource
{
	public PointLightSource(float brightness, float colorTemp, Vector3 pos) : base(brightness, colorTemp, pos)
	{
		this.brightness = brightness;
		this.colorTemp = colorTemp;

		UpdatePosition(pos);
	}

	public override List<Vector3Int> FindAffectedChunks()
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

	protected override void OnDirty()
	{
		
	}

	public override float GetBrightnessAt(Vector3Int at, bool inWater)
	{
		return !inWater ?
			Mathf.Clamp01(brightness / Mathf.Max(1, Utils.DistanceSqr(worldX, worldY, worldZ, at.x, at.y, at.z))) : // Rapid decay, soft ambient
			Mathf.Clamp01(0.4f * brightness - 0.02f * Mathf.Sqrt(Utils.DistanceSqr(worldX, worldY, worldZ, at.x, at.y, at.z)) // Full decay faster, stays bright for longer
		);
	}

	public override float GetColorTemperatureAt(float value, bool inWater)
	{
		return !inWater ?
			(colorTemp) :
			(Mathf.Lerp(0, colorTemp, 0.6f + value)
		);
	}
}
