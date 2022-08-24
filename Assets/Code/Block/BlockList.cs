﻿using System.Collections;
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
		Rock,

		Mushrooms,
		Grass,
	}

	public static Block EMPTY = new Block(false, false, false, BlockType.Utility);
	public static Block BORDER = new Block(false, false, false, BlockType.Utility);

	public static Block CONCRETE = new Block(true, true, true, BlockType.Concrete);
	public static Block CARPET = new Block(true, true, true, BlockType.Carpet);
	public static Block LIGHT = new Block(true, true, true, BlockType.Light);
	public static Block ROCK = new Block(true, true, true, BlockType.Rock);

	public static Block GRASS = new Block(true, false, false, BlockType.Grass);
	public static Block MUSHROOMS = new Block(true, false, false, BlockType.Mushrooms);

}
