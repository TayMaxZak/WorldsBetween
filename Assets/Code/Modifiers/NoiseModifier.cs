using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseModifier : Modifier
{
	public Vector3 scale = Vector3.one;
	public float offset = 2444.0424f;
	public float strength = 1;
	public float preMul = 1;
	public float gate = 0;
	public float boundary = 0.5f;
	public int ribbonCount = 0;
	public bool addOrSub = false;

	private Vector3 randomOffset = Vector3.zero;

	public override bool Init()
	{
		if (!base.Init())
			return false;

		randomOffset = new Vector3(Random.value, Random.value, Random.value) * offset;

		return true;
	}

	public override ModifierOutput OutputAt(float x, float y, float z)
	{
		x = x * scale.x + offset + randomOffset.x;
		y = y * scale.y + offset + randomOffset.y;
		z = z * scale.z + offset + randomOffset.z;

		float xPlane = Mathf.PerlinNoise(y, z);
		float yPlane = Mathf.PerlinNoise(z, x);
		float zPlane = Mathf.PerlinNoise(x, y);

		float noise = Mathf.Clamp01(1.33f * (xPlane + yPlane + zPlane) / 3f);

		for (int ribs = ribbonCount; ribs > 0; ribs--)
			noise = Mathf.Clamp01(Mathf.Abs(noise * preMul - 0.5f) * 2f);

		noise = Mathf.Clamp01(noise - gate) / (1 - gate);

		return new ModifierOutput { passed = noise * strength > boundary, addOrSub = addOrSub };
	}
}
