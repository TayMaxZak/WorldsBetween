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
	private Queue<Block> postUpdate = new Queue<Block>();

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
					if (blocks[x, y, z].nearAir == 0)
						continue;

					blocks[x, y, z].needsUpdate = 255;
				}
			}
		}
	}

	public void AddLight(LightSource light, bool firstPass, bool lastPass)
	{
		int counter = 0;

		// Set brightness
		foreach (Block block in blocks)
		{
			// Block is already correct light
			if (block.needsUpdate == 0)
				continue;

			// Is block currently processing?
			if (block.updatePending > 0 || block.postUpdate > 0)
				continue;

			counter++;

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

			if (lastPass && block.updatePending == 0 && block.postUpdate == 0)
			{
				block.updatePending = 255;

				// 1.0 priority never reached???
				if (newBrightness > 0)
					toLightUpdate.Enqueue(block, 1 - newBrightness);
			}
		}

		//Debug.Log(name + " blocks to add light = " + counter);
	}

	public void UpdateLightVisuals()
	{
		//Debug.Log(name + " toUpdate size = " + toLightUpdate.Count + " postUpdate size = " + postUpdate.Count);

		Block inQueue;

		// Handle previously dequeued blocks first
		while (postUpdate.Count > 0)
		{
			inQueue = postUpdate.Dequeue();

			inQueue.postUpdate = 0;

			inQueue.needsUpdate = 0;

			bool diff = false;
			if (inQueue.lastBrightness != inQueue.brightness)
			{
				inQueue.lastBrightness = inQueue.brightness;
				diff = true;
			}
			if (inQueue.lastColorTemp != inQueue.colorTemp)
			{
				inQueue.lastColorTemp = inQueue.colorTemp;
				diff = true;
			}

			if (diff)
				chunkMesh.SetVertexColors(inQueue);
		}

		// Apply vertex colors to most important blocks to update
		int count = toLightUpdate.Count;
		for (int i = 0; i < Mathf.Min(count, World.GetUpdateSize()); i++)
		{
			inQueue = toLightUpdate.Dequeue();
			inQueue.updatePending = 0;

			chunkMesh.SetVertexColors(inQueue);

			inQueue.postUpdate = 255;
			postUpdate.Enqueue(inQueue);
		}

		chunkMesh.ApplyVertexColors();
	}

	public void ApplyCarver(Carver carver, bool firstPass, bool lastPass)
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

	public void CacheNearAir()
	{
		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					// Remember if this block is bordering air
					if (blocks[x, y, z].opacity <= 127)
					{
						blocks[x, y, z].nearAir = 255;
						continue;
					}

					bool nearAir = false;
					bool chunkBorder = false;

					// Check chunk border
					if (x == 0 || x == chunkSize - 1)
						chunkBorder = true;
					else if (y == 0 || y == chunkSize - 1)
						chunkBorder = true;
					else if (z == 0 || z == chunkSize - 1)
						chunkBorder = true;

					if (!chunkBorder)
					{
						// Check adjacent blocks in this chunk
						if (blocks[x - 1, y, z].opacity <= 127)
							nearAir = true;
						else if (blocks[x + 1, y, z].opacity <= 127)
							nearAir = true;
						else if (blocks[x, y - 1, z].opacity <= 127)
							nearAir = true;
						else if (blocks[x, y + 1, z].opacity <= 127)
							nearAir = true;
						else if (blocks[x, y, z - 1].opacity <= 127)
							nearAir = true;
						else if (blocks[x, y, z + 1].opacity <= 127)
							nearAir = true;
					}
					else
					{
						// Check adjacent blocks (in world space this time)
						if (World.GetBlockFor(position.x + x - 1,		position.y + y,		position.z + z).opacity <= 127)
							nearAir = true;
						else if (World.GetBlockFor(position.x + x + 1,	position.y + y,		position.z + z).opacity <= 127)
							nearAir = true;
						else if (World.GetBlockFor(position.x + x,		position.y + y - 1, position.z + z).opacity <= 127)
							nearAir = true;
						else if (World.GetBlockFor(position.x + x,		position.y + y + 1, position.z + z).opacity <= 127)
							nearAir = true;
						else if (World.GetBlockFor(position.x + x,		position.y + y,		position.z + z - 1).opacity <= 127)
							nearAir = true;
						else if (World.GetBlockFor(position.x + x,		position.y + y,		position.z + z + 1).opacity <= 127)
							nearAir = true;
					}

					blocks[x, y, z].nearAir = (byte)(nearAir ? 255 : 0);
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
