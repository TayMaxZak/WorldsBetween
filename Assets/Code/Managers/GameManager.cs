using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameManager : MonoBehaviour
{
	public static GameManager Instance;

	public bool finishedLoading = false;

	public PlayerLoader player;

	public CanvasHider loadingScreen;

	private void Awake()
	{
		// Ensure singleton
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}
		else
			Instance = this;
	}

	public void FinishLoading()
	{
		player.ActivatePlayer();

		GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>().enabled = true;

		Cursor.lockState = CursorLockMode.Locked;

		loadingScreen.Hide();

		finishedLoading = true;
	}

	public static bool GetFinishedLoading()
	{
		return Instance.finishedLoading;
	}
}
