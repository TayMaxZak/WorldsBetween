using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;

[System.Serializable]
public class FakeChunk : Chunk
{
	public override void Init(int chunkSize, int scaleFactor)
	{
		base.Init(chunkSize, scaleFactor);

		chunkType = ChunkType.Far;
	}

	public override void CacheDataFromBlocks()
	{
		int airCount = 0;

		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
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
