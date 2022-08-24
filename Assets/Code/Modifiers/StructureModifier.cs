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
	public Block floorBlock = BlockList.MUD;
	public Block ceilingBlock = BlockList.DIRTGRASS;

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
			Vector3Int newsize = new Vector3Int(Random.Range(2, 21), Random.Range(2, 9), Random.Range(2, 21));
			pos += new Vector3Int(RandomDirection(true) * Mathf.Abs(size.x + newsize.x) / 2, -1 * Mathf.Abs(size.y - newsize.y) / 2, RandomDirection(true) * Mathf.Abs(size.z + newsize.z) / 2);
			size = newsize;

			Bounds bounds = new Bounds(pos, size);

			/*if (i == 0)
			{
				bounds.SetMinMax(bounds.min, new Vector3(bounds.max.x, 9999, bounds.max.z));
			}
			else */if (i % 1 == 0)
			{
				Bounds skylightBounds = new Bounds(pos, size);
				skylightBounds.size = Vector3.one * 1;
				skylightBounds.SetMinMax(skylightBounds.min, new Vector3(skylightBounds.max.x, 9999, skylightBounds.max.z));
				rooms.Add(new StructureRoom(skylightBounds));
			}

			rooms.Add(new StructureRoom(bounds));

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
			if (room.innerBounds.Contains(checkPos))
				World.SetBlock(pos.x, pos.y, pos.z, BlockList.EMPTY);
			else if (room.outerBounds.Contains(checkPos) && World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
				World.SetBlock(pos.x, pos.y, pos.z, BlockList.CONCRETE);
		}

		return true;
	}

	protected virtual Vector3 WarpPosition(Vector3 pos)
	{
		return pos;
	}

	protected int RandomDirection(bool includeZero)
	{
		if (includeZero)
			return Random.Range(-1, 2);
		else
			return Random.Range(0, 2) * 2 - 1;
	}
}
