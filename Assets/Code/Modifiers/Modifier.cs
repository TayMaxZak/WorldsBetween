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

	public enum ModifierStage
	{
		Height,
		Terrain,
		Feature,
		Decorator
	}
	public ModifierStage stage;

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

	protected delegate bool BlockPosAction(Vector3Int pos, Chunk chunk);

	protected void ApplyToAll(BlockPosAction action, Chunk chunk, Vector3Int min, Vector3Int max)
	{
		for (int x = min.x; x <= max.x; x += chunk.scaleFactor)
		{
			for (int y = min.y; y <= max.y; y += chunk.scaleFactor)
			{
				for (int z = min.z; z <= max.z; z += chunk.scaleFactor)
				{
					action(new Vector3Int(x, y, z), chunk);
				}
			}
		}
	}
}
