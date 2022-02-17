using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Modifier
{
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

	protected void ApplyToAll(BlockPosAction action, Vector3Int min, Vector3Int max)
	{
		for (int x = min.x; x <= max.x; x++)
		{
			for (int y = min.y; y <= max.y; y++)
			{
				for (int z = min.z; z <= max.z; z++)
				{
					action(new Vector3Int(x, y, z));
				}
			}
		}
	}
}
