using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
	public static MainMenu Instance;

	public enum MainMenuState
	{
		Start,
		Main,

		NewGame,
		Database,
		Settings
	}
	public MainMenuState state = MainMenuState.Start;

	public Animator startAnimator;
	public Animator mainAnimator;
	public Animator playGame;

	private void Awake()
	{
		Instance = this;

		startAnimator.SetFloat("IdleHide", 0);

		mainAnimator.SetFloat("IdleHide", 1);

		playGame.SetFloat("IdleHide", 1);
	}

	// Update is called once per frame
	void Update()
	{
		// Wait past initial frames
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
		else if (Input.GetButtonDown("Cancel"))
		{
			ReturnToMain();
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

	public void ReturnToMain()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Back);

		MainMenuState prev = state;
		state = MainMenuState.Main;
		Debug.Log(prev + "? -> Main");

		mainAnimator.SetTrigger("ShowMenu");

		if (prev == MainMenuState.NewGame)
		{
			playGame.SetTrigger("HideMenu");
		}
		else if (prev == MainMenuState.Database)
		{

		}
		else if (prev == MainMenuState.Settings)
		{

		}
	}

	/* Main */

	public void QuitGame()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Back);

		Debug.Log("Quit Game");
		Application.Quit();
	}

	public void OpenPlayGame()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Click);

		state = MainMenuState.NewGame;
		Debug.Log("Main -> PlayGame?" + state);

		mainAnimator.SetTrigger("HideMenu");
		playGame.SetTrigger("ShowMenu");
	}

	public void OpenDatabase()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Click);

		state = MainMenuState.Database;
		Debug.Log("Main -> Database");

		mainAnimator.SetTrigger("HideMenu");
		//startAnimator.SetTrigger("ShowMenu");
	}

	public void OpenSettings()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Click);

		state = MainMenuState.Settings;
		Debug.Log("Main -> Settings");

		mainAnimator.SetTrigger("HideMenu");
		//startAnimator.SetTrigger("ShowMenu");
	}
}
