﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DirectionalLightSource : LightSource
{
	private Vector3 direction;

	private float waterFalloffFactor = 128;
	private float waterExponent = 3;

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

	public override float GetBrightnessAt(Chunk chunk, BlockSurface surface, float distance, bool inWater)
	{
		if (!inWater)
		{
			float dotMult = Mathf.Clamp01(Vector3.Dot(direction, -surface.normal));

			dotMult = 1 - (1 - dotMult) * (1 - dotMult);

			return Mathf.Clamp01(brightness * dotMult);
		}

		float falloff = 1f - distance * (1f / (waterFalloffFactor * SeedlessRandom.NextFloatInRange(0.64f, 1)));
		falloff = Mathf.Clamp01(falloff);

		for (int i = 1; i < waterExponent; i++)
			falloff *= falloff;

		return 0.7f * falloff * brightness;
	}

	public override float GetShadowBrightnessAt(Chunk chunk, BlockSurface surface, float distance, bool inWater)
	{
		if (!inWater)
			return SeedlessRandom.NextFloatInRange(0.1f, 0.2f);

		return 0;
	}

	public override float GetColorOpacityAt(Chunk chunk, BlockSurface surface, float distance, bool inWater)
	{
		if (!inWater)
			return Mathf.Clamp01(brightness);

		distance = Mathf.Max(0, distance - 1);

		float falloff = 1f - distance * (1f / waterFalloffFactor);
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
			bool occluded = !World.GetBlockFor(
				(int)(blockPos.x + -direction.x * i + adj),
				(int)(blockPos.y + -direction.y * i + adj),
				(int)(blockPos.z + -direction.z * i + adj)
			).IsAir();

			if (occluded)
				return true;
		}

		return false;
	}
}
