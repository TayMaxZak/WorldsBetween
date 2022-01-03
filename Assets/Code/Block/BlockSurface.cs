using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSurface
{
	public Chunk chunk;
	public Block block;
	public Vector3 normal;
	public Vector3 relativeOffset; // Where is the CENTER of this surface relative to the CENTER (0.5) of its associated block

	// Which vertices are associated with the mesh of this surface?
	public int startIndex, endIndex;

	public int startVegIndex, endVegIndex;

	public float brightness; // How bright is this surface (0 is complete darkness, 1 is fully bright)
	public float lastBrightness; // How bright was this surface at the last light update

	public float colorTemp; // Lighting color temp of this surface (-1 is red-orange, 1 is blue-gray)
	public float lastColorTemp; // Lighting color temp of this surface at the last light update

	private static Vector3 blockCenter = new Vector3(0.5f, 0.5f, 0.5f);

	public BlockSurface(Chunk chunk, Block block, Vector3 normal, Vector3 relativeOffset)
	{
		this.chunk = chunk;
		this.block = block;
		this.normal = normal;
		this.relativeOffset = relativeOffset;
	}

	public Vector3 GetLocalPosition()
	{
		return blockCenter + relativeOffset + block.GetLocalPosVector();
	}

	public Vector3 GetWorldPosition()
	{
		return GetLocalPosition() + chunk.position;
	}

	public Vector3 GetBlockWorldPosition()
	{
		return blockCenter + block.GetLocalPosVector() + chunk.position;
	}

	public Vector3Int GetAdjBlockLocalCoord()
	{
		return new Vector3Int(
			Mathf.FloorToInt(blockCenter.x + relativeOffset.x + normal.x * 0.5f + block.localX),
			Mathf.FloorToInt(blockCenter.y + relativeOffset.y + normal.y * 0.5f + block.localY),
			Mathf.FloorToInt(blockCenter.z + relativeOffset.z + normal.z * 0.5f + block.localZ)
		);
	}

	public Vector3Int GetAdjBlockWorldCoord()
	{
		return GetAdjBlockLocalCoord() + chunk.position;
	}

	public Vector3Int GetBlockWorldCoord()
	{
		return block.GetLocalPosVector() + chunk.position;
	}

	public override int GetHashCode()
	{
		int chunkSize = World.GetChunkSize();
		return block.localX * chunkSize * chunkSize + block.localY * chunkSize + block.localZ;
	}
}
