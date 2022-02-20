using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
	public Vector2Int backpackSize = new Vector2Int(1, 1);
	public ItemSlot[,] backpackSlots;

	public ItemSlot holsterL = new ItemSlot() { heldItem = null, size = 2 };
	public ItemSlot holsterR = new ItemSlot() { heldItem = null, size = 2 };

	public Inventory(Vector2Int size)
	{
		backpackSize = size;

		backpackSlots = new ItemSlot[backpackSize.x, backpackSize.y];
		for (int x = 0; x < backpackSize.x; x++)
		{
			for (int y = 0; y < backpackSize.y; y++)
			{
				backpackSlots[x, y] = new ItemSlot() { heldItem = null, size = 1 };
			}
		}
	}

	public struct ItemSlot
	{
		public Item heldItem;
		public int size;
	}
}
