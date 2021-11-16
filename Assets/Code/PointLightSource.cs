using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointLightSource : LightSource
{
	private float falloffFactor = 16;
	private float exponent = 4;

	public PointLightSource(float brightness, float colorTemp, Vector3 pos) : base(brightness, colorTemp, pos) { }

	public PointLightSource(float brightness, float colorTemp) : base(brightness, colorTemp) { }

	public PointLightSource() : base() { }

	public override List<Vector3Int> FindAffectedChunkCoords()
	{
		// Remember and return old chunks
		oldAffectedChunks.Clear();

		foreach (Vector3Int chunk in affectedChunks)
		{
			oldAffectedChunks.Add(chunk);
		}

		affectedChunks.Clear();

		// Find new chunks in range
		float maxDistance = falloffFactor * brightness * 0.6f;

		int chunkSize = World.GetChunkSize();

		int range = Mathf.CeilToInt(maxDistance / chunkSize);

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

	public override float GetBrightnessAt(Chunk chunk, float distance, bool inWater)
	{
		float falloff = 1f - distance * (1f / (falloffFactor * brightness));
		falloff = Mathf.Clamp01(falloff);

		for (int i = 1; i < exponent; i++)
			falloff *= falloff;

		return falloff;
	}

	public override float GetShadowBrightnessAt(Chunk chunk, float distance, bool inWater)
	{
		float falloff = 1f - distance * (1f / (falloffFactor * brightness));
		falloff = Mathf.Clamp01(falloff);

		for (int i = 1; i < exponent; i++)
			falloff *= falloff;

		return falloff;
	}

	public override float GetColorOpacityAt(Chunk chunk, float distance, bool inWater)
	{
		distance = Mathf.Max(0, distance - 1);

		float falloff = 1f - distance * (1f / (falloffFactor * brightness));
		falloff = Mathf.Clamp01(falloff);

		for (int i = 1; i < exponent; i++)
			falloff *= falloff;

		return falloff;
	}

	public override bool IsShadowed(Vector3Int blockPos)
	{
		float adj = 0.5f;

		Vector3 offset = new Vector3(worldX - blockPos.x, worldY - blockPos.y, worldZ - blockPos.z).normalized;
		for (int i = 1; i <= 3; i++)
		{
			bool occluded = World.GetBlockFor(
				(int)(blockPos.x + offset.x * i + adj),
				(int)(blockPos.y + offset.y * i + adj),
				(int)(blockPos.z + offset.z * i + adj)
			).opacity > 127;

			if (occluded)
				return true;
		}

		return false;
	}
}
