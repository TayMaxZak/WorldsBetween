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

	private int counter;
	private Chunk curChunk;

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

		curChunk = chunk;

		BlockPosAction toApply = ApplyDecorator;

		counter = count;

		//if (count <= 0) // TODO
			ApplyToAll(toApply, chunk.scaleFactor, chunk.position, chunk.position + Vector3Int.one * (World.GetChunkSize() - 1));
	}

	protected virtual void ApplyDecorator(Vector3Int pos)
	{
		if (count > 0 && counter <= 0)
			return;

		bool pass = SeedlessRandom.NextFloat() < chance;

		if (!pass)
			return;

		counter--;

		Block block;
		bool above = World.GetBlock(pos + Vector3Int.down).IsRigid();
		bool under = World.GetBlock(pos + Vector3Int.up).IsRigid();

		if (above && under)
			return;

		if (above)
			block = aboveBlock;
		else if (under)
			block = underBlock;
		else
			return;

		if (mask.fill && !World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);

		if (mask.replace && World.GetBlock(pos.x, pos.y, pos.z).IsFilled())
			World.SetBlock(pos.x, pos.y, pos.z, block);

		if (curChunk == null || curChunk.chunkType != Chunk.ChunkType.Close)
			return;

		bool glowshroom = above && pos.y > World.GetWaterHeight();

		// Create light
		curChunk.GetLights().Add(new LightSource()
		{
			pos = pos,
			// Randomize color
			lightColor = glowshroom ?
			(SeedlessRandom.NextFloat() < 0.8 ? LightSource.colorOrange : LightSource.colorGold) :
			(SeedlessRandom.NextFloat() < 0.8 ? LightSource.colorBlue : LightSource.colorCyan),
			brightness = SeedlessRandom.NextFloatInRange(0.67f, 1.33f) * (glowshroom ? 0.5f : 0.5f),
			spread = glowshroom ? 1f : 0.5f,
			noise = glowshroom ? 0.67f : 0.33f
		});
	}
}
