using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GameManager : MonoBehaviour
{
	public static GameManager Instance;

	public bool finishedLoading = false;

	public PlayerLoader player;

	public CanvasHider loadingScreen;

	public Volume deathPostProcess;
	private float panSpeed;
	private float newPanSpeed;
	private Timer panSpeedRandomizer = new Timer(2);

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

		EnableLoadingUX();
	}

	private void Start()
	{
		
	}

	private void Update()
	{
		if (finishedLoading)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		else
		{
			panSpeedRandomizer.Increment(Time.deltaTime);
			if (panSpeedRandomizer.Expired())
			{
				panSpeedRandomizer.Reset();

				newPanSpeed = Mathf.Lerp(newPanSpeed, SeedlessRandom.NextFloatInRange(-90, 90), 0.5f);
			}
			panSpeed = Mathf.Lerp(panSpeed, newPanSpeed, Time.deltaTime);

			player.transform.Rotate(Vector3.up * panSpeed * Time.deltaTime);
		}
	}

	public void FinishLoading()
	{
		if (finishedLoading)
			return;

		DisableLoadingUX();

		player.ActivatePlayer();

		GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>().enabled = true;

		loadingScreen.Hide();

		finishedLoading = true;
	}

	public static bool GetFinishedLoading()
	{
		return Instance.finishedLoading;
	}

	private void EnableLoadingUX()
	{
		deathPostProcess.weight = 1;
	}

	private void DisableLoadingUX()
	{
		deathPostProcess.weight = 0;
	}
}
