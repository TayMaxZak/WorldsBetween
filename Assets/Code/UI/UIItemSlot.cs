using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[SelectionBase]
public class UIItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public enum SlotType
	{
		Backpack,
		HolsterL,
		HolsterR,
		Stock
	}
	public SlotType slotType;

	[HideInInspector]
	public RectTransform rectTransform;
	[HideInInspector]
	public UIInventory container;

	public Vector2Int xyCoord;

	public UIItem occupied;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!occupied && slotType != SlotType.Stock)
			container.SetHoveredSlot(this);
	}

	public void OnPointerExit(PointerEventData eventData)
	{

	}
}
