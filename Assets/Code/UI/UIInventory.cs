using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIInventory : MonoBehaviour
{
	public Inventory inventory;

	public Transform backpackUiItemsRoot;
	public UIItem[] backpackUiItems;

	public Vector2Int size = new Vector2Int(6, 4);

	public Item testItem;

	public void Init(Inventory inventory)
	{
		if (inventory == null)
			return;
		this.inventory = inventory;

		backpackUiItems = backpackUiItemsRoot.GetComponentsInChildren<UIItem>();
		foreach (UIItem slot in backpackUiItems)
		{
			slot.SetItem(testItem);
		}
	}
}
