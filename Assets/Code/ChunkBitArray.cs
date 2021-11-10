using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 3D bit array used for shadow bit arrays
public class ChunkBitArray
{
	private BitArray bits;

	private readonly int size;

	public ChunkBitArray(int dimension)
	{
		size = dimension;
		bits = new BitArray(size * size * size);
		bits.SetAll(true);
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
