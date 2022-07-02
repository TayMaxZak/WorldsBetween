﻿using System.Collections;
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
		for (int counter = count; counter > 0; counter--)
		{
			action(
				new Vector3Int(
					SeedlessRandom.NextIntInRange(min.x, max.x + 1),
					SeedlessRandom.NextIntInRange(min.y, max.y + 1),
					SeedlessRandom.NextIntInRange(min.z, max.z + 1)
				),
				chunk
			);
		}
	}

	protected virtual void ApplyDecorator(Vector3Int pos, Chunk chunk)
	{
		bool pass = SeedlessRandom.NextFloat() < chance;

		if (!pass)
			return;

		Block block;
		bool above = World.GetBlock(pos + Vector3Int.down).IsRigid();
		bool under = World.GetBlock(pos + Vector3Int.up).IsRigid();

		// TODO: Placeholder
		if (above)
			return;

		if (above && under)
			return;

		if (above)
			block = aboveBlock;
		else if (under)
			block = underBlock;
		else
			return;

		block.SetNeedsMesh(true);

		if (mask.fill && !World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);

		if (mask.replace && World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);

		if (chunk == null || chunk.chunkType != Chunk.ChunkType.Close)
			return;

		bool glowshroomColor = above && pos.y > World.GetWaterHeight();
		bool waterColor = pos.y <= World.GetWaterHeight();

		// Invert some colors
		PersistentData data = PersistentData.GetInstanceForRead();
		if (data && (data.GetDepth() + 1) % 3 == 0)
			 glowshroomColor = !glowshroomColor;

		// Create light
		chunk.GetLights().Add(new LightSource()
		{
			pos = pos,
			// Randomize color
			lightColor = waterColor ? LightSource.colorWhite : (glowshroomColor ?
			(SeedlessRandom.NextFloat() < 0.8 ? LightSource.colorBlue : LightSource.colorCyan) :
			(SeedlessRandom.NextFloat() < 0.8 ? LightSource.colorOrange : LightSource.colorGold)),
			brightness = SeedlessRandom.NextFloatInRange(0.67f, 1.33f) * (glowshroomColor ? 1 : 2),
			spread = glowshroomColor ? 1 : 1,
			noise = glowshroomColor ? 0 : 0
		});
	}
}
