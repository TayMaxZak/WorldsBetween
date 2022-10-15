using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSound
{
	public Vector3 pos;
	public Sound sound;

	public BlockSound(Vector3 pos, Sound sound)
	{
		this.pos = pos;
		this.sound = sound;
	}
}
