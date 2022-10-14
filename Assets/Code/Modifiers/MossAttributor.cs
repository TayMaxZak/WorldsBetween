using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MossAttributor : Modifier
{
	public float chance = 1;

	// TODO: Strength as chance to exceed 0.5
	public MossAttributor(float chance)
	{
		this.chance = chance;

		stage = ModifierStage.Decorator;
	}

	public override void ApplyModifier(Chunk chunk)
	{
		if (!active)
			return;

		BlockPosAction toApply = ApplyDecorator;

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

		BlockAttributes attr = World.GetAttributes(pos.x, pos.y, pos.z);
		attr.SetMoss(SeedlessRandom.NextFloatInRange(0.1f, 1f));

		if (attr.GetMoss() > 0.999f)
			Debug.DrawRay(new Vector3Int(pos.x, pos.y, pos.z), SeedlessRandom.RandomPoint(), Color.green, 5);

		return true;
	}
}
