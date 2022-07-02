﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HeightFinder
{
	public Vector3 scale = Vector3.one * 0.05f;
	public float strength = 1;
	public float ribbonGateMult = 1;
	public float gate = 0;
	private float boundary = 0.5f;
	public int ribbonCount = 0;

	private Vector3 randomOffset = Vector3.zero;

	// TODO: Strength as chance to exceed 0.5
	public HeightFinder(float strength, Vector3 scale)
	{
		this.strength = strength;
		this.scale = scale;
	}

	public virtual bool Init()
	{
		SeedNoise();

		return true;
	}

	public virtual float GetHeight(int baseWorldHeight, Vector3 pos)
	{
		return baseWorldHeight + GetNoiseAt(pos);
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
