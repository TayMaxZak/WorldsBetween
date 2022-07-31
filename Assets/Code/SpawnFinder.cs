﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[System.Serializable]
public class SpawnFinder
{
	private Vector3Int pos;
	public Vector3Int extents = Vector3Int.one;

	private bool isBusy;

	private int attemptsLeft;
	private int attemptsMax = 100;

	protected delegate void BlockPosAction(Vector3Int pos);

	private int airCount = 0;
	private int solidCount = 0;

	private int minAirCount = 9;
	private int minSolidCount = 9;

	private bool foundPlayerPos = false;
	private Vector3 playerPos = Vector3.zero;

	private bool success = false;

	private System.Random random;

	public void Reset()
	{
		random = new System.Random(0);

		pos = Vector3Int.zero;

		isBusy = false;

		attemptsLeft = attemptsMax;

		airCount = 0;
		solidCount = 0;

		foundPlayerPos = false;
		playerPos = Vector3.zero;

		success = false;
	}

	public void Move()
	{
		airCount = 0;
		solidCount = 0;

		foundPlayerPos = false;
		playerPos = Vector3.zero;

		// Move to a new location in the world near-ish the origin
		Vector3Int testBounds = new Vector3Int(16, 16, 16);
		pos = new Vector3Int(
			World.GetPointA().x + (int)(testBounds.x * (((float)random.NextDouble() * 2) - 1)),
			World.GetPointA().y + (int)(testBounds.y * (((float)random.NextDouble() * 2) - 1)),
			World.GetPointA().z + (int)(testBounds.z * (((float)random.NextDouble() * 2) - 1))
		);
	}

	public async void Tick()
	{
		if (success)
			return;
		else
			Move();

		BlockPosAction act = Scan;

		isBusy = true;

		await ScanAll(act, pos, pos + extents);

		await CheckConditions();

		attemptsLeft--;

		isBusy = false;
	}

	protected virtual void Scan(Vector3Int pos)
	{
		if (!World.Contains(pos))
			return;

		bool notWaterOrNoChoice = pos.y > World.GetWaterHeight() || attemptsLeft <= 0;

		if (notWaterOrNoChoice && !World.GetBlock(pos).IsRigid())
		{
			airCount++;

			// Check playerPos conditions
			if (!foundPlayerPos)
			{
				if (!World.GetBlock(pos + Vector3Int.up).IsRigid() && World.GetBlock(pos + Vector3Int.down).IsRigid())
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

		await Task.Delay(1);
	}

	protected async Task CheckConditions()
	{
		if (airCount < minAirCount || solidCount < minSolidCount)
			return;

		if (!foundPlayerPos)
			return;

		success = true;

		await Task.Delay(1);

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
