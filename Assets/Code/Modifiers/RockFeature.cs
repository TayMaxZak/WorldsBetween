using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RockFeature : Modifier
{
	public float chance = 1;
	public int count = 0;

	public Block toPlace = BlockList.EMPTY;

	public Mask mask;

	public RockFeature(Block toPlace, Mask mask, float chance)
	{
		this.toPlace = toPlace;
		this.chance = chance;
		this.mask = mask;

		stage = ModifierStage.Decorator;
	}

	public override void ApplyModifier(Chunk chunk)
	{
		if (!active)
			return;

		BlockPosAction toApply = ApplyDecorator;

		if (count <= 0)
			ApplyToAll(toApply, chunk, chunk.position, chunk.position + Vector3Int.one * (World.GetChunkSize() - 1));
		else
			RandomlyApplyToAll(toApply, chunk, chunk.position, chunk.position + Vector3Int.one * (World.GetChunkSize() - 1));
	}

	protected void RandomlyApplyToAll(BlockPosAction action, Chunk chunk, Vector3Int min, Vector3Int max)
	{
		for (int x = min.x; x <= max.x; x++)
		{
			for (int y = min.y; y <= max.y; y++)
			{
				for (int z = min.z; z <= max.z; z++)
				{
					action(new Vector3Int(x, y, z), chunk);
				}
			}
		}

		//int counter = count;
		//int failsafe = 0;

		//while (counter > 0 && failsafe < 4096 * 4)
		//{
		//	failsafe++;



		//	//if (action(new Vector3Int(
		//	//		SeedlessRandom.NextIntInRange(min.x, max.x + 1),
		//	//		SeedlessRandom.NextIntInRange(min.y, max.y + 1),
		//	//		SeedlessRandom.NextIntInRange(min.z, max.z + 1)
		//	//	), chunk
		//	//))
		//	//{
		//	//	counter--;
		//	//}
		//}
	}

	protected virtual bool ApplyDecorator(Vector3Int pos, Chunk chunk)
	{
		float compare = SeedlessRandom.NextFloat();
		bool pass = compare < chance;

		if (!pass)
			return false;

		Block block;
		bool grassBelow = World.GetBlock(pos + Vector3Int.down).GetBlockType() == (int)BlockList.BlockType.DirtGrass;

		bool rockBelow = World.GetBlock(pos + Vector3Int.down).GetBlockType() == (int)BlockList.BlockType.Rock;

		if (!grassBelow && !rockBelow)
			return false;
		//if ((!rockBelow && !rockAbove) || (rockBelow && rockAbove))
		//	return false;

		bool rockNear = World.GetBlock(pos + Vector3Int.left).GetBlockType() == (int)BlockList.BlockType.Rock
				|| World.GetBlock(pos + Vector3Int.right).GetBlockType() == (int)BlockList.BlockType.Rock
				|| World.GetBlock(pos + Vector3Int.forward).GetBlockType() == (int)BlockList.BlockType.Rock
				|| World.GetBlock(pos + Vector3Int.back).GetBlockType() == (int)BlockList.BlockType.Rock;

		bool grassNear = World.GetBlock(pos + Vector3Int.left).GetBlockType() == (int)BlockList.BlockType.DirtGrass
				|| World.GetBlock(pos + Vector3Int.right).GetBlockType() == (int)BlockList.BlockType.DirtGrass
				|| World.GetBlock(pos + Vector3Int.forward).GetBlockType() == (int)BlockList.BlockType.DirtGrass
				|| World.GetBlock(pos + Vector3Int.back).GetBlockType() == (int)BlockList.BlockType.DirtGrass;

		bool pass2 = (rockNear && grassBelow) || (grassNear && rockBelow)/* || compare < chance / 2*/;

		if (!pass2)
			return false;

		block = toPlace;

		block.SetNeedsMesh(true);

		if (mask.fill && !World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);

		if (mask.replace && World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);

		return true;
	}
}
