using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
	public Vector2Int dimensions = new Vector2Int(1,1);

	public struct InventorySlot
	{
		public Item heldItem;
	}
}
