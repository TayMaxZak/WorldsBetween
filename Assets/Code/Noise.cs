﻿using System.Collections;
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

	protected override bool Init()
	{
		if (!base.Init())
			return false;

		randomOffset = new Vector3(Random.value, Random.value, Random.value) * offset;

		return true;
	}

	public override float StrengthAt(float x, float y, float z)
	{
		x = x * scale + offset + randomOffset.x;
		y = y * scale + offset + randomOffset.y;
		z = z * scale + offset + randomOffset.z;

		float xPlane = Mathf.PerlinNoise(y, z);
		float yPlane = Mathf.PerlinNoise(z, x);
		float zPlane = Mathf.PerlinNoise(x, y);

		float noise = Mathf.Clamp01((xPlane + yPlane + zPlane) / 3f);

		noise = Mathf.Clamp01(noise - cutoff) / (1 - cutoff);

		return noise * strength;
	}
}