﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
	public Vector3Int position; // Coordinates of chunk

	[SerializeField]
	private int chunkSize = 2;

	private Block[,,] blocks;

	private ChunkMesh chunkMesh;

	private void Awake()
	{
		UpdatePos();

		// Create blocks
		CreateBlocks();

		// Init chunk mesh
		chunkMesh = GetComponent<ChunkMesh>();
		chunkMesh.Init(this);
	}

	private void UpdatePos()
	{
		Vector3 pos = transform.position;
		position.x = Mathf.RoundToInt(pos.x);
		position.y = Mathf.RoundToInt(pos.y);
		position.z = Mathf.RoundToInt(pos.z);
	}

	public void CreateBlocks()
	{
		blocks = new Block[chunkSize, chunkSize, chunkSize];

		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					blocks[x, y, z] = new Block(x, y, z, 0, 1);
				}
			}
		}
	}

	public void CreateDummyMesh()
	{
		MeshFilter dummy1 = GetComponent<ChunkMesh>().dummyMesh;
		MeshFilter dummy2;

		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					dummy2 = Instantiate(dummy1, new Vector3(position.x + x + 0.5f, position.y + y + 0.5f, position.z + z + 0.5f), Quaternion.identity, transform);
					dummy2.name = x + ", " + y + ", " + z;
				}
			}
		}
	}

	public void AddLight(LightSource light, bool firstPass)
	{
		UpdatePos();
		light.UpdatePos();

		// Set brightness
		foreach (Block b in blocks)
		{
			if (firstPass)
				b.lastBrightness = b.brightness;

			// 1.0 up to 1 block away, then divide by distance sqr. Rapid decay of brightness
			float addBrightness = light.brightness / Mathf.Max(1, World.DistanceSqr(light.worldX, light.worldY, light.worldZ, position.x + b.localX, position.y + b.localY, position.z + b.localZ));

			// Add to existing brightness (if not first pass). Affect less if already bright
			float newBrightness = firstPass ? 0 : (b.brightness / 255f);
			newBrightness += (1 - newBrightness) * addBrightness;
			newBrightness = Mathf.Clamp01(newBrightness);

			b.brightness = (byte)(newBrightness * 255f);
		}
	}

	public void UpdateLightVisuals()
	{
		// Apply vertex colors, interpolating between previous brightness and new brightness by partial time
		chunkMesh.SetVertexColors(blocks);
	}

	public bool ContainsPos(int x, int y, int z)
	{
		return x >= 0 && chunkSize > x && y >= 0 && chunkSize > y && z >= 0 && chunkSize > z;
	}

	public Block GetBlock(int x, int y, int z)
	{
		return blocks[x, y, z];
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawWireCube(transform.position + chunkSize / 2 * Vector3.one, chunkSize * Vector3.one);
	}
}
