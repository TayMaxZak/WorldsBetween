using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
	public enum MainMenuState
	{
		Start,
		Main,

		Play,
		Database,
		Settings
	}
	public MainMenuState state = MainMenuState.Start;

	public Animator startAnimator;
	public Animator mainAnimator;

	private void Awake()
	{
		startAnimator.SetFloat("IdleHide", 0);
		mainAnimator.SetFloat("IdleHide", 1);
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

	private void ReturnToMain()
	{
		state = MainMenuState.Main;
		Debug.Log("? -> Main");

		mainAnimator.SetTrigger("ShowMenu");
	}

	/* Main */

	public void Play()
	{
		state = MainMenuState.Play;
		Debug.Log("Main -> Play");

		mainAnimator.SetTrigger("HideMenu");
		//startAnimator.SetTrigger("ShowMenu");

		SceneManager.LoadScene(1);
	}

	public void Database()
	{
		state = MainMenuState.Database;
		Debug.Log("Main -> Database");

		mainAnimator.SetTrigger("HideMenu");
		//startAnimator.SetTrigger("ShowMenu");
	}

	public void Settings()
	{
		state = MainMenuState.Settings;
		Debug.Log("Main -> Settings");

		mainAnimator.SetTrigger("HideMenu");
		//startAnimator.SetTrigger("ShowMenu");
	}
}
