using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Used for displaying private info that normally wouldn't be useful
public partial class World : MonoBehaviour
{
	public class WorldEditorInfo
	{
		public static int GetChunkCount(World worldIn)
		{
			return worldIn.chunks.Count;
		}

		public static Dictionary<Chunk.GenStage, ChunkGenerator> GetChunkGenerators(World worldIn)
		{
			return worldIn.chunkGenerators;
		}
	}
}