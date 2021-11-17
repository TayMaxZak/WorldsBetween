using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSurface
{
	public Vector3 normal;
	public Vector3 relativeOffset; // Where is the CENTER of this surface relative to the ORIGIN of its associated block
	public Block block;

	// Which vertices are associated with the mesh of this surface?
	public int startIndex, endIndex;

	public BlockSurface(Vector3 normal, Vector3 relativeOffset)
	{
		this.normal = normal;
		this.relativeOffset = relativeOffset;
	}
}
