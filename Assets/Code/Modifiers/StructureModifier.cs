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
		public bool lightOff;
		public RoomData genData;

		public StructureRoom(Bounds bounds)
		{
			innerBounds = bounds;
			outerBounds = new Bounds(bounds.center, bounds.size + Vector3.one * 2);
		}
	}

	public struct RoomData
	{
		// First room of structure
		public bool starter;
		// Center of room. May be adjusted by 0.5 for odd-sized rooms
		public Vector3Int pos;
		// Total width, height, and length of room
		public Vector3Int size;
		// Direction from which this room was started. Don't backtrack
		public Vector3Int returnDirection;
		// Override random direction picking
		public bool forceDirection;

		public int debugIndex;
		public Color debugColor;
	}
	public List<StructureRoom> rooms;

	private int maxRoomCount = 10;
	private int actualRoomCount;
	[SerializeField]
	private float fillPercent = 0;
	public StructureRoom startRoom;
	public StructureRoom furthestRoom;
	public StructureRoom encounterRoom;

	public Block wallBlock = BlockList.CONCRETE;
	public Block lightBlock = BlockList.LIGHT;
	public Block floorBlock = BlockList.CARPET;
	public Block ceilingBlock = BlockList.CEILING;

	public Mask mask = new Mask() { fill = false };

	private readonly Vector3Int[] DIRS = new Vector3Int[] { Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };

	private readonly Vector3Int[] DIRS_FROM_LEFT = new Vector3Int[] { Vector3Int.right, Vector3Int.forward, Vector3Int.back };
	private readonly Vector3Int[] DIRS_FROM_RIGHT = new Vector3Int[] { Vector3Int.left, Vector3Int.forward, Vector3Int.back };
	private readonly Vector3Int[] DIRS_FROM_FORWARD = new Vector3Int[] { Vector3Int.left, Vector3Int.right, Vector3Int.back };
	private readonly Vector3Int[] DIRS_FROM_BACK = new Vector3Int[] { Vector3Int.left, Vector3Int.right, Vector3Int.forward };

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
		startRoom = MakeRoom(new RoomData() { starter = true, returnDirection = RandomDirection(false), forceDirection = true });
		furthestRoom = startRoom;

		startRoom.genData.debugColor = Color.cyan;
		FinishRoom(startRoom);
		RecursiveRoom(startRoom);

		if (actualRoomCount < maxRoomCount)
		{
			startRoom.genData = new RoomData()
			{
				starter = startRoom.genData.starter,
				pos = startRoom.genData.pos,
				size = startRoom.genData.size,
				returnDirection = -startRoom.genData.returnDirection,
				forceDirection = true,

				debugIndex = startRoom.genData.debugIndex,
				debugColor = Utils.colorOrange
			};
			RecursiveRoom(startRoom);
		}
		fillPercent = (float)actualRoomCount / maxRoomCount;

		FindEncounterRoom();
	}

	protected void FinishRoom(StructureRoom room)
	{
		actualRoomCount++;
		rooms.Add(room);

		if (Utils.DistManhattan(room.genData.pos) > Utils.DistManhattan(furthestRoom.genData.pos))
			furthestRoom = room;
	}

	protected void FindEncounterRoom()
	{
		float targetDistance = Utils.DistManhattan(furthestRoom.genData.pos) * 0.6f;
		encounterRoom = startRoom;

		foreach (StructureRoom room in rooms)
		{
			if (Mathf.Abs(Utils.DistManhattan(room.genData.pos) - targetDistance) < Mathf.Abs(Utils.DistManhattan(encounterRoom.genData.pos) - targetDistance))
				encounterRoom = room;
		}
	}

	protected void RecursiveRoom(StructureRoom prevRoom)
	{
		StructureRoom newRoom = MakeRoom(prevRoom.genData);

		if (newRoom == null)
		{
			// Try again once
			newRoom = MakeRoom(prevRoom.genData);
			// Still failed
			if (newRoom == null)
				return;
		}

		FinishRoom(newRoom);

		if (actualRoomCount < maxRoomCount)
			RecursiveRoom(newRoom);
		if (actualRoomCount < maxRoomCount && Random.value < 0.25f)
			RecursiveRoom(newRoom);
	}

	protected StructureRoom MakeRoom(RoomData prevRoom)
	{
		Vector3Int newPos;
		Vector3Int newSize;
		Vector3Int offsetDirection;
		Vector3Int offset = Vector3Int.up;

		if (prevRoom.starter)
		{
			newSize = new Vector3Int(8, 12, 8);

			offsetDirection = -prevRoom.returnDirection;

			newPos = Vector3Int.zero;
		}
		else
		{
			newSize = new Vector3Int(2 * Random.Range(2, 11), Random.Range(3, 9), 2 * Random.Range(2, 11));

			if (!prevRoom.forceDirection)
				offsetDirection = RandomDirection(true, prevRoom.returnDirection);
			else
				offsetDirection = -prevRoom.returnDirection;

			offset = Utils.Scale(offsetDirection, new Vector3Int(
				Mathf.CeilToInt((prevRoom.size.x + newSize.x) / 2f),
				Mathf.CeilToInt((prevRoom.size.y + newSize.y) / 2f),
				Mathf.CeilToInt((prevRoom.size.z + newSize.z) / 2f))
			);
			newPos = prevRoom.pos + offset;
		}

		if (Random.Range(0, 8) == 0)
			newPos += RandomSign() * Vector3Int.up;

		Bounds bounds = new Bounds(newPos + Vector3Int.up * Mathf.CeilToInt(newSize.y / 2f), newSize);

		bool intersecting = false;
		foreach (StructureRoom otherRoom in rooms)
		{
			// Results in light floating in air?
			if (otherRoom.lightBounds.Intersects(new Bounds(bounds.center, bounds.size * 0.99f)))
			{
				intersecting = true;
				break;
			}

			// Results in thin floor and other weird stuff?
			if (!otherRoom.outerBounds.Intersects(new Bounds(bounds.center, bounds.size * 0.99f)) &&
				otherRoom.outerBounds.Intersects(new Bounds(bounds.center + Vector3.up, bounds.size * 0.99f)))
			{
				intersecting = true;
				break;
			}
		}

		// Check if any part of room is outside of world bounds (- 2 for the walls on both sides)
		Bounds worldBounds = new Bounds(Vector3Int.zero, Vector3.one * (World.GetWorldSize() - 2));

		if (intersecting || !worldBounds.Contains(bounds.min) || !worldBounds.Contains(bounds.max))
		{
			Debug.DrawRay(newPos, -offset, Color.black, 30);
			return null;
		}
		else
		{
			Debug.DrawRay(newPos, -offset, prevRoom.debugColor, 30);
		}

		StructureRoom room = new StructureRoom(bounds);

		int lightSize = prevRoom.starter ? 6 : Random.value < 0.4 ? 4 : 2;

		Bounds lightBounds = new Bounds(newPos + Vector3Int.up * Mathf.CeilToInt(newSize.y / 2f) + Vector3Int.up, new Vector3Int(lightSize, newSize.y, lightSize));
		room.lightBounds = lightBounds;

		if (Random.value < 0.1f && !prevRoom.starter)
			room.lightOff = true;

		room.genData = new RoomData()
		{
			starter = false,
			pos = newPos,
			size = newSize,
			returnDirection = -offsetDirection,
			forceDirection = prevRoom.debugIndex == 0 ? true : false,
			debugIndex = prevRoom.starter ? 0 : prevRoom.debugIndex + 1,
			debugColor = prevRoom.debugColor
		};

		return room;
	}

	public override void ApplyModifier(Chunk chunk)
	{
		if (!active)
			return;

		BlockPosAction toApply = ApplyRooms;

		ApplyToAll(toApply, chunk, chunk.position, chunk.position + Vector3Int.one * (World.GetChunkSize() - 1));
	}

	protected virtual bool ApplyRooms(Vector3Int pos, Chunk chunk)
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
					if (checkBlock != lightBlock.GetBlockType() && checkBlock != floorBlock.GetBlockType())
					{
						World.SetBlock(pos.x, pos.y, pos.z, lightBlock);

						if (!room.lightOff)
						{
							BlockLight.ColorFalloff color = SeedlessRandom.NextFloat() < 0.8 ? BlockLight.colorWhite : (SeedlessRandom.NextFloat() < 0.8 ? BlockLight.colorOrange : BlockLight.colorGold);
							chunk.AddBlockLight(new BlockLight(pos, color));
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
					if (checkBlock != lightBlock.GetBlockType() && checkBlock != floorBlock.GetBlockType())
						World.SetBlock(pos.x, pos.y, pos.z, ceilingBlock);
				}
				else if (checkBlock != floorBlock.GetBlockType() && checkBlock != ceilingBlock.GetBlockType() && checkBlock != lightBlock.GetBlockType())
				{
					World.SetBlock(pos.x, pos.y, pos.z, wallBlock);
				}
			}
		}

		return true;
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

	public float GetFillPercent()
	{
		return fillPercent;
	}

	public void DrawGizmo()
	{
		if (rooms == null)
			return;

		foreach (StructureRoom room in rooms)
		{
			Gizmos.color = Color.Lerp(Color.red, Color.blue, (float)room.genData.debugIndex / (actualRoomCount / 2f));
			Gizmos.DrawWireCube(room.innerBounds.center, room.innerBounds.size);
			//Gizmos.color = Color.white;
			//Gizmos.DrawWireCube(room.lightBounds.center, room.lightBounds.size);
		}
	}
}
