using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 3D bit array used for shadow bit arrays
public class ChunkBitArray
{
	private BitArray bits;

	private readonly int size;

	public bool needsCalc = true;

	public ChunkBitArray(int dimension, bool value)
	{
		needsCalc = true;

		size = dimension;
		bits = new BitArray(size * size * size, value);
		//for (int i = 0; i < bits.Length; i++)
		//	bits[i] = SeedlessRandom.NextFloat() > 0.5f;
	}

	public bool Get(int x, int y, int z)
	{
		return bits.Get(x * size * size + y * size + z);
	}

	public void Set(bool value, int x, int y, int z)
	{
		bits.Set(x * size * size + y * size + z, value);
	}
}
