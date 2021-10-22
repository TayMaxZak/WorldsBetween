using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
	public Vector3Int position; // Coordinates of chunk

	[SerializeField]
	private int chunkSize = 2;

	private Block[] blocks;

	private ChunkMesh chunkMesh;

	private void Awake()
	{
		UpdatePosition();

		// Create blocks
		CreateChunk();

		// Init chunk mesh
		chunkMesh = GetComponent<ChunkMesh>();
		chunkMesh.Init(this);

		// Initial light pass
		UpdateLight();
	}

	private void UpdatePosition()
	{
		Vector3 pos = transform.position;
		position.x = Mathf.RoundToInt(pos.x);
		position.y = Mathf.RoundToInt(pos.y);
		position.z = Mathf.RoundToInt(pos.z);
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
		UpdatePosition();

		// Set brightness
		foreach (Block b in blocks)
		{
			b.lastBrightness = b.brightness;

			// 1.0 up to 1 block away, then divide by distance sqr. Rapid decay of brightness
			float newBrightness = 1f / Mathf.Max(1, World.DistanceSqr(0, 0, 0, position.x + b.localX, position.y + b.localY, position.z + b.localZ));

			b.brightness = (byte)(newBrightness * 255f);
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

	public bool ValidIndex(int index)
	{
		return index >= 0 && blocks.Length > index;
	}

	public Block GetBlock(int index)
	{
		return blocks[index];
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawWireCube(transform.position + chunkSize / 2 * Vector3.one, chunkSize * Vector3.one);
	}
}
