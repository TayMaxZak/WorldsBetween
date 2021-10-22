using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
	[SerializeField]
	private int chunkSize = 2;

	private Block[] blocks;

	private ChunkMesh chunkMesh;

	public void CreateChunk()
	{
		blocks = new Block[chunkSize * chunkSize * chunkSize];

		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					Block b = new Block();
					b.localX = x;
					b.localY = y;
					b.localZ = z;
					b.brightness = (byte)(Random.value * 256);

					blocks[CoordToIndex(x, y, z)] = b;
				}
			}
		}
	}

	void Awake()
	{
		// Create blocks
		CreateChunk();

		// Init chunk mesh
		chunkMesh = GetComponent<ChunkMesh>();
		chunkMesh.Init(this);

		// Initial light pass
		UpdateLight();
	}

	void UpdateLight()
	{
		chunkMesh.SetVertexColors(blocks);
	}

	public int CoordToIndex(int x, int y, int z)
	{
		return x * chunkSize * chunkSize + y * chunkSize + z;
	}
}
