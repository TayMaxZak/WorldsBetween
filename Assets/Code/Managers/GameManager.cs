using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Threading.Tasks;

public partial class GameManager : MonoBehaviour
{
	public static GameManager Instance;

	private float loadingProgress;
	private float loadingProgressSmooth;

	private bool startedFadingOut = false;
	private bool finishedLoading = false;

	private bool startedBuilding = false;
	private bool finishedBuilding = false;

	private bool startedLight = false;
	private bool finishedLight = false;

	public LoadingScreenHook loadingScreen;

	// Rotate player while loading
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

		loadingScreen.ShowProgressBar();
	}

	private void OnDestroy()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	private void Update()
	{
		// Constant behavior while loading or not loading
		if (finishedLoading)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		else
		{
			// Calculate progress
			float builderProgress = World.WorldBuilder.GetGenProgress();
			float lighterProgress = World.LightEngine.GetGenProgress();

			loadingProgress = (builderProgress * 2 + lighterProgress * 3) / (2 + 3);
			// Get display progress by interpolating
			loadingProgressSmooth = Mathf.Lerp(loadingProgressSmooth, loadingProgress, Time.deltaTime * 2);

			if (World.WorldBuilder.genStage >= WorldBuilder.GenStage.EnqueueChunks && !startedBuilding)
				ShowProgress();

			// Lighting can be calculated efficiently
			if (CloseEnough(builderProgress, 0.95f) && !startedLight)
				StartLighting();

			if (CloseEnough(builderProgress, 1) && !finishedBuilding)
				FinishBuilding();

			// Lighting can be applied
			if (CloseEnough(lighterProgress, 1) && !finishedLight)
				FinishLighting();

			// Start transition from loading
			if (CloseEnough(loadingProgress, 1))
				FinishLoading(1000);

			RotateCameraWhileLoading();
		}
	}

	private bool CloseEnough(float val, float target)
	{
		return val >= target - 0.0001f;
	}

	private void RotateCameraWhileLoading()
	{
		panSpeedRandomizer.Increment(Time.deltaTime);
		if (panSpeedRandomizer.Expired())
		{
			panSpeedRandomizer.Reset();

			newPanSpeed = Mathf.Lerp(newPanSpeed, SeedlessRandom.NextFloatInRange(-90, 90), 0.5f);
		}
		panSpeed = Mathf.Lerp(panSpeed, newPanSpeed, Time.deltaTime);


		float fade = Mathf.Clamp01((1 - loadingProgressSmooth) * 5);

		Player.Instance.transform.Rotate(Vector3.up * panSpeed * fade * Time.deltaTime);
	}

	public void ShowProgress()
	{
		startedBuilding = true;

		loadingScreen.ShowProgressBar();
	}

	public void FinishBuilding()
	{
		finishedBuilding = true;

		World.WorldBuilder.genStage = WorldBuilder.GenStage.Ready;
	}

	public void StartLighting()
	{
		startedLight = true;

		World.LightEngine.Begin();
	}

	public void FinishLighting()
	{
		finishedLight = true;

		WorldLightAtlas.Instance.ApplyChanges();
	}

	public async void FinishLoading(int delay)
	{
		if (startedFadingOut || finishedLoading)
			return;
		startedFadingOut = true;

		// Transition from loading state to game state
		loadingScreen.StartFadingOut();

		await Task.Delay(delay);


		// Init actors
		PhysicsManager.Instance.InitAll();
		// Activate player
		Player.Instance.ActivatePlayer();

		finishedLoading = true;


		// Disable loading screen completely after some time
		await Task.Delay(delay * 4);

		loadingScreen.Hide();
	}

	public static bool GetFinishedLoading()
	{
		return Instance.finishedLoading;
	}

	public static float GetSmoothLoadingProgress()
	{
		return Instance.loadingProgressSmooth;
	}
}
