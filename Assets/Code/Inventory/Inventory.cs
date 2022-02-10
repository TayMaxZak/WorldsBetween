using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
	public Vector2Int backpackSize = new Vector2Int(1, 1);
	public InventorySlot[,] backpackSlots;

	public InventorySlot holsterL = new InventorySlot() { heldItem = null, size = 2 };
	public InventorySlot holsterR = new InventorySlot() { heldItem = null, size = 2 };

	public struct InventorySlot
	{
		public Item heldItem;
		public int size;
	}
}
