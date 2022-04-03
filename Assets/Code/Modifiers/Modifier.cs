using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Modifier
{
	public struct Mask
	{
		public bool fill;
		public bool replace;
	}

	public string label = "";

	public bool active = true;

	private bool didInit = false;

	public virtual bool Init()
	{
		if (didInit)
			return false;
		didInit = true;

		return true;
	}

	public virtual void ApplyModifier(Chunk chunk)
	{
		
	}

	protected delegate void BlockPosAction(Vector3Int pos);

	protected void ApplyToAll(BlockPosAction action, int scaleFactor, Vector3Int min, Vector3Int max)
	{
		for (int x = min.x; x <= max.x; x += scaleFactor)
		{
			for (int y = min.y; y <= max.y; y += scaleFactor)
			{
				for (int z = min.z; z <= max.z; z += scaleFactor)
				{
					action(new Vector3Int(x, y, z));
				}
			}
		}
	}
}
