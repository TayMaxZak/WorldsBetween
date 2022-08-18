using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BumpFeature : Modifier
{
	public int count = 0;
	public int radius = 1;
	public int fill = 10;

	public Block toPlace = BlockList.ROCK;
	public Block placeOn = BlockList.ROCK;

	public Mask mask;

	public BumpFeature(Block toPlace, Block placeOn, Mask mask, int count, int radius, int fill)
	{
		this.toPlace = toPlace;
		this.placeOn = placeOn;
		this.count = count;
		this.radius = radius;
		this.fill = fill;
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
		int counter = count;
		//int failsafe = 0;

		while (counter > 0 /*&& failsafe < 4096 * 4*/)
		{
			counter--;

			action(new Vector3Int(
					SeedlessRandom.NextIntInRange(min.x, max.x + 1),
					SeedlessRandom.NextIntInRange(min.y, max.y + 1),
					SeedlessRandom.NextIntInRange(min.z, max.z + 1)
				), chunk
			);
		}
	}

	protected virtual bool ApplyDecorator(Vector3Int pos, Chunk chunk)
	{
		if (/*mask.fill && */World.GetBlock(pos).IsFilled())
			return false;

		//if (mask.replace && !World.GetBlock(pos).IsFilled())
		//	return false;

		bool placedNear = World.GetBlock(pos + Vector3Int.left).GetBlockType() == placeOn.GetBlockType()
				|| World.GetBlock(pos + Vector3Int.right).GetBlockType() == placeOn.GetBlockType()
				|| World.GetBlock(pos + Vector3Int.forward).GetBlockType() == placeOn.GetBlockType()
				|| World.GetBlock(pos + Vector3Int.back).GetBlockType() == placeOn.GetBlockType()
				|| World.GetBlock(pos + Vector3Int.up).GetBlockType() == placeOn.GetBlockType()
				|| World.GetBlock(pos + Vector3Int.down).GetBlockType() == placeOn.GetBlockType();

		if (!placedNear)
			return false;
		
		for (int i = 0; i < fill; i++)
			PlaceBlock(pos + SeedlessRandom.RandomPoint(radius));

		return true;
	}

	private void PlaceBlock(Vector3Int pos)
	{

		Block block = toPlace;

		block.SetNeedsMesh(true);

		if (mask.fill && !World.GetBlock(pos).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);

		if (mask.replace && World.GetBlock(pos).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);
	}
}
