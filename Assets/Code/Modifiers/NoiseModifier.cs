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
	public bool addOrSub = false;

	private Vector3 randomOffset = Vector3.zero;

	// TODO: Strength as chance to exceed 0.5
	public NoiseModifier(bool addOrSub, float strength, Vector3 scale)
	{
		this.addOrSub = addOrSub;
		this.strength = strength;
		this.scale = scale;
	}

	public override bool Init()
	{
		base.Init();

		SeedNoise();

		return true;
	}

	protected void SeedNoise()
	{
		float offsetAmount = 999999;

		randomOffset = new Vector3(
			Random.value + (int)(Random.value * offsetAmount),
			Random.value + (int)(Random.value * offsetAmount),
			Random.value + (int)(Random.value * offsetAmount)
		);
	}

	public override void ApplyModifier(Chunk chunk)
	{
		if (!active)
			return;

		BlockPosAction toApply = ApplyNoise;

		ApplyToAll(toApply, chunk.position, chunk.position + Vector3Int.one * (World.GetChunkSize() - 1));
	}

	protected virtual void ApplyNoise(Vector3Int pos)
	{
		float noise = GetNoiseAt(WarpPosition(pos));

		if (noise > boundary)
		{
			if (addOrSub)
				World.SetBlock(pos.x, pos.y, pos.z, BlockList.FILLED);
			else
				World.SetBlock(pos.x, pos.y, pos.z, BlockList.EMPTY);
		}
	}

	protected virtual Vector3 WarpPosition(Vector3 pos)
	{
		return pos;
	}

	protected float GetNoiseAt(Vector3 pos)
	{
		float x = pos.x * scale.x + randomOffset.x;
		float y = pos.y * scale.y + randomOffset.y;
		float z = pos.z * scale.z + randomOffset.z;

		float xPlane = Mathf.PerlinNoise(y, z);
		float yPlane = Mathf.PerlinNoise(z, x);
		float zPlane = Mathf.PerlinNoise(x, y);

		float noise = Mathf.Clamp01(1.33f * (xPlane + yPlane + zPlane) / 3f);

		for (int ribs = ribbonCount; ribs > 0; ribs--)
			noise = Mathf.Clamp01(Mathf.Abs(noise * ribbonGateMult - 0.5f) * 2f);

		noise = Mathf.Clamp01(noise - gate) / (1 - gate);

		return noise * strength;
	}
}
