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

	private Item item;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	public void SetPos(UIItemSlot slot)
	{
		Transform origParent = transform.parent;

		transform.SetParent(slot.transform, false);
		rectTransform.anchoredPosition = Vector2.zero;
		transform.SetParent(origParent, true);
	}

	public void SetItem(Item item)
	{
		this.item = item;

		toolTip = item.label;
		itemIcon.sprite = item.icon;
		itemIcon.color = item.uiTint;
	}

	public Item GetItem()
	{
		return item;
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
