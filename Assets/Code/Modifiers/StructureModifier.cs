﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StructureModifier : Modifier
{
	public class StructureRoom
	{
		public Bounds innerBounds;
		public Bounds outerBounds;
		public Bounds lightBounds;
		public bool lightOff;

		public StructureRoom(Bounds bounds)
		{
			innerBounds = bounds;
			outerBounds = new Bounds(bounds.center, bounds.size + Vector3.one * 2);
		}
	}
	public List<StructureRoom> rooms;
	private int maxRoomCount = 10;
	public float fillPercent = 0;
	public Vector3Int lastRoomPos;

	public Block wallBlock = BlockList.CONCRETE;
	public Block lightBlock = BlockList.LIGHT;
	public Block floorBlock = BlockList.CARPET;
	public Block ceilingBlock = BlockList.CEILING;

	public Mask mask = new Mask() { fill = false };

	private readonly Vector3Int[] DIRS = new Vector3Int[] { Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };

	private readonly Vector3Int[] DIRS_FROM_LEFT = new Vector3Int[] { Vector3Int.left, Vector3Int.forward, Vector3Int.back };
	private readonly Vector3Int[] DIRS_FROM_RIGHT = new Vector3Int[] { Vector3Int.right, Vector3Int.forward, Vector3Int.back };
	private readonly Vector3Int[] DIRS_FROM_FORWARD = new Vector3Int[] { Vector3Int.left, Vector3Int.right, Vector3Int.forward };
	private readonly Vector3Int[] DIRS_FROM_BACK = new Vector3Int[] { Vector3Int.left, Vector3Int.right, Vector3Int.back };

	// TODO: Strength as chance to exceed 0.5
	public StructureModifier(int roomCount /*Block wallBlock, Block floorBlock, Block ceilingBlock*/)
	{
		maxRoomCount = roomCount;
		rooms = new List<StructureRoom>();
		//this.wallBlock = wallBlock;
		//this.floorBlock = floorBlock;
		//this.ceilingBlock = ceilingBlock;

		stage = ModifierStage.Terrain;
	}

	public override bool Init()
	{
		base.Init();

		MakeRooms();

		return true;
	}

	protected void MakeRooms()
	{
		Vector3Int pos = Vector3Int.zero;
		Vector3Int size = new Vector3Int(8, 12, 8);
		Vector3Int direction = RandomDirection(false);
		Vector3Int prevPos = pos;

		int i;
		for (i = 0; i < maxRoomCount; i++)
		{
			prevPos = pos;

			if (i > 0)
			{
				Vector3Int newsize = new Vector3Int(2 * Random.Range(2, 11), Random.Range(3, 9), 2 * Random.Range(2, 11));
				pos += Utils.Scale(direction, new Vector3Int(Mathf.CeilToInt((size.x + newsize.x) / 2f), Mathf.CeilToInt((size.y + newsize.y) / 2f), Mathf.CeilToInt((size.z + newsize.z) / 2f)));
				size = newsize;

				direction = RandomDirection(true, direction);

				if (Random.Range(0, 8) == 0)
					pos += RandomSign() * Vector3Int.up;
			}

			Bounds bounds = new Bounds(pos + Vector3Int.up * Mathf.CeilToInt(size.y / 2f), size);

			if (i > 0)
			{
				bool intersecting = false;
				for (int j = i; j >= 1; j--)
				{
					if (rooms[j - 1].lightBounds.Intersects(new Bounds(bounds.center, bounds.size * 0.99f)))
					{
						intersecting = true;
						break;
					}
				}

				if (intersecting)
					break;

				// Check if any part of room is outside of world bounds (- 2 for the walls on both sides)
				Bounds worldBounds = new Bounds(Vector3Int.zero, Vector3.one * (World.GetWorldSize() - 2));
				if (!worldBounds.Contains(bounds.min) || !worldBounds.Contains(bounds.max))
				{
					break;
				}
			}

			StructureRoom room = new StructureRoom(bounds);

			if (i == 0)
			{
				Bounds lightBounds = new Bounds(pos + Vector3Int.up * Mathf.CeilToInt(size.y / 2f) + Vector3Int.up, new Vector3Int(6, size.y, 6));
				room.lightBounds = lightBounds;
			}
			else if (i % 1 == 0)
			{
				int lightSize = Random.value < 0.4 ? 4 : 2;

				Bounds lightBounds = new Bounds(pos + Vector3Int.up * Mathf.CeilToInt(size.y / 2f) + Vector3Int.up/* + new Vector3(0.5f, 0, 0.5f)*/, new Vector3Int(lightSize, size.y, lightSize));
				room.lightBounds = lightBounds;

				if (Random.value < 0.1f)
					room.lightOff = true;
			}

			rooms.Add(room);
		}

		lastRoomPos = prevPos;
		fillPercent = (float)i / maxRoomCount;

		Debug.Log(i + " rooms created, " + (maxRoomCount - i) + " short. Fill percent: " + fillPercent);
	}

	public override void ApplyModifier(Chunk chunk)
	{
		if (!active)
			return;

		BlockPosAction toApply = CheckRooms;

		ApplyToAll(toApply, chunk, chunk.position, chunk.position + Vector3Int.one * (World.GetChunkSize() - 1));
	}

	protected virtual bool CheckRooms(Vector3Int pos, Chunk chunk)
	{
		Vector3 checkPos = pos + Vector3.one / 2f;

		foreach (StructureRoom room in rooms)
		{
			int checkBlock = World.GetBlock(pos.x, pos.y, pos.z).GetBlockType();

			if (!room.outerBounds.Contains(checkPos))
				continue;

			else if (World.GetBlock(pos.x, pos.y, pos.z).IsFilled()/* && checkBlock != lightBlock.GetBlockType()*/)
			{
				if (room.innerBounds.Contains(checkPos))
				{
					if (checkBlock != lightBlock.GetBlockType())
						World.SetBlock(pos.x, pos.y, pos.z, BlockList.EMPTY);
				}
				else if (room.lightBounds.Contains(checkPos))
				{
					if (checkBlock != wallBlock.GetBlockType() && checkBlock != lightBlock.GetBlockType())
					{
						World.SetBlock(pos.x, pos.y, pos.z, lightBlock);

						if (/*SeedlessRandom.NextFloat() < 0.6f && */!room.lightOff)
						{
							LightSource.ColorFalloff color = SeedlessRandom.NextFloat() < 0.8 ? LightSource.colorWhite : (SeedlessRandom.NextFloat() < 0.8 ? LightSource.colorOrange : LightSource.colorGold);
							chunk.AddLight(new LightSource(pos, color));
						}
					}
				}
				else if (room.innerBounds.Contains(checkPos + Vector3Int.up))
				{
					if (checkBlock != lightBlock.GetBlockType())
						World.SetBlock(pos.x, pos.y, pos.z, floorBlock);
				}
				else if (room.innerBounds.Contains(checkPos + Vector3Int.down))
				{
					if (checkBlock != lightBlock.GetBlockType())
						World.SetBlock(pos.x, pos.y, pos.z, ceilingBlock);
				}
				else if (checkBlock != floorBlock.GetBlockType() && checkBlock != ceilingBlock.GetBlockType())
				{
					if (checkBlock == lightBlock.GetBlockType())
						chunk.RemoveLightAt(pos);

					World.SetBlock(pos.x, pos.y, pos.z, wallBlock);
				}
			}
		}

		return true;
	}

	protected virtual Vector3 WarpPosition(Vector3 pos)
	{
		return pos;
	}

	protected int RandomSign()
	{
		int index = Random.Range(0, 2) * 2 - 1;

		return index;
	}

	protected Vector3Int RandomDirection(bool exclude, Vector3Int toExclude = new Vector3Int())
	{
		if (!exclude)
		{
			int index = Random.Range(0, 4);

			return DIRS[index];
		}
		else
		{
			int index = Random.Range(0, 2);

			if (toExclude == Vector3Int.left)
				return DIRS_FROM_LEFT[index];
			else if (toExclude == Vector3Int.right)
				return DIRS_FROM_RIGHT[index];
			else if (toExclude == Vector3Int.forward)
				return DIRS_FROM_FORWARD[index];
			else if (toExclude == Vector3Int.back)
				return DIRS_FROM_BACK[index];
			else
				return Vector3Int.zero;
		}
	}

	public void DrawGizmo()
	{
		if (rooms == null)
			return;

		Gizmos.color = Color.white;

		foreach (StructureRoom room in rooms)
		{
			Gizmos.DrawWireCube(room.innerBounds.center, room.innerBounds.size);
		}
	}
}
