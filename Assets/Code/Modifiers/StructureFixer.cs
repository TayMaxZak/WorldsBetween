using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StructureFixer : Modifier
{
	private StructureModifier structure;

	// TODO: Strength as chance to exceed 0.5
	public StructureFixer(StructureModifier structure)
	{
		this.structure = structure;

		stage = ModifierStage.Decorator;
	}

	public override void ApplyModifier(Chunk chunk)
	{
		if (!active)
			return;

		BlockPosAction toApply = ApplyDecorator;

		ApplyToAll(toApply, chunk, chunk.position, chunk.position + Vector3Int.one * (World.GetChunkSize() - 1));
	}

	protected virtual bool ApplyDecorator(Vector3Int pos, Chunk chunk)
	{
		Vector3 checkPos = pos + Vector3.one * 0.5f;

		int checkBlock = World.GetBlock(pos.x, pos.y, pos.z).GetBlockType();

		if (checkBlock != structure.floorBlock.GetBlockType() && checkBlock != structure.ceilingBlock.GetBlockType())
			return false;

		foreach (StructureModifier.StructureRoom room in structure.rooms)
		{
			if (!room.outerBounds.Contains(pos + Vector3.one / 2f))
				continue;

			bool emptyNear = false;

			for (int i = -1; i <= 1; i++)
			{
				for (int k = -1; k <= 1; k++)
				{
					int nearBlock = World.GetBlock(pos.x + i, pos.y, pos.z + k).GetBlockType();

					if (nearBlock == BlockList.EMPTY.GetBlockType())
					{
						emptyNear = true;
					}
					if (emptyNear)
						break;
				}
				if (emptyNear)
					break;
			}

			if (emptyNear)
				World.SetBlock(pos, structure.wallBlock);

			break;
		}

		return true;
	}
}
