using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block
{
	public static readonly Block empty = new Block(0, 0, 0, 255, 0);

	public byte localX, localY, localZ; // Coordinates in chunk local space

	public byte opacity; // How full is this block (0 is empty air, 15 is completely solid)

	// TODO: Make into a flags enum; mor eclear what each value means, more memory efficient, etc.
	public byte maybeNearAir; // Is this block visible to the player
	public byte needsUpdate; // nonzero = light changed and should be updated
	public byte updatePending; // nonzero = update in progress, do not touch
	public byte postUpdate; // nonzero = needs to finalize update

	public byte blockType; // 0 = normal block, 1 = grass

	public Block(byte localX, byte localY, byte localZ, byte opacity) : this(localX, localY, localZ, opacity, 0)
	{
		this.localX = localX;
		this.localY = localY;
		this.localZ = localZ;

		maybeNearAir = 0;

		needsUpdate = 0;
		updatePending = 0;
		postUpdate = 0;

		this.opacity = opacity;
	}

	private Block(byte localX, byte localY, byte localZ, byte opacity, byte nearAir)
	{
		this.localX = localX;
		this.localY = localY;
		this.localZ = localZ;

		maybeNearAir = nearAir;

		needsUpdate = 0;
		updatePending = 0;
		postUpdate = 0;

		this.opacity = opacity;
	}

	public bool IsAir()
	{
		return opacity <= 127;
	}

	public Vector3Int GetLocalPosVector()
	{
		return new Vector3Int(localX, localY, localZ);
	}
}
