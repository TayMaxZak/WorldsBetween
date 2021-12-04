using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
	private static SceneLoader Instance;

	public bool immediate = false;

	public int sceneIndex;

	private bool loaded = false;

	private void Awake()
	{
		if (!Instance)
		{
			if (!immediate)
				Instance = this;
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		DontDestroyOnLoad(gameObject);

		Random.InitState(System.Environment.TickCount);

		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		if (immediate)
		{
			Destroy(gameObject);
			Load();
		}
	}

	private void Update()
	{
		if (loaded || immediate)
			return;

		if (Time.time > 0.1f)
		{
			Load();
		}
	}

	private void Load()
	{
		loaded = true;

		SceneManager.LoadScene(sceneIndex);
	}

	public static void Remove()
	{
		if (Instance)
			Instance.Kill();
	}

	private void Kill()
	{
		Destroy(gameObject);
		Instance = null;
	}
}
