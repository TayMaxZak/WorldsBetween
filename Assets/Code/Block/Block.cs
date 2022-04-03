using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Specialized;

public class Block
{
	private BitVector32 bits;

	public Block()
	{
		bits = new BitVector32();
	}

	public Block(bool exists, bool opaque, bool rigid, BlockList.BlockType blockType)
	{
		bits = new BitVector32();

		bits[BlockUtils.exists] = exists ? 1 : 0;
		bits[BlockUtils.opaque] = opaque ? 1 : 0;
		bits[BlockUtils.rigid] = rigid ? 1 : 0;
		bits[BlockUtils.needsMesh] = 0;

		bits[BlockUtils.blockType] = (int)blockType;
	}

	public bool IsFilled()
	{
		return bits[BlockUtils.exists] == 1;
	}

	public bool IsOpaque()
	{
		return bits[BlockUtils.opaque] == 1;
	}

	public bool IsRigid()
	{
		return bits[BlockUtils.rigid] == 1;
	}

	public bool GetNeedsMesh()
	{
		return bits[BlockUtils.rigid] == 1;
	}

	public void SetNeedsMesh(bool needsMesh)
	{
		bits[BlockUtils.needsMesh] = needsMesh ? 1 : 0;
	}

	public float GetHardness()
	{
		return BlockUtils.GetHardness(bits[BlockUtils.blockType]);
	}

	private static class BlockUtils
	{
		public static BitVector32.Section exists;       // 1 bit: Air/empty/no block, or not
		public static BitVector32.Section opaque;       // 1 bit: Hides mesh rendering and stops light rays, or not
		public static BitVector32.Section rigid;        // 1 bit: Stops physics objects, or not
		public static BitVector32.Section needsMesh;    // 1 bit: Flag used for building mesh faster

		public static BitVector32.Section blockType;    // 1 byte: Which type of block is this

		static BlockUtils()
		{
			exists = BitVector32.CreateSection(1);
			opaque = BitVector32.CreateSection(1, exists);
			rigid = BitVector32.CreateSection(1, opaque);
			needsMesh = BitVector32.CreateSection(1, rigid);

			blockType = BitVector32.CreateSection(8, needsMesh);
		}

		public static float GetHardness(int blockType)
		{
			if (blockType == (int)BlockList.BlockType.Artifical)
				return 0.9f;

			return 0;
		}
	}
}
