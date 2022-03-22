using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Specialized;

public static class BlockList
{
	public static Block EMPTY = new Block(false, false, false, 0);
	public static Block FILLED = new Block(true, true, true, 0);
	public static Block BORDER = new Block(false, true, false, 0);
}
