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

	public float brightness; // How bright is this surface (0 is complete darkness, 1 is fully bright)
	public float lastBrightness; // How bright was this surface at the last light update

	public float colorTemp; // Lighting color temp of this surface (0 is red-orange, 1 is blue-gray)
	public float lastColorTemp; // Lighting color temp of this surface at the last light update

	public BlockSurface(Vector3 normal, Vector3 relativeOffset)
	{
		this.normal = normal;
		this.relativeOffset = relativeOffset;
	}
}
