using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Specialized;

public static class BlockList
{
	public enum BlockType
	{
		Utility,

		Concrete,
		Carpet,
		Light,
		Ceiling,
		Tiles,
		Rock,
	}

	public static Block EMPTY = new Block(false, false, false, BlockType.Utility);
	public static Block BORDER = new Block(false, false, false, BlockType.Utility);
	public static Block RIGID_BORDER = new Block(false, false, true, BlockType.Utility);

	public static Block CONCRETE = new Block(true, true, true, BlockType.Concrete);
	public static Block CARPET = new Block(true, true, true, BlockType.Carpet);
	public static Block LIGHT = new Block(true, true, true, BlockType.Light);
	public static Block CEILING = new Block(true, true, true, BlockType.Ceiling);
	public static Block TILES = new Block(true, true, true, BlockType.Tiles);
	public static Block ROCK = new Block(true, true, true, BlockType.Rock);
}
