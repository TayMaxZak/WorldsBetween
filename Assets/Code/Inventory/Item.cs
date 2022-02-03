using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item")]
[System.Serializable]
public class Item : ScriptableObject
{
	public string label = "Item";

	public Vector2Int inventorySize = new Vector2Int(1,1);

	[HideInInspector]
	public Transform hand;

	// Called when the player clicks while this item is held
	public virtual void Use()
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

	// Called from the player every frame
	public virtual void Update()
	{
		
	}
}
