using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item")]
[System.Serializable]
public class Item : ScriptableObject
{
	public enum UseHow
	{
		Main,
		Alt
	}

	public string label = "Item";

	public Sprite icon;
	public Color uiTint = Color.white;

	public Vector2Int inventorySize = new Vector2Int(1, 1);

	[HideInInspector]
	public Transform hand;

	// Called when the player clicks while this item is held
	public virtual void Use(UseHow useHow)
	{

	}

	// Called when the player starts holding this item
	public virtual void Equip(Transform hand)
	{
		this.hand = hand;
	}

	// Called when the player stops holding this item
	public virtual void Unequip()
	{
		hand = null;
	}

	// Called from the player every frame while held
	public virtual void Update()
	{

	}

	// Called occasionally while in inventory or held
	public virtual void InventoryTick(float deltaTime)
	{

	}
}
