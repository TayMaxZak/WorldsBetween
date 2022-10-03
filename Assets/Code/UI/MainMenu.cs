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
	public Animator newGameAnimator;
	public Animator databaseAnimator;
	public Animator settingsAnimator;

	private void Awake()
	{
		Instance = this;

		startAnimator.SetFloat("IdleHide", 0);

		mainAnimator.SetFloat("IdleHide", 1);

		newGameAnimator.SetFloat("IdleHide", 1);
	}

	// Update is called once per frame
	void Update()
	{
		// Wait past initial frames
		if (Time.time < 0.1f)
			return;

		if (state == MainMenuState.Start && Input.anyKeyDown)
		{
			PressStart();
		}
		else if (state == MainMenuState.Main && Input.GetButtonDown("Cancel"))
		{
			ReturnToStart();
		}
		//else if (Input.GetButtonDown("Cancel"))
		//{
		//	ReturnToMain();
		//}
	}

	private void PressStart()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Start);

		state = MainMenuState.Main;
		//Debug.Log("Start -> Main");

		startAnimator.SetTrigger("HideMenu");
		mainAnimator.SetTrigger("ShowMenu");
	}

	// TODO: This breaks if the player clicks on buttons during transition
	private void ReturnToStart()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Back);

		state = MainMenuState.Start;
		//Debug.Log("Main -> Start");

		mainAnimator.SetTrigger("HideMenu");
		startAnimator.SetTrigger("ShowMenu");
	}

	public void ReturnToMain()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Back);

		MainMenuState prev = state;
		state = MainMenuState.Main;
		//Debug.Log(prev + " -> Main");

		mainAnimator.SetTrigger("ShowMenu");

		if (prev == MainMenuState.NewGame)
		{
			newGameAnimator.SetTrigger("HideMenu");
		}
		else if (prev == MainMenuState.Database)
		{
			databaseAnimator.SetTrigger("HideMenu");
		}
		else if (prev == MainMenuState.Settings)
		{
			settingsAnimator.SetTrigger("HideMenu");
		}
	}

	/* Main */

	public void QuitGame()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Click);

		Debug.Log("Quit Game");

		Application.Quit();
	}

	public void OpenNewGame()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Click);

		state = MainMenuState.NewGame;
		//Debug.Log("Main -> NewGame");

		mainAnimator.SetTrigger("HideMenu");
		newGameAnimator.SetTrigger("ShowMenu");
	}

	public void OpenDatabase()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Click);

		state = MainMenuState.Database;
		//Debug.Log("Main -> Database");

		mainAnimator.SetTrigger("HideMenu");
		databaseAnimator.SetTrigger("ShowMenu");
	}

	public void OpenSettings()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Click);

		state = MainMenuState.Settings;
		//Debug.Log("Main -> Settings");

		mainAnimator.SetTrigger("HideMenu");
		settingsAnimator.SetTrigger("ShowMenu");
	}
}
