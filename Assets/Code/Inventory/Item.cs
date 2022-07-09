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

	// For in-game display
	public Sprite icon;
	public Color uiTint = Color.white;

	// Affects how it can be placed inside an inventory
	public Vector2Int inventorySize = new Vector2Int(1, 1);

	// Only set on instantiated copies during game
	protected Transform hand;
	protected bool equipped;

	// Called when the player spawns
	public virtual void Init()
	{

	}

	// Called when the player clicks while this item is held
	public virtual void Use(UseHow useHow)
	{

	}

	// Called when the player starts holding this item
	public virtual void Equip(Transform hand)
	{
		equipped = true;
		this.hand = hand;
	}

	// Called when the player stops holding this item
	public virtual void Unequip()
	{
		equipped = false;
		hand = null;
	}

	// Called from the player every frame while held
	public virtual void Update()
	{

	}

	// Called from the player every frame while held
	public virtual void ModelUpdate(GameObject model)
	{

	}

	// Called occasionally while in inventory or held
	public virtual void InventoryTick(float deltaTime)
	{

	}
}
