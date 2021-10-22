﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
	[SerializeField]
	private int chunkSize = 2;

	private Block[] blocks;

	private ChunkMesh chunkMesh;

	private void Awake()
	{
		// Create blocks
		CreateChunk();

		// Init chunk mesh
		chunkMesh = GetComponent<ChunkMesh>();
		chunkMesh.Init(this);

		// Initial light pass
		UpdateLight();
	}

	public void CreateChunk()
	{
		blocks = new Block[chunkSize * chunkSize * chunkSize];

		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					blocks[CoordToIndex(x, y, z)] = new Block(x, y, z, 0, 1);
				}
			}
		}
	}

	public void UpdateLight()
	{
		// Set brightness
		foreach (Block b in blocks)
		{
			b.lastBrightness = b.brightness;
			b.brightness = (byte)(Random.value * 256);
		}
	}

	public void InterpLight(float partialTime)
	{
		// Apply vertex colors, interpolating between previous brightness and new brightness by partial time
		chunkMesh.SetVertexColors(blocks, partialTime);
	}

	public int CoordToIndex(int x, int y, int z)
	{
		return x * chunkSize * chunkSize + y * chunkSize + z;
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawWireCube(transform.position + chunkSize / 2 * Vector3.one, chunkSize * Vector3.one);
	}
}
