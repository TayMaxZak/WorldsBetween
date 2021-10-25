using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

public class Chunk : MonoBehaviour
{
	public Vector3Int position; // Coordinates of chunk

	[SerializeField]
	private int chunkSize = 8;

	private Block[,,] blocks;

	private ChunkMesh chunkMesh;

	private SimplePriorityQueue<Block> toLightUpdate = new SimplePriorityQueue<Block>();
	private Queue<Block> afterLightUpdate = new Queue<Block>();

	public int lightsToHandle = 0;

	private void Awake()
	{
		// Create blocks
		CreateBlocks();

		// Init chunk mesh
		chunkMesh = GetComponent<ChunkMesh>();
		chunkMesh.Init(this);
	}

	public void UpdatePos()
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
					blocks[x, y, z] = new Block(x, y, z, 255);
				}
			}
		}
	}

	public void MarkAsDirtyForLight()
	{
		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					blocks[x, y, z].needsUpdate = 255;
				}
			}
		}

		// Clear existing queues
		//toLightUpdate.Clear();
		//afterLightUpdate.Clear();
	}

	public void AddLight(LightSource light, bool firstPass, bool lastPass)
	{
		// Set brightness
		foreach (Block block in blocks)
		{
			if (block.updatePending > 0)
				continue;

			// 1.0 up to 1 block away, then divide by distance sqr. Rapid decay of brightness
			float addBrightness = light.brightness / Mathf.Max(1, Utils.DistanceSqr(light.worldX, light.worldY, light.worldZ, position.x + block.localX, position.y + block.localY, position.z + block.localZ));

			// Add to existing brightness (if not first pass). Affect less if already bright
			float oldBrightness = firstPass ? 0 : (block.brightness / 255f);
			float newBrightness = oldBrightness + (1 - oldBrightness) * addBrightness;
			newBrightness = Mathf.Clamp01(newBrightness);

			block.brightness = (byte)(newBrightness * 255f);

			// Affect color temp of blocks
			float oldColorTemp = firstPass ? 0 : (-1 + 2 * block.colorTemp / 255f);

			float newColorTemp = oldColorTemp + addBrightness * light.colorTemp;
			newColorTemp = Mathf.Clamp(newColorTemp, -1, 1);

			block.colorTemp = (byte)(255f * ((newColorTemp + 1) / 2));

			// Add block to update queue
			if (lastPass && block.needsUpdate > 0)
			{
				block.updatePending = 255;

				toLightUpdate.Enqueue(block, 1 - newBrightness);
			}
		}
	}

	public void UpdateLightVisuals()
	{
		Block inQueue;

		// Handle previously dequeued blocks first
		while (afterLightUpdate.Count > 0)
		{
			inQueue = afterLightUpdate.Dequeue();

			inQueue.lastBrightness = inQueue.brightness;
			inQueue.lastColorTemp = inQueue.colorTemp;

			chunkMesh.SetVertexColors(inQueue);
		}

		// Apply vertex colors to most important blocks to update
		int count = toLightUpdate.Count;
		for (int i = 0; i < Mathf.Min(count, 16); i++)
		{
			inQueue = toLightUpdate.Dequeue();
			chunkMesh.SetVertexColors(inQueue);

			inQueue.needsUpdate = 0;
			inQueue.updatePending = 0;

			afterLightUpdate.Enqueue(inQueue);
		}

		chunkMesh.ApplyVertexColors();
	}

	public void ApplyCarver(Carver carver, bool firstPass)
	{
		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					float carve = carver.strength / Utils.DistanceSqr(carver.worldX, carver.worldY, carver.worldZ, position.x + x, position.y + y, position.z + z);

					float newOpacity = (firstPass ? 1 : blocks[x, y, z].opacity / 255f) - carve;

					blocks[x, y, z].opacity = (byte)(Mathf.Clamp01(newOpacity) * 255);
				}
			}
		}
	}

	public void UpdateOpacityVisuals()
	{
		chunkMesh.GenerateMesh(blocks);
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
		Gizmos.color = Utils.colorOrange;

		Gizmos.DrawWireCube(transform.position + chunkSize / 2f * Vector3.one, chunkSize * Vector3.one);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Utils.colorBlue;

		if (!Application.isPlaying)
			Gizmos.DrawWireCube(transform.position + chunkSize / 2f * Vector3.one, chunkSize * Vector3.one);
	}
}
