using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
	public int sceneIndex;

	private bool loaded = false;

	private void Update()
	{
		if (loaded)
			return;

		if (Time.time > 0.1f)
		{
			loaded = true;

			UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);
		}
	}
}
