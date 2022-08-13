using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseModifier : Modifier
{
	public Vector3 scale = Vector3.one * 0.05f;
	public float strength = 1;
	public float ribbonGateMult = 1;
	public float gate = 0;
	private float boundary = 0.5f;
	public int ribbonCount = 0;

	public Block block = BlockList.EMPTY;
	public Mask mask = new Mask() { fill = false };

	private Vector3 randomOffset = Vector3.zero;

	// TODO: Strength as chance to exceed 0.5
	public NoiseModifier(Block block, Mask mask, float strength, Vector3 scale)
	{
		this.block = block;
		this.mask = mask;
		this.strength = strength;
		this.scale = scale;

		stage = ModifierStage.Terrain;
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

		BlockPosAction toApply = ApplyNoise;

		ApplyToAll(toApply, chunk, chunk.position, chunk.position + Vector3Int.one * (World.GetChunkSize() - 1));
	}

	protected virtual bool ApplyNoise(Vector3Int pos, Chunk chunk)
	{
		float noise = GetNoiseAt(WarpPosition(pos));

		if (noise > boundary)
		{
			if (mask.fill && !World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
				World.SetBlock(pos.x, pos.y, pos.z, block);

			if (mask.replace && World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
				World.SetBlock(pos.x, pos.y, pos.z, block);
		}

		return true;
	}

	protected virtual Vector3 WarpPosition(Vector3 pos)
	{
		return pos;
	}

	protected float GetNoiseAt(Vector3 pos)
	{
		float x = (float)pos.x * scale.x;
		float y = (float)pos.y * scale.y;
		float z = (float)pos.z * scale.z;

		float xPlane = Mathf.PerlinNoise(y + randomOffset.x, z + randomOffset.x);
		float yPlane = Mathf.PerlinNoise(z + randomOffset.y, x + randomOffset.y);
		float zPlane = Mathf.PerlinNoise(x + randomOffset.z, y + randomOffset.z);

		float noise = Mathf.Clamp01((4 / 3f) * (xPlane + yPlane + zPlane) / 3f);

		for (int ribs = ribbonCount; ribs > 0; ribs--)
			noise = Mathf.Clamp01(Mathf.Abs(noise * ribbonGateMult - 0.5f) * 2f);

		noise = Mathf.Clamp01(noise - gate) / (1 - gate);

		return noise * strength;
	}
}
