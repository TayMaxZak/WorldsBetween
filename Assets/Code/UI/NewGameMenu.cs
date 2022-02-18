using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewGameMenu : MonoBehaviour
{
	public UIInventory uiInventory;
	public UIKeystone uiKeystone;

	public int gameSceneIndex = 1;

	public void NewGame()
	{
		// Set cross-scene data
		PersistentData data = PersistentData.GetInstanceForWrite();
		data.SetSeed(uiKeystone.GetStringSeed());
		data.SetInventory(uiInventory.inventory);

		SceneManager.LoadScene(gameSceneIndex);
	}
}
