using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIInventory : MonoBehaviour
{
	public Vector2Int backpackSize;

	public Vector2Int stockSize;

	[Header("")]
	[SerializeField]
	private Transform uiItemsRoot;
	private List<UIItem> uiItems;

	[Header("")]
	[SerializeField]
	private Transform uiInvSlotsRoot;
	private UIItemSlot[,] backpackUiSlots;
	private UIItemSlot holsterLUiSlot;
	private UIItemSlot holsterRUiSlot;
	[SerializeField]
	private Transform uiStkSlotsRoot;
	private UIItemSlot[,] stockUiSlots;

	[Header("")]
	public UIItemSlot hoveredSlot;
	public UIItem dragDropItem;
	public UIItem uiItemPrefab;

	public void Init(Inventory inventory, List<Item> stock)
	{
		if (inventory == null)
			return;

		// Slots for drag and dropping
		backpackUiSlots = new UIItemSlot[inventory.backpackSize.x, inventory.backpackSize.y];
		UIItemSlot[] slots = uiInvSlotsRoot.GetComponentsInChildren<UIItemSlot>();

		foreach (UIItemSlot slot in slots)
		{
			slot.container = this;

			// Link matching slots
			if (slot.slotType == UIItemSlot.SlotType.Backpack)
			{
				Vector2Int coord = slot.xyCoord;

				backpackUiSlots[coord.x, coord.y] = slot;
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

		// Slots for drag and dropping
		stockUiSlots = new UIItemSlot[stockSize.x, stockSize.y];
		slots = uiStkSlotsRoot.GetComponentsInChildren<UIItemSlot>();

		foreach (UIItemSlot slot in slots)
		{
			slot.container = this;

			// Link matching slots
			if (slot.slotType == UIItemSlot.SlotType.Stock)
			{
				Vector2Int coord = slot.xyCoord;

				stockUiSlots[coord.x, coord.y] = slot;
			}
		}


		// UI items
		uiItems = new List<UIItem>();
		for (int i = 0; i < stock.Count; i++)
		{
			// Copy of prefab
			UIItem uiItem = Instantiate(uiItemPrefab, uiItemsRoot);
			// Copy of item data
			uiItem.SetItem(Instantiate(stock[i]));

			// Moving it into place
			uiItem.SetPos(stockUiSlots[i % stockSize.x, i / stockSize.x]);

			uiItem.container = this;

			uiItems.Add(uiItem);
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
		if (currentItem.occupiedSlot)
			currentItem.occupiedSlot.occupied = null;
	}

	private void DropDragDropItem()
	{
		if (!hoveredSlot)
			return;
		dragDropItem.occupiedSlot = hoveredSlot;
		dragDropItem.occupiedSlot.occupied = dragDropItem;

		dragDropItem.DropItem();

		dragDropItem.SetPos(hoveredSlot);

		dragDropItem = null;
	}

	public Inventory GetInventory()
	{
		Inventory inventory = new Inventory(backpackSize);

		foreach (UIItem uiItem in uiItems)
		{
			if (!uiItem.occupiedSlot)
				continue;
			inventory.Add(uiItem.GetItem(), uiItem.occupiedSlot.xyCoord);
		}

		return inventory;
	}
}
