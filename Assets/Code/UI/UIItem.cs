using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[SelectionBase]
public class UIItem : MonoBehaviour, IPointerDownHandler
{
	[HideInInspector]
	public RectTransform rectTransform;
	[HideInInspector]
	public UIInventory container;

	[SerializeField]
	private Button button;
	[SerializeField]
	private Image itemIcon;

	[SerializeField]
	private Vector2 baseDimensions = new Vector2(30, 30);
	[SerializeField]
	private string toolTip;

	[HideInInspector]
	public UIItemSlot occupiedSlot;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	public void SetItem(Item item)
	{
		toolTip = item.label;

		itemIcon.sprite = item.icon;
		itemIcon.color = item.uiTint;

		rectTransform.sizeDelta = Utils.Scale(baseDimensions, (Vector2)item.inventorySize);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		container.PickUpDragDropItem(this);

		button.targetGraphic.raycastTarget = false;
	}

	public void DropItem()
	{
		button.targetGraphic.raycastTarget = true;
	}
}
