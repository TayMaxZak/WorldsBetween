using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Threading.Tasks;

public partial class GameManager : MonoBehaviour
{
	public static GameManager Instance;

	public bool finishedLoading = false;

	public PlayerLoader player;

	public LoadingScreenHook loadingScreen;

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


			float fade = Mathf.Clamp01((1 - loadingScreen.GetDisplayProgress()) * 5);

			player.transform.Rotate(Vector3.up * panSpeed * fade * Time.deltaTime);
		}
	}

	public void MidLoading()
	{
		loadingScreen.SeeThrough();
	}

	private void AlmostFinishLoading()
	{
		loadingScreen.AlmostDone();
	}

	public async void FinishLoading()
	{
		AlmostFinishLoading();

		if (finishedLoading)
			return;

		await Task.Delay(1000);

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
		loadingScreen.Show();

		deathPostProcess.weight = 1;
	}

	private void DisableLoadingUX()
	{
		deathPostProcess.weight = 0;
	}
}
