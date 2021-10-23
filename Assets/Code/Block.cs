using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block
{
	public byte localX, localY, localZ; // Coordinates in chunk local space

	public byte opacity; // How full is this block (0 is empty air, 15 is completely solid)

	public byte brightness; // How bright is this block (0 is complete darkness, 15 is fully bright) // TODO: R, G, and B
	public byte lastBrightness; // How bright was this block at the last light update

	public byte colorTemp; // Lighting color temp of this block (0 is red-orange, 127 is white, 255 is blue-gray)
	public byte lastColorTemp; // Lighting color temp of this block at the last light update

	public byte needsUpdate; // 0 = no updating needed, nonzero = light changed and should be updated. greater number = higher priority
	public byte updatePending; // 0 = no updating needed, nonzero = light changed and should be updated. greater number = higher priority

	public int startIndex, endIndex; // Which vertices to search?

	public Block(byte localX, byte localY, byte localZ, byte opacity)
	{
		this.localX = localX;
		this.localY = localY;
		this.localZ = localZ;

		brightness = 0;
		lastBrightness = brightness;

		colorTemp = 127;
		lastColorTemp = colorTemp;

		needsUpdate = 0;
		updatePending = 0;

		this.opacity = opacity;
	}
}
