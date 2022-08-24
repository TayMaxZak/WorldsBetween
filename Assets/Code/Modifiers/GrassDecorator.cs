using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GrassDecorator : Modifier
{
	public float chance = 1;
	public int count = 0;

	public Block toPlace = BlockList.EMPTY;
	public Block placeOn = BlockList.EMPTY;

	public Mask mask;

	// TODO: Strength as chance to exceed 0.5
	public GrassDecorator(Block toPlace, Block placeOn, Mask mask, float chance)
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
		bool pass = SeedlessRandom.NextFloat() < chance;

		if (!pass)
			return false;

		Block block;
		bool placeOnTop = World.GetBlock(pos + Vector3Int.down).GetBlockType() == placeOn.GetBlockType();
		//bool placeUnder = World.GetBlock(pos + Vector3Int.up).IsRigid();

		// TODO: Placeholder
		if (!placeOnTop || pos.y <= World.GetWaterHeight())
			return false;

		//if (placeOnTop && placeUnder)
		//	return false;

		block = toPlace;

		block.SetNeedsMesh(true);

		if (mask.fill && !World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);

		if (mask.replace && World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);

		return true;
	}
}
