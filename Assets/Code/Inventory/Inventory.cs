using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
	public Vector2Int backpackSize = new Vector2Int(1, 1);
	public InventorySlot[,] backpackSlots;

	public InventorySlot holsterL = new InventorySlot() { heldItem = null, size = 2 };
	public InventorySlot holsterR = new InventorySlot() { heldItem = null, size = 2 };

	public Inventory(Vector2Int size)
	{
		backpackSize = size;

		backpackSlots = new InventorySlot[backpackSize.x, backpackSize.y];
		for (int x = 0; x < backpackSize.x; x++)
		{
			for (int y = 0; y < backpackSize.y; y++)
			{
				backpackSlots[x, y] = new InventorySlot() { heldItem = null, size = 1 };
			}
		}
	}

	public struct InventorySlot
	{
		public Item heldItem;
		public int size;
	}
}
