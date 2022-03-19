using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
	// Number of 1-size slots in the backpack grid
	public Vector2Int backpackSize = new Vector2Int(1, 1);
	// All of the items and associated transforms in the inventory
	public List<InventoryItem> invItems;

	public Inventory(Vector2Int size)
	{
		backpackSize = size;
		invItems = new List<InventoryItem>();
	}

	public bool Add(Item item, Vector2Int pos)
	{
		// Check bounds
		if (pos.x < 0 || pos.x >= backpackSize.x)
			return false;
		else if (pos.y < 0 || pos.y >= backpackSize.y)
			return false;

		// Slot taken
		foreach (InventoryItem i in invItems)
			if (i.pos == pos)
				return false;

		invItems.Add(new InventoryItem(item, pos));

		return true;
	}

	// TODO: Change to iterator
	public List<InventoryItem> GetItems()
	{
		return invItems;
	}

	// TODO: Change to iterator
	public Item GetNth(int n)
	{
		if (n < invItems.Count)
			return invItems[n].item;
		else
			return null;
	}

	public override string ToString()
	{
		string output = "";
		for (int i = 0; i < invItems.Count; i++)
		{
			output += invItems[i].item.label + " ";
		}
		return output;
	}
}

public class InventoryItem
{
	public Item item;
	public Vector2Int pos;

	public InventoryItem(Item item, Vector2Int pos)
	{
		this.item = item;
		this.pos = pos;
	}
}
