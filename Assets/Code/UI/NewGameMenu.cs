using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewGameMenu : MonoBehaviour
{
	public UIInventory uiInventory;
	public UIKeystone uiKeystone;

	public int gameSceneIndex = 1;

	private void Awake()
	{
		PersistentData data = PersistentData.GetInstanceForWrite();

		// Create inventory and assign it
		Inventory playerInventory = new Inventory(uiInventory.size);
		data.SetPlayerInventory(playerInventory);
		uiInventory.Init(playerInventory);
	}

	public void NewGame()
	{
		// Set cross-scene data
		PersistentData data = PersistentData.GetInstanceForWrite();
		data.SetSeed(uiKeystone.GetStringSeed());

		SceneManager.LoadScene(gameSceneIndex);
	}
}
