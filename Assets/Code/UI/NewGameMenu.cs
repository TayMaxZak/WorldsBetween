using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewGameMenu : MonoBehaviour
{
	// TODO: Change scene indexing
	public int gameSceneIndex = 1;

	public int startSeed;

	[ContextMenu("")]
	private void Awake()
	{
		startSeed = SeedlessRandom.NextIntInRange(int.MinValue, int.MaxValue);
	}

	public void NewGame()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Click);

		// Set cross-scene data
		PersistentData data = PersistentData.GetInstanceForWrite();
		data.SetSeed(startSeed);
		data.SetDebugMode(false);

		SceneManager.LoadScene(gameSceneIndex);
	}
}
