using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Noise : Modifier
{
	public float scale = 0.0424f;
	public float offset = 2444.0424f;
	public float strength = 1;
	public float cutoff = 0;

	private Vector3 randomOffset = Vector3.zero;

	public override void Init()
	{
		base.Init();

		//randomOffset = new Vector3(Random.value, Random.value, Random.value) * offset;
	}

	public override float StrengthAt(float x, float y, float z)
	{
		x = x * scale + offset;
		y = y * scale + offset;
		z = z * scale + offset;

		float xPlane = Mathf.PerlinNoise(y, z);
		float yPlane = Mathf.PerlinNoise(z, x);
		float zPlane = Mathf.PerlinNoise(x, y);

		float noise = Mathf.Clamp01((xPlane + yPlane + zPlane) / 3f);

		noise = Mathf.Clamp01(noise - cutoff) / (1 - cutoff);

		return noise * strength;
	}
}
