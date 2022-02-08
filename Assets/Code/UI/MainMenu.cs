using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
	public enum MainMenuState
	{
		PressStart,
		MainLayout
	}
	public MainMenuState state = MainMenuState.PressStart;

	// Update is called once per frame
	void Update()
	{
		if (state == MainMenuState.PressStart && Input.anyKeyDown)
		{
			NextState();
		}
	}

	private void NextState()
	{
		Debug.Log("Next state");
	}
}
