using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[SelectionBase]
public class UIItem : MonoBehaviour
{
	private RectTransform rectTransform;

	[SerializeField]
	private Image itemIcon;
	[SerializeField]
	private Image itemOutline;

	[SerializeField]
	private Vector2 baseDimensions = new Vector2(30, 30);
	[SerializeField]
	private TMPro.TextMeshProUGUI label;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	public void SetItem(Item item)
	{
		label.text = item.label;

		itemIcon.sprite = item.icon;
		itemIcon.color = item.tint;

		itemOutline.sprite = item.icon;

		rectTransform.sizeDelta = Utils.Scale(baseDimensions, (Vector2)item.inventorySize);
	}
}
