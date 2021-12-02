using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameManager : MonoBehaviour
{
	private static GameManager Instance;

	public bool finishedLoading = false;

	public PlayerLoader player;

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

		Instance.finishedLoading = true;
	}

	public static bool GetFinishedLoading()
	{
		return Instance.finishedLoading;
	}
}
