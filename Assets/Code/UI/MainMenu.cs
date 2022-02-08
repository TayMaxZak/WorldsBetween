using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
	public enum MainMenuState
	{
		Start,
		Main
	}
	public MainMenuState state = MainMenuState.Start;

	public Animator startAnimator;
	public Animator mainAnimator;

	// Update is called once per frame
	void Update()
	{
		if (Time.time < 0.2f)
			return;

		if (state == MainMenuState.Start && Input.anyKeyDown)
		{
			PressStart();
		}
		else if (state == MainMenuState.Main && Input.GetButtonDown("Cancel"))
		{
			ReturnToStart();
		}
	}

	private void PressStart()
	{
		state = MainMenuState.Main;
		Debug.Log("Start -> Main");

		startAnimator.SetTrigger("HideMenu");
		mainAnimator.SetTrigger("ShowMenu");
	}

	private void ReturnToStart()
	{
		state = MainMenuState.Start;
		Debug.Log("Main -> Start");

		mainAnimator.SetTrigger("HideMenu");
		startAnimator.SetTrigger("ShowMenu");
	}
}
