using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIInventory : MonoBehaviour
{
	public Inventory inventory;

	public Transform backpackUiSlotsRoot;
	public UIInventorySlot[,] backpackUiSlots;

	public Vector2Int size = new Vector2Int(6, 4);

	public Sprite testSprite;

	public void Init(Inventory inventory)
	{
		if (inventory == null)
			return;
		this.inventory = inventory;

		backpackUiSlots = new UIInventorySlot[inventory.backpackSize.x, inventory.backpackSize.y];

		// Sort UI slots
		UIInventorySlot[] slots = backpackUiSlotsRoot.GetComponentsInChildren<UIInventorySlot>();
		foreach (UIInventorySlot slot in slots)
		{
			Debug.Log("" + slot.xCoord + " , " + slot.yCoord);
			//backpackUiSlots[slot.xCoord, slot.yCoord] = slot;
			slot.itemIcon.sprite = testSprite;
		}
	}
}
