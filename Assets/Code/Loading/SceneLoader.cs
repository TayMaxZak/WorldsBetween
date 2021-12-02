using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
	public int sceneIndex;

	private bool loaded = false;

	private void Awake()
	{
		Random.InitState(System.Environment.TickCount);

		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	private void Update()
	{
		if (loaded)
			return;

		if (Time.time > 0.1f)
		{
			loaded = true;

			SceneManager.LoadScene(sceneIndex);
		}
	}
}
