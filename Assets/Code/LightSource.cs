using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LightSource
{
	public int worldX, worldY, worldZ; // Coordinates in world space

	protected int lastWorldX, lastWorldY, lastWorldZ; // Last coordinates in world space (to determine if dirty)

	public float brightness = 1; // From 0 to 1
	public float colorTemp = 0; // From -1 to 1

	protected List<Vector3Int> affectedChunks = new List<Vector3Int>();
	protected List<Vector3Int> oldAffectedChunks = new List<Vector3Int>();

	public bool dirty = true;

	public LightSource(float brightness, float colorTemp, Vector3 pos)
	{
		this.brightness = brightness;
		this.colorTemp = colorTemp;

		UpdatePosition(pos);
	}

	public abstract List<Vector3Int> FindAffectedChunks();

	public void UpdatePosition(Vector3 newPos)
	{
		worldX = Mathf.RoundToInt(newPos.x);
		worldY = Mathf.RoundToInt(newPos.y);
		worldZ = Mathf.RoundToInt(newPos.z);

		if (worldX != lastWorldX || worldY != lastWorldY || worldZ != lastWorldZ)
		{
			dirty = true;
			OnDirty();
		}

		lastWorldX = worldX;
		lastWorldY = worldY;
		lastWorldZ = worldZ;
	}

	protected abstract void OnDirty();

	public abstract float GetBrightnessAt(Vector3Int at, bool inWater);

	public abstract float GetColorTemperatureAt(float value, bool inWater);

	public List<Vector3Int> GetAffectedChunks()
	{
		return affectedChunks;
	}
}
