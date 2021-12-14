using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class SeedlessRandom : MonoBehaviour
{
	private static SeedlessRandom Instance;

	static int seed = Environment.TickCount;

	static readonly ThreadLocal<System.Random> random =
		new ThreadLocal<System.Random>(() => new System.Random(Interlocked.Increment(ref seed)));

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

	public static int NextInt()
	{
		return random.Value.Next();
	}

	public static float NextFloat()
	{
		return (float)random.Value.NextDouble();
	}

	public static int NextIntInRange(int min, int max)
	{
		return (int)NextFloatInRange(min, max);
	}

	public static float NextFloatInRange(float min, float max)
	{
		return min + (max - min) * (float)random.Value.NextDouble();
	}

	public static Vector3 RandomPoint(float size)
	{
		return new Vector3(NextFloatInRange(-size, size), NextFloatInRange(-size, size), NextFloatInRange(-size, size));
	}

	public static int NextIntInRange(float min, float max)
	{
		return (int)NextFloatInRange(min, max);
	}
}
