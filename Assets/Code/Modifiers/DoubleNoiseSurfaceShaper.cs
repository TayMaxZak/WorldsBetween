using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoubleNoiseSurfaceShaper : SurfaceShaper
{
	public Vector3 minScale = Vector3.one * 0.1f;
	public Vector3 maxScale = Vector3.one * 2.0f;

	private Vector3 randomOffset2 = Vector3.zero;

	// TODO: Strength as chance to exceed 0.5
	public DoubleNoiseSurfaceShaper(float strength, Vector3 minScale, Vector3 maxScale, Vector3 scale) : base(strength, scale)
	{
		this.minScale = minScale;
		this.maxScale = maxScale;
	}

	public override bool Init()
	{
		base.Init();

		SeedNoise2();

		return true;
	}

	protected void SeedNoise2()
	{
		float offsetAmount = 9999;

		randomOffset2 = new Vector3(
			Random.value - (Random.value * offsetAmount),
			Random.value - (Random.value * offsetAmount),
			Random.value - (Random.value * offsetAmount)
		);
	}

	protected override float GetNoiseAt(Vector3 pos)
	{
		// Use scaleScale for finding main scale
		Vector3 scaleScale = scale;
		float x2 = pos.x * scaleScale.x + randomOffset2.x;
		float y2 = pos.y * scaleScale.y + randomOffset2.y;
		float z2 = pos.z * scaleScale.z + randomOffset2.z;

		float xPlane2 = Mathf.PerlinNoise(y2, z2);
		float yPlane2 = Mathf.PerlinNoise(z2, x2);
		float zPlane2 = Mathf.PerlinNoise(x2, y2);

		float noise2 = Mathf.Clamp01((4 / 3f) * (xPlane2 + yPlane2 + zPlane2) / 3f);

		// Find main scale
		Vector3 mainScale = new Vector3(
			Mathf.Lerp(minScale.x, maxScale.x, noise2),
			Mathf.Lerp(minScale.y, maxScale.y, noise2),
			Mathf.Lerp(minScale.z, maxScale.z, noise2)
		);

		float x = pos.x * mainScale.x + randomOffset.x;
		float y = pos.y * mainScale.y + randomOffset.y;
		float z = pos.z * mainScale.z + randomOffset.z;

		float xPlane = Mathf.PerlinNoise(y, z);
		float yPlane = Mathf.PerlinNoise(z, x);
		float zPlane = Mathf.PerlinNoise(x, y);

		float noise = Mathf.Clamp01((4 / 3f) * (xPlane + yPlane + zPlane) / 3f);

		for (int ribs = ribbonCount; ribs > 0; ribs--)
			noise = Mathf.Clamp01(Mathf.Abs(noise * ribbonGateMult - 0.5f) * 2f);

		noise = Mathf.Clamp01(noise - gate) / (1 - gate);

		return (noise - 0.5f) * strength;
	}
}
