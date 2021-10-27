using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise : MonoBehaviour
{
	private static float scale = 0.0424f;
	private static float offset = 244444.0424f;

	private static Noise Instance;

	private void Awake()
	{
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}
		else
			Instance = this;
	}

	public static float GetNoiseAt(float x, float y, float z)
	{
		x = x * scale + offset;
		y = y * scale + offset;
		z = z * scale + offset;

		float xPlane = Mathf.PerlinNoise(y, z);
		float yPlane = Mathf.PerlinNoise(z, x);
		float zPlane = Mathf.PerlinNoise(x, y);

		float noise = Mathf.Clamp01((xPlane + yPlane + zPlane) / 3f);

		return noise;
	}
}
