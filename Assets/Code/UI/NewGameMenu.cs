using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewGameMenu : MonoBehaviour
{
	public UIInventory uiInventory;
	public UIKeystone uiKeystone;

	public int gameSceneIndex = 1;

	[SerializeField]
	public List<Item> stockItems;

	private void Awake()
	{
		PersistentData data = PersistentData.GetInstanceForWrite();

		// Create inventory and assign it
		Inventory playerInventory = new Inventory(uiInventory.backpackSize);
		data.SetPlayerInventory(playerInventory);

		uiInventory.Init(playerInventory, stockItems);
	}

	public void NewGame()
	{
		// Set cross-scene data
		PersistentData data = PersistentData.GetInstanceForWrite();

		uiKeystone.ShuffleIfDefault();

		data.SetSeed(uiKeystone.GetStringSeed());

		data.SetPlayerInventory(uiInventory.GetInventory());

		data.SetDebugMode(false);

		SceneManager.LoadScene(gameSceneIndex);
	}
}
