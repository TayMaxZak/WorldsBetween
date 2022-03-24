using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;

[System.Serializable]
public class FakeChunk : Chunk
{
	public override void Init(int chunkSize)
	{
		scaleFactor = 2;
		chunkType = ChunkType.Far;

		base.Init(chunkSize);
	}

	public override void CacheDataFromBlocks()
	{
		int airCount = 0;

		for (byte x = 0; x < chunkSizeBlocks; x++)
		{
			for (byte y = 0; y < chunkSizeBlocks; y++)
			{
				for (byte z = 0; z < chunkSizeBlocks; z++)
				{
					// Only care if this block is an air block
					if (GetBlock(x,y,z).IsFilled())
						continue;

					airCount++;

					// Handle adjacent blocks for this block
					FlagAdjacentsAsMaybeNearAir(x, y, z);
				}
			}
		}
	}

	//protected override void OnDone()
	//{
	//	blocks = null;
	//}
}
