using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block
{
	public byte localX, localY, localZ; // Coordinates in chunk local space

	public byte brightness; // How bright is this block (0 is complete darkness, 15 is fully bright) // TODO: 255 for R, G, and B
	public byte opacity; // How full is this block (0 is empty air, 15 is completely solid)

	public static float GetFloatBrightness(byte brightness)
	{
		return brightness / 256f;
	}
}
