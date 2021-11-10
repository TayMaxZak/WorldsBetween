using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SeedlessRandom : MonoBehaviour
{
	private static SeedlessRandom Instance;

	private System.Random random = new System.Random(1);

	private void Awake()
	{
		// Ensure singleton
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}
		else
			Instance = this;
	}

	public static float NextFloat()
	{
		return (float)Instance.random.NextDouble();
	}

	public static float NextFloatInRange(float min, float max)
	{
		return min + (max - min) * (float)Instance.random.NextDouble();
	}
}
