using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIInventory : MonoBehaviour
{
	public Inventory inventory;

	public Vector2Int size = new Vector2Int(6, 4);

	[Header("")]
	[SerializeField]
	private Transform uiItemsRoot;
	private UIItem[] uiItems;

	[Header("")]
	[SerializeField]
	private Transform uiSlotsRoot;
	private UIItemSlot[,] backpackUiSlots;
	private UIItemSlot holsterLUiSlot;
	private UIItemSlot holsterRUiSlot;

	[Header("")]
	public UIItemSlot hoveredSlot;
	public UIItem dragDropItem;
	public Item testItem;

	public void Init(Inventory inventory)
	{
		if (inventory == null)
			return;
		this.inventory = inventory;

		// Moveable items
		uiItems = uiItemsRoot.GetComponentsInChildren<UIItem>();
		foreach (UIItem item in uiItems)
		{
			item.container = this;

			item.SetItem(testItem);
		}

		// Slots for drag and dropping
		UIItemSlot[,] backpackUiSlots = new UIItemSlot[inventory.backpackSize.x, inventory.backpackSize.y];
		UIItemSlot[] slots = uiSlotsRoot.GetComponentsInChildren<UIItemSlot>();

		foreach (UIItemSlot slot in slots)
		{
			slot.container = this;

			// Link matching slots
			if (slot.slotType == UIItemSlot.SlotType.Backpack)
			{
				Vector2Int coord = slot.xyCoord;

				backpackUiSlots[coord.x, coord.y] = slot;
				slot.itemSlot = inventory.backpackSlots[coord.x, coord.y];
			}
			else if (slot.slotType == UIItemSlot.SlotType.HolsterL)
			{
				holsterLUiSlot = slot;
			}
			else if (slot.slotType == UIItemSlot.SlotType.HolsterR)
			{
				holsterLUiSlot = slot;
			}
		}
	}

	private void Update()
	{
		if (!dragDropItem)
			return;

		//if (hoveredSlot)
		//{
		//	dragDropItem.transform.SetParent(hoveredSlot.transform, false);
		//	dragDropItem.rectTransform.anchoredPosition = Vector2.zero;
		//	dragDropItem.transform.SetParent(uiItemsRoot, true);
		//}

		dragDropItem.rectTransform.position = Input.mousePosition;

		if (Input.GetMouseButtonUp(0))
			DropDragDropItem();
	}

	public void SetHoveredSlot(UIItemSlot currentSlot)
	{
		hoveredSlot = currentSlot;
	}

	public void PickUpDragDropItem(UIItem currentItem)
	{
		dragDropItem = currentItem;
	}

	private void DropDragDropItem()
	{
		if (!hoveredSlot)
			return;

		dragDropItem.DropItem();

		dragDropItem.transform.SetParent(hoveredSlot.transform, false);
		dragDropItem.rectTransform.anchoredPosition = Vector2.zero;
		dragDropItem.transform.SetParent(uiItemsRoot, true);

		dragDropItem = null;
	}
}
