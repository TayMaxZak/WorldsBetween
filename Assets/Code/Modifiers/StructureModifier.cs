using System.Collections;
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

		public StructureRoom(Bounds bounds)
		{
			innerBounds = bounds;
			outerBounds = new Bounds(bounds.center, bounds.size + Vector3.one * 2);
		}
	}
	public List<StructureRoom> rooms;
	private int maxRoomCount = 10;
	public Vector3Int lastRoomPos;

	public Block wallBlock = BlockList.CONCRETE;
	public Block lightBlock = BlockList.LIGHT;
	public Block floorBlock = BlockList.CARPET;
	public Block ceilingBlock = BlockList.ROCK;

	public Mask mask = new Mask() { fill = false };

	private Vector3 randomOffset = Vector3.zero;

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

		SeedNoise();

		MakeRooms();

		return true;
	}

	protected void SeedNoise()
	{
		float offsetAmount = 9999;

		randomOffset = new Vector3(
			Random.value + (float)(Random.value * offsetAmount),
			Random.value + (float)(Random.value * offsetAmount),
			Random.value + (float)(Random.value * offsetAmount)
		);
	}

	protected void MakeRooms()
	{
		Vector3Int pos = Vector3Int.zero;
		Vector3Int size = new Vector3Int(8, 4, 8);


		for (int i = 0; i < maxRoomCount; i++)
		{
			if (i > 0)
			{
				Vector3Int direction = RandomDirection();
				Vector3Int newsize = new Vector3Int(Random.Range(2, 21), Random.Range(2, 9), Random.Range(2, 21));
				pos += Utils.Scale(direction, (size + newsize) / 2);
				size = newsize;
			}

			Bounds bounds = new Bounds(pos, size);
			if (i == 0)
			{
				bounds.SetMinMax(bounds.min, new Vector3(bounds.max.x, 9999, bounds.max.z));
			}
			StructureRoom room = new StructureRoom(bounds);


			if (i > 0 && i % 3 == 0)
			{
				Bounds skylightBounds = new Bounds(pos + Vector3Int.up, new Vector3Int(1, size.y, 1));
				room.lightBounds = skylightBounds;
			}

			rooms.Add(room);

			if (i == maxRoomCount - 1)
				lastRoomPos = pos;
		}
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
		Vector3 checkPos = pos + Vector3.one * 0.5f;

		foreach (StructureRoom room in rooms)
		{
			if (!room.outerBounds.Contains(checkPos))
				continue;
			else if (World.GetBlock(pos.x, pos.y, pos.z).IsFilled() && World.GetBlock(pos.x, pos.y, pos.z).GetBlockType() != lightBlock.GetBlockType())
			{
				if (room.innerBounds.Contains(checkPos))
				{
					World.SetBlock(pos.x, pos.y, pos.z, BlockList.EMPTY);
				}
				else if (room.lightBounds.Contains(checkPos))
				{
					World.SetBlock(pos.x, pos.y, pos.z, lightBlock);

					LightSource.ColorFalloff color = SeedlessRandom.NextFloat() < 0.8 ? LightSource.colorWhite : (SeedlessRandom.NextFloat() < 0.8 ? LightSource.colorOrange : LightSource.colorGold);
					chunk.GetLights().Add(new LightSource(pos, color));
				}
				else if (room.innerBounds.Contains(checkPos + Vector3Int.up))
					World.SetBlock(pos.x, pos.y, pos.z, floorBlock);
				else if (room.innerBounds.Contains(checkPos + Vector3Int.down))
					World.SetBlock(pos.x, pos.y, pos.z, ceilingBlock);
				else
					World.SetBlock(pos.x, pos.y, pos.z, wallBlock);
			}
		}

		return true;
	}

	protected virtual Vector3 WarpPosition(Vector3 pos)
	{
		return pos;
	}

	protected Vector3Int RandomDirection(Vector3Int exclude = new Vector3Int(), bool includeVertical = false)
	{
		int index = Random.Range(0, 4);

		if (index == 0)
			return Vector3Int.forward;
		else if (index == 1)
			return Vector3Int.right;
		else if (index == 2)
			return Vector3Int.back;
		else
			return Vector3Int.left;
	}
}
