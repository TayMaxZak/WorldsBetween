using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LightSource
{
	[HideInInspector]
	public int worldX, worldY, worldZ; // Coordinates in world space

	protected int lastWorldX, lastWorldY, lastWorldZ; // Last coordinates in world space (to determine if dirty)

	public float brightness = 1; // From 0 to 1
	public float colorTemp = 0; // From -1 to 1

	protected List<Vector3Int> affectedChunks = new List<Vector3Int>();
	protected List<Vector3Int> oldAffectedChunks = new List<Vector3Int>();

	public LightSource(float brightness, float colorTemp, Vector3 pos) : this(brightness, colorTemp)
	{
		UpdatePosition(pos);
	}

	public LightSource(float brightness, float colorTemp)
	{
		this.brightness = brightness;
		this.colorTemp = colorTemp;
	}

	public LightSource()
	{
		affectedChunks = new List<Vector3Int>();
		oldAffectedChunks = new List<Vector3Int>();
	}

	public abstract List<Vector3Int> FindAffectedChunkCoords();

	public void UpdatePosition(Vector3 newPos)
	{
		worldX = Mathf.RoundToInt(newPos.x);
		worldY = Mathf.RoundToInt(newPos.y);
		worldZ = Mathf.RoundToInt(newPos.z);

		if (worldX != lastWorldX || worldY != lastWorldY || worldZ != lastWorldZ)
		{
			
		}

		lastWorldX = worldX;
		lastWorldY = worldY;
		lastWorldZ = worldZ;
	}

	public virtual float GetDistanceTo(Vector3Int blockPos)
	{
		float d = Utils.DistSquared(worldX, worldY, worldZ, blockPos.x, blockPos.y, blockPos.z);
		return Mathf.Sqrt(d);
	}

	public abstract float GetBrightnessAt(Chunk chunk, Vector3 pos, float distance);

	public abstract float GetColorOpacityAt(Chunk chunk, Vector3 pos, float value);

	public List<Vector3Int> GetAffectedChunkCoords()
	{
		return affectedChunks;
	}

	public virtual bool IsShadowed(Vector3Int blockPos)
	{
		return false;
	}
}
