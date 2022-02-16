using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseModifier : Modifier
{
	public Vector3 scale = Vector3.one * 0.05f;
	public float offset = 2444.0424f;
	public float strength = 1;
	public float ribbonGateMult = 1;
	public float gate = 0;
	public float boundary = 0.5f;
	public int ribbonCount = 0;
	public bool addOrSub = false;

	private Vector3 randomOffset = Vector3.zero;

	public NoiseModifier(bool addOrSub, Vector3 scale)
	{
		this.addOrSub = addOrSub;
		this.scale = scale;
	}

	public override bool Init()
	{
		base.Init();

		//if (!base.Init())
		//	return false;

		randomOffset = new Vector3(Random.value, Random.value, Random.value) * offset;

		return true;
	}

	public override void ApplyModifier(Chunk chunk)
	{
		BlockPosAction toApply = ApplyNoise;

		ApplyToAll(toApply, chunk.position, chunk.position + Vector3Int.one * (World.GetChunkSize() - 1));
	}

	private void ApplyNoise(Vector3Int pos)
	{
		float noise = GetNoiseAt(WarpPosition(pos));

		if (noise > boundary)
		{
			if (addOrSub)
				World.GetBlockFor(pos.x, pos.y, pos.z).opacity = 255;
			else
				World.GetBlockFor(pos.x, pos.y, pos.z).opacity = 0;
		}
	}

	protected virtual Vector3Int WarpPosition(Vector3Int input)
	{
		return input / (1 + ((int)input.magnitude / 3) % 9);
		//return input / (1 + Utils.MaxAbs(input) % 9);
		//return input / (1 + Utils.SumAbs(input) % 9);
		//return input;
	}

	private float GetNoiseAt(Vector3Int pos)
	{
		float x = pos.x * scale.x + offset + randomOffset.x;
		float y = pos.y * scale.y + offset + randomOffset.y;
		float z = pos.z * scale.z + offset + randomOffset.z;

		float xPlane = Mathf.PerlinNoise(y, z);
		float yPlane = Mathf.PerlinNoise(z, x);
		float zPlane = Mathf.PerlinNoise(x, y);

		float noise = Mathf.Clamp01(1.33f * (xPlane + yPlane + zPlane) / 3f);

		for (int ribs = ribbonCount; ribs > 0; ribs--)
			noise = Mathf.Clamp01(Mathf.Abs(noise * ribbonGateMult - 0.5f) * 2f);

		noise = Mathf.Clamp01(noise - gate) / (1 - gate);

		return noise;
	}
}
