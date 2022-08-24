using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StackEdgeFeature : Modifier
{
	public float chance = 1;
	public int count = 0;

	public Block toPlace = BlockList.EMPTY;
	public Block placeOn = BlockList.EMPTY;

	public Mask mask;

	public StackEdgeFeature(Block toPlace, Block placeOn, Mask mask, float chance)
	{
		this.toPlace = toPlace;
		this.placeOn = placeOn;
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
	}

	protected virtual bool ApplyDecorator(Vector3Int pos, Chunk chunk)
	{
		float compare = SeedlessRandom.NextFloat();
		bool pass = compare < chance;

		if (!pass)
			return false;

		Block block;
		bool floorBelow = World.GetBlock(pos + Vector3Int.down).GetBlockType() == placeOn.GetBlockType();

		bool placedBelow = World.GetBlock(pos + Vector3Int.down).GetBlockType() == toPlace.GetBlockType();

		if (!floorBelow && !placedBelow)
			return false;
		//if ((!rockBelow && !rockAbove) || (rockBelow && rockAbove))
		//	return false;

		bool placedNear = World.GetBlock(pos + Vector3Int.left).GetBlockType() == toPlace.GetBlockType()
				|| World.GetBlock(pos + Vector3Int.right).GetBlockType() == toPlace.GetBlockType()
				|| World.GetBlock(pos + Vector3Int.forward).GetBlockType() == toPlace.GetBlockType()
				|| World.GetBlock(pos + Vector3Int.back).GetBlockType() == toPlace.GetBlockType();

		bool floorNear = World.GetBlock(pos + Vector3Int.left).GetBlockType() == placeOn.GetBlockType()
				|| World.GetBlock(pos + Vector3Int.right).GetBlockType() == placeOn.GetBlockType()
				|| World.GetBlock(pos + Vector3Int.forward).GetBlockType() == placeOn.GetBlockType()
				|| World.GetBlock(pos + Vector3Int.back).GetBlockType() == placeOn.GetBlockType();

		bool pass2 = (placedNear && floorBelow) || (floorNear && placedBelow);

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
