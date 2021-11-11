using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DirectionalLightSource : LightSource
{
	private Vector3 direction;

	public DirectionalLightSource(float brightness, float colorTemp, Vector3 direction) : base(brightness, colorTemp, direction)
	{
		this.direction = direction;
	}

	public DirectionalLightSource(float brightness, float colorTemp) : base(brightness, colorTemp)
	{
		direction = Vector3.down;
	}

	public DirectionalLightSource() : base() { }

	public override List<Vector3Int> FindAffectedChunks()
	{
		var keys = World.GetLitChunks();

		affectedChunks.Clear();
		foreach (Vector3Int key in keys)
			affectedChunks.Add(key);

		return null;
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
		return Mathf.Clamp01(brightness);
	}

	public override float GetAttenAt(Chunk chunk, float distance, bool inWater)
	{
		return 0;
	}

	public override float GetColorOpacityAt(Chunk chunk, float distance, bool inWater)
	{
		return Mathf.Clamp01(brightness);
	}

	public override bool IsShadowed(Vector3Int blockPos)
	{
		float adj = 0.5f;

		for (int i = 1; i <= 6; i++)
		{
			bool occluded = World.GetBlockFor(
				(int)(blockPos.x + -direction.x * i + adj),
				(int)(blockPos.y + -direction.y * i + adj),
				(int)(blockPos.z + -direction.z * i + adj)
			).opacity > 127;

			if (occluded)
				return true;
		}

		return false;
	}
}
