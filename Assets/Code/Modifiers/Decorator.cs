using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Decorator : Modifier
{
	public float chance = 1;
	public int count = 0;

	public Block aboveBlock = BlockList.EMPTY;
	public Block underBlock = BlockList.EMPTY;

	public Mask mask;

	// TODO: Strength as chance to exceed 0.5
	public Decorator(Block aboveBlock, Block underBlock, Mask mask, float chance, int count)
	{
		this.aboveBlock = aboveBlock;
		this.underBlock = underBlock;
		this.chance = chance;
		this.count = count;
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
		bool pass = SeedlessRandom.NextFloat() < chance;

		if (!pass)
			return false;

		Block block;
		bool placeOnTop = World.GetBlock(pos + Vector3Int.down).GetBlockType() == (int)BlockList.BlockType.DirtGrass;
		bool placeUnder = World.GetBlock(pos + Vector3Int.up).IsRigid();

		// TODO: Placeholder
		if (!placeOnTop || pos.y <= World.GetWaterHeight())
			return false;

		if (placeOnTop && placeUnder)
			return false;

		if (placeOnTop)
			block = aboveBlock;
		else if (placeUnder)
			block = underBlock;
		else
			return false;

		block.SetNeedsMesh(true);

		if (mask.fill && !World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);

		if (mask.replace && World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);

		//if (chunk == null || chunk.chunkType != Chunk.ChunkType.Close)
		//	return;

		return true;

		//bool glowshroomColor = placeOnTop && pos.y > World.GetWaterHeight();
		//bool waterColor = pos.y <= World.GetWaterHeight();

		//// Invert some colors
		//PersistentData data = PersistentData.GetInstanceForRead();
		//if (data && (data.GetDepth() + 1) % 3 == 0)
		//	 glowshroomColor = !glowshroomColor;

		//// Create light
		//chunk.GetLights().Add(new LightSource()
		//{
		//	pos = pos,
		//	// Randomize color
		//	lightColor = waterColor ? LightSource.colorWhite : (glowshroomColor ?
		//	(SeedlessRandom.NextFloat() < 0.8 ? LightSource.colorBlue : LightSource.colorCyan) :
		//	(SeedlessRandom.NextFloat() < 0.8 ? LightSource.colorOrange : LightSource.colorGold)),
		//	brightness = SeedlessRandom.NextFloatInRange((2 / 3f), (4 / 3f)) * (glowshroomColor ? 1 : 2),
		//	spread = glowshroomColor ? 1 : 1,
		//	noise = glowshroomColor ? 0 : 0
		//});
	}
}
