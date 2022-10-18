using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

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

	public float timeSpentLoading = 0;

	public Sound exitLevelSound;
	private bool finishingLevel = false;
	private float exitCurTimeScale = 1;
	private float exitGoalTimeScale = 0.1f;
	private float goalWaveDistance;
	private float worldSFXLowpass = 1;

	private readonly int exitShaderPropId = Shader.PropertyToID("GoalWaveDistance");

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

		goalWaveDistance = -10;
		Shader.SetGlobalFloat(exitShaderPropId, goalWaveDistance);
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
			timeSpentLoading += Time.deltaTime;

			// Calculate progress
			float builderProgress = World.WorldBuilder.GetGenProgress();
			float lighterProgress = World.LightEngine.GetGenProgress();

			loadingProgress = (builderProgress * 1 + lighterProgress * 1) / (1 + 1);
			// Get display progress by interpolating
			loadingProgressSmooth = Mathf.Lerp(loadingProgressSmooth, loadingProgress, Time.deltaTime * 3);
			loadingProgressSmooth = Mathf.Clamp(loadingProgressSmooth, 0, 1);

			if (World.WorldBuilder.genStage >= WorldBuilder.GenStage.EnqueueChunks && !startedBuilding)
				ShowProgress();

			// Lighting can be calculated efficiently
			if (CloseEnough(builderProgress, 1) && !startedLight)
				StartLighting();

			if (CloseEnough(builderProgress, 1) && !finishedBuilding)
				FinishBuilding();

			// Lighting can be applied
			if (CloseEnough(lighterProgress, 1) && !finishedLight)
				FinishLighting();

			// Start transition from loading
			if (CloseEnough(loadingProgress, 1) && World.WorldBuilder.genStage == WorldBuilder.GenStage.Ready)
				FinishLoading();
		}

		if (finishingLevel)
		{
			goalWaveDistance = Mathf.Min(goalWaveDistance + Time.deltaTime * ((goalWaveDistance < 0) ? 20 : 40 + goalWaveDistance * 1), 10000);
			Shader.SetGlobalFloat(exitShaderPropId, goalWaveDistance);

			exitCurTimeScale = Mathf.Lerp(exitCurTimeScale, exitGoalTimeScale, Time.unscaledDeltaTime);
			Time.timeScale = exitCurTimeScale;

			worldSFXLowpass = Mathf.Clamp01(worldSFXLowpass - Time.unscaledDeltaTime * 0.5f);
			AudioManager.SetWorldEffectsLowpass(Mathf.Lerp(1, Mathf.Abs(worldSFXLowpass), 0.7f));
		}
		else
		{
			exitCurTimeScale = 1;
			worldSFXLowpass = 1;
		}
	}

	private bool CloseEnough(float val, float target)
	{
		// Used to need - 0.001 in order to be true, but this lead to more issues in rare cases
		return val >= target; 
	}

	public void ShowProgress()
	{
		startedBuilding = true;

		loadingScreen.ShowProgressBar();
	}

	public void FinishBuilding()
	{
		finishedBuilding = true;

		World.WorldBuilder.genStage = WorldBuilder.GenStage.FindPoints;
	}

	public void StartLighting()
	{
		startedLight = true;

		World.LightEngine.Begin();
	}

	public void FinishLighting()
	{
		finishedLight = true;

		WorldLightAtlas.Instance.UpdateLightTextures();
	}

	public async void FinishLoading()
	{
		if (startedFadingOut || finishedLoading)
			return;
		startedFadingOut = true;

		// Transition from loading state to game state
		loadingScreen.StartFadingOut();


		await Task.Delay(1000);


		// Init actors
		PhysicsManager.Instance.InitAll();
		// Activate player
		Player.Instance.ActivatePlayer();
		// Start appropriate music
		if (World.HasEncounter())
			AudioManager.PlayMusicCue(AudioManager.CueType.EncounterPossible);

		finishedLoading = true;


		// Disable loading screen completely after some time
		await Task.Delay(4000);


		if (loadingScreen)
			loadingScreen.Hide();
	}

	public static bool IsFinishedLoading()
	{
		if (!Instance)
			return true;

		return Instance.finishedLoading;
	}

	public static float GetSmoothLoadingProgress()
	{
		return Instance.loadingProgressSmooth;
	}

	public static void FinishLevel()
	{
		Instance.ExitLevel();
	}

	public static bool IsFinishingLevel()
	{
		return Instance.finishingLevel;
	}

	private async void ExitLevel()
	{
		if (finishingLevel)
			return;

		AudioManager.StopMusicCue();
		AudioManager.PlaySoundDontDestroyOnLoad(Instance.exitLevelSound, transform.position);

		goalWaveDistance = -10;
		finishingLevel = true;

		Player.Instance.mover.invertControls = true;


		await Task.Delay(3000);


		loadingProgress = 0;
		loadingProgressSmooth = 0;
		loadingScreen.Reactivate();


		await Task.Delay(1500);


		Player.Instance.mover.invertControls = false;

		goalWaveDistance = -10;
		Shader.SetGlobalFloat(exitShaderPropId, goalWaveDistance);

		worldSFXLowpass = 1;
		AudioManager.SetWorldEffectsLowpass(worldSFXLowpass);

		finishingLevel = false;

		Time.timeScale = 1;

		SceneManager.LoadScene(1);
	}
}
