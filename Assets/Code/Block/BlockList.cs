﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Specialized;

public static class BlockList
{
	public enum BlockType
	{
		Utility,
		Natural,
		Artifical
	}

	public static Block EMPTY = new Block(false, false, false, BlockType.Utility);
	public static Block BORDER = new Block(false, true, false, BlockType.Utility);

	public static Block NATURAL = new Block(true, true, true, BlockType.Natural);
	public static Block ARTIFICAL = new Block(true, true, true, BlockType.Artifical);

}
