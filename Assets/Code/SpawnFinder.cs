using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[System.Serializable]
public class SpawnFinder
{
	private Vector3Int pos;
	public Vector3Int extents = Vector3Int.one;

	private bool isBusy;

	protected delegate void BlockPosAction(Vector3Int pos);

	private int airCount = 0;
	private int solidCount = 0;

	private int minAirCount = 75;
	private int minSolidCount = 25;

	// TODO: Add to a prio queue instead
	private bool foundPlayerPos = false;
	private Vector3 playerPos = Vector3.zero;

	private bool success = false;

	public void Move()
	{
		airCount = 0;
		solidCount = 0;

		foundPlayerPos = false;
		playerPos = Vector3.zero;

		// Move to a new location in the world near-ish the origin
		Vector3Int testBounds = Vector3Int.one * 32;
		pos = new Vector3Int(
			(int)(testBounds.x * SeedlessRandom.NextFloatInRange(-1, 1)),
			(int)(testBounds.y * SeedlessRandom.NextFloatInRange(-1, 1)),
			(int)(testBounds.z * SeedlessRandom.NextFloatInRange(-1, 1))
		);
	}

	public async void Tick()
	{
		BlockPosAction act = Scan;

		isBusy = true;

		await ScanAll(act, pos, pos + extents);

		await CheckConditions();

		if (success)
			return;
		else
			Move();

		isBusy = false;
	}

	protected virtual void Scan(Vector3Int pos)
	{
		if (pos.y > World.GetWaterHeight() && World.GetBlockFor(pos).IsAir())
		{
			airCount++;

			// Check playerPos conditions
			if (!foundPlayerPos)
			{
				if (World.GetBlockFor(pos + Vector3Int.up).IsAir() && !World.GetBlockFor(pos + Vector3Int.down).IsAir())
				{
					foundPlayerPos = true;

					playerPos = pos;
				}
			}
		}
		else
		{
			solidCount++;
		}
	}

	protected async Task ScanAll(BlockPosAction scanAction, Vector3Int min, Vector3Int max)
	{
		for (int x = min.x; x < max.x; x++)
		{
			for (int y = min.y; y < max.y; y++)
			{
				for (int z = min.z; z < max.z; z++)
				{
					scanAction(new Vector3Int(x, y, z));
				}
			}
		}

		await Task.Delay(5);
	}

	protected async Task CheckConditions()
	{
		if (airCount < minAirCount || solidCount < minSolidCount)
			return;

		if (!foundPlayerPos)
			return;

		success = true;

		await Task.Delay(5);

		Player.Instance.InitPlayerActor(playerPos);
	}

	public bool IsSuccessful()
	{
		return success;
	}

	public bool IsBusy()
	{
		return isBusy;
	}

	public void DrawGizmo()
	{
		Gizmos.color = success ? Utils.colorBlue : Utils.colorOrange;

		Gizmos.DrawWireCube(pos + (Vector3)extents / 2f, extents);
	}
}
