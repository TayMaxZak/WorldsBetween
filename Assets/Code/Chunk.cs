using System.Collections;
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
					blocks[x, y, z] = new Block(x, y, z, 127, 255);
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
			{
				b.lastBrightness = b.brightness;
				b.lastColorTemp = b.colorTemp;
			}

			// 1.0 up to 1 block away, then divide by distance sqr. Rapid decay of brightness
			float addBrightness = light.brightness / Mathf.Max(1, World.DistanceSqr(light.worldX, light.worldY, light.worldZ, position.x + b.localX, position.y + b.localY, position.z + b.localZ));

			// Add to existing brightness (if not first pass). Affect less if already bright
			float oldBrightness = firstPass ? 0 : (b.brightness / 255f);
			float newBrightness = oldBrightness + (1 - oldBrightness) * addBrightness;
			newBrightness = Mathf.Clamp01(newBrightness);

			b.brightness = (byte)(newBrightness * 255f);

			// Affect color temp of blocks
			float oldColorTemp = firstPass ? 0 : (-1 + 2 * b.colorTemp / 255f);

			float newColorTemp = oldColorTemp + addBrightness * light.colorTemp;
			newColorTemp = Mathf.Clamp(newColorTemp, -1, 1);

			b.colorTemp = (byte)(255f * ((newColorTemp + 1) / 2));
		}
	}

	public void UpdateLightVisuals()
	{
		// Apply vertex colors, interpolating between previous brightness and new brightness by partial time
		chunkMesh.SetVertexColors(blocks);
	}

	public void ApplyCarver(Carver carver, bool firstPass)
	{
		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					float carve = carver.strength / World.DistanceSqr(carver.worldX, carver.worldY, carver.worldZ, position.x + x, position.y + y, position.z + z);

					float newOpacity = (firstPass ? 1 : blocks[x, y, z].opacity / 255f) - carve;

					blocks[x, y, z].opacity = (byte)(Mathf.Clamp01(newOpacity) * 255);
				}
			}
		}
	}

	public void UpdateOpacityVisuals()
	{
		chunkMesh.SetOpacity(blocks);
	}

	// Utility
	public bool ContainsPos(int x, int y, int z)
	{
		return x >= 0 && chunkSize > x && y >= 0 && chunkSize > y && z >= 0 && chunkSize > z;
	}

	public Block GetBlock(int x, int y, int z)
	{
		return blocks[x, y, z];
	}

	public int GetChunkSize()
	{
		return chunkSize;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.DrawWireCube(transform.position + chunkSize / 2f * Vector3.one, chunkSize * Vector3.one);
	}
}
