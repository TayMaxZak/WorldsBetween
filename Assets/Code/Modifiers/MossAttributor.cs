using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MossAttributor : Modifier
{
	public Vector3 scale = new Vector3(0.08f, 0.16f, 0.08f);
	public float chance = 1;

	public float minValue = 0;
	public float maxValue = 1;

	private Vector3 randomOffset = Vector3.zero;

	// TODO: Strength as chance to exceed 0.5
	public MossAttributor(float chance, float minValue, float maxValue)
	{
		this.chance = chance;

		this.minValue = minValue;
		this.maxValue = maxValue;

		stage = ModifierStage.Decorator;
	}

	public override bool Init()
	{
		base.Init();

		SeedNoise();

		return true;
	}

	protected void SeedNoise()
	{
		float offsetAmount = 9999;

		randomOffset = new Vector3(
			Random.value + (float)(Random.value * offsetAmount),
			Random.value + (float)(Random.value * offsetAmount),
			Random.value + (float)(Random.value * offsetAmount)
		);
	}

	public override void ApplyModifier(Chunk chunk)
	{
		if (!active)
			return;

		BlockPosAction toApply = ApplyDecorator;

		RandomlyApplyToAll(toApply, chunk, chunk.position, chunk.position + Vector3Int.one * (World.GetChunkSize() - 1));
	}

	protected void RandomlyApplyToAll(BlockPosAction action, Chunk chunk, Vector3Int min, Vector3Int max)
	{
		for (int x = min.x; x <= max.x; x++)
		{
			for (int y = min.y; y <= max.y; y++)
			{
				for (int z = min.z; z <= max.z; z++)
				{
					action(new Vector3Int(x, y, z), chunk);
				}
			}
		}
	}

	protected virtual bool ApplyDecorator(Vector3Int pos, Chunk chunk)
	{
		float noise = GetNoiseAt(pos);
		float rand = SeedlessRandom.NextFloatInRange(-0.05f * chance, 0.05f * chance);

		// Will not pass
		if (noise + rand > chance)
			return false;

		bool ground;
		bool wall;
		bool ceiling;

		// Check adjacent blocks
		{
			bool ruf = World.GetBlock(pos + new Vector3Int(0, 0, 0)).IsRigid();
			bool luf = World.GetBlock(pos + new Vector3Int(-1, 0, 0)).IsRigid();
			bool rdf = World.GetBlock(pos + new Vector3Int(0, -1, 0)).IsRigid();
			bool ldf = World.GetBlock(pos + new Vector3Int(-1, -1, 0)).IsRigid();

			bool rub = World.GetBlock(pos + new Vector3Int(0, 0, -1)).IsRigid();
			bool lub = World.GetBlock(pos + new Vector3Int(-1, 0, -1)).IsRigid();
			bool rdb = World.GetBlock(pos + new Vector3Int(0, -1, -1)).IsRigid();
			bool ldb = World.GetBlock(pos + new Vector3Int(-1, -1, -1)).IsRigid();

			ground = (rdf && ldf && rdb && ldb) && (!ruf || !luf || !rub || !lub);
			wall = !(rdf && ldf && rdb && ldb) && (!ruf || !luf || !rub || !lub) && !(ruf && luf && rub && lub);
			ceiling = (ruf && luf && rub && lub) && (!rdf || !ldf || !rdb || !ldb);
		}

		// Not fully in wall / fully in the air
		if (!(ground || wall || ceiling))
			return false;

		float penalty = 0;
		if (ceiling)
			penalty = 0.2f * chance;
		if (wall)
			penalty = 0.1f * chance;

		// Check noise again, considering what surface we're on
		if (noise + rand + penalty > chance)
			return false;

		BlockAttributes attr = World.GetAttributes(pos);
		// Less moss towards edges of noise
		//float remappedNoise = Mathf.Clamp01(Remap(1 - noise, chance, Mathf.Min(chance * 2, 1), 0.33f, 1));
		//float remappedNoise = Mathf.Clamp01(1 - noise);
		// Determined by both noise and a random value
		//float mossAmount = Mathf.Min(SeedlessRandom.NextFloat(), remappedNoise);
		attr.SetMoss(Mathf.Lerp(minValue, maxValue, SeedlessRandom.NextFloat()));
		World.SetAttributes(pos, attr);

		return true;
	}

	private static float Remap(float value, float from1, float to1, float from2, float to2)
	{
		return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
	}

	protected float GetNoiseAt(Vector3 pos)
	{
		float x = (float)pos.x * scale.x;
		float y = (float)pos.y * scale.y;
		float z = (float)pos.z * scale.z;

		float xPlane = Mathf.PerlinNoise(y + randomOffset.x, z + randomOffset.x);
		float yPlane = Mathf.PerlinNoise(z + randomOffset.y, x + randomOffset.y);
		float zPlane = Mathf.PerlinNoise(x + randomOffset.z, y + randomOffset.z);

		float noise = Mathf.Clamp01((xPlane + yPlane + zPlane) / 3f);

		noise = Mathf.Clamp01(noise);

		return noise;
	}
}
