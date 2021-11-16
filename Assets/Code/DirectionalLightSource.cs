﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DirectionalLightSource : LightSource
{
	private Vector3 direction;

	private float waterFalloffFactor = 64;
	private float waterExponent = 4;

	public DirectionalLightSource(float brightness, float colorTemp, Vector3 direction) : base(brightness, colorTemp, direction)
	{
		this.direction = direction;
	}

	public DirectionalLightSource(float brightness, float colorTemp) : base(brightness, colorTemp)
	{
		direction = Vector3.down;
	}

	public DirectionalLightSource() : base() { }

	public override List<Vector3Int> FindAffectedChunkCoords()
	{
		var keys = World.GetLitChunkCoords();

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

	public override float GetDistanceTo(Vector3Int blockPos)
	{
		return Mathf.Max(0, World.GetWaterHeight() - blockPos.y);
	}

	public override float GetBrightnessAt(Chunk chunk, float distance, bool inWater)
	{
		if (!inWater)
			return Mathf.Clamp01(brightness);
		//else
		//	return Mathf.Clamp01(0.5f * brightness);

		float falloff = 1f - distance * (1f / (waterFalloffFactor * SeedlessRandom.NextFloatInRange(0.55f, 1)));
		falloff = Mathf.Clamp01(falloff);

		for (int i = 1; i < waterExponent; i++)
			falloff *= falloff;

		return 0.75f * falloff * brightness;
	}

	public override float GetAttenAt(Chunk chunk, float distance, bool inWater)
	{
		if (!inWater)
			return 0;

		return 0;
	}

	public override float GetColorOpacityAt(Chunk chunk, float distance, bool inWater)
	{
		if (!inWater)
			return Mathf.Clamp01(brightness);
		//else
		//	return Mathf.Clamp01(0.5f * brightness);

		distance = Mathf.Max(0, distance - 1);

		float falloff = 1f - distance * (1f / (waterFalloffFactor * SeedlessRandom.NextFloatInRange(0.55f, 1)));
		falloff = Mathf.Clamp01(falloff);

		for (int i = 1; i < waterExponent; i++)
			falloff *= falloff;

		return 0.75f * falloff * brightness;
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
