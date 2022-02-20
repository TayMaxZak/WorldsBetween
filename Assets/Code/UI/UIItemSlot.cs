using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[SelectionBase]
public class UIItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[HideInInspector]
	public RectTransform rectTransform;
	[HideInInspector]
	public UIInventory container;

	public Vector2Int xyCoord;

	public Inventory.ItemSlot itemSlot;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		container.SetHoveredSlot(this);
	}

	public void OnPointerExit(PointerEventData eventData)
	{

	}
}
