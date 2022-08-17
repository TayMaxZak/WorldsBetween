using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TreeFeature : Modifier
{
	public float chance = 1;
	public int count = 0;

	public Block trunk = BlockList.MUD;
	public Block leaves = BlockList.GRASS;

	public Mask mask;

	public TreeFeature(Block trunk, Block leaves, Mask mask, float chance)
	{
		this.trunk = trunk;
		this.leaves = leaves;
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

		bool grassBelow = World.GetBlock(pos + Vector3Int.down).GetBlockType() == (int)BlockList.BlockType.DirtGrass;

		bool grassNear = World.GetBlock(pos + Vector3Int.down + Vector3Int.left).GetBlockType() == (int)BlockList.BlockType.DirtGrass
						|| World.GetBlock(pos + Vector3Int.down + Vector3Int.right).GetBlockType() == (int)BlockList.BlockType.DirtGrass
						|| World.GetBlock(pos + Vector3Int.down + Vector3Int.forward).GetBlockType() == (int)BlockList.BlockType.DirtGrass
						|| World.GetBlock(pos + Vector3Int.down + Vector3Int.back).GetBlockType() == (int)BlockList.BlockType.DirtGrass;

		if (!grassBelow || !grassNear)
			return false;

		int trunkHeight = SeedlessRandom.NextIntInRange(10, 15);
		for (int y = 0; y < trunkHeight; y++)
			SetTrunk(pos + Vector3Int.up * y);

		Vector3Int canopyPos = pos + Vector3Int.up * trunkHeight;
		for (int c = 0; c < trunkHeight; c++)
			SetTrunk(canopyPos + SeedlessRandom.RandomPoint(trunkHeight / 8));
		for (int c = 0; c < trunkHeight * trunkHeight; c++)
			SetLeaves(canopyPos + SeedlessRandom.RandomPoint(trunkHeight / 3 - 1));

		return true;
	}

	private void SetTrunk(Vector3Int pos)
	{
		Block block = trunk;

		block.SetNeedsMesh(true);

		if (mask.fill && !World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);

		if (mask.replace && World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);
	}

	private void SetLeaves(Vector3Int pos)
	{
		Block block = leaves;

		block.SetNeedsMesh(true);

		if (mask.fill && !World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);

		if (mask.replace && World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);
	}
}
