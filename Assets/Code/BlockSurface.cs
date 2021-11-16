using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSurface
{
	public Vector3 normal;
	public Vector3 relativeOffset; // Where is the center of this surface relative to its associated block

	public BlockSurface(Vector3 normal, Vector3 relativeOffset)
	{
		this.normal = normal;
		this.relativeOffset = relativeOffset;
	}
}
