using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item")]
[System.Serializable]
public class Item : ScriptableObject
{
	public string label = "Item";

	public Vector2Int inventorySize = new Vector2Int(1,1);

	// Called when the player clicks while this item is held
	public virtual void Use()
	{
		
	}
}
