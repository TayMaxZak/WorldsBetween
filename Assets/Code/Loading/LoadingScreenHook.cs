using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class LoadingScreenHook : MonoBehaviour
{
	[System.Serializable]
	public class SpinRing
	{
		public Transform toRotate;
		public float speedMult;
	}

	[SerializeField]
	private CanvasGroup group;

	[Header("Main")]
	[SerializeField]
	private GameObject generatingUI;

	[SerializeField]
	private Image loadingBar;

	[SerializeField]
	private Button playButton;

	[SerializeField]
	private TMPro.TextMeshProUGUI playButtonText;

	private Color normalText = Color.white;
	private Color fadedText = new Color(1, 1, 1, 0.25f);

	[Header("Background")]
	[SerializeField]
	private Image background;

	[SerializeField]
	private Sprite opaqueBkg;
	[SerializeField]
	private Sprite transparentBkg;

	[SerializeField]
	private List<SpinRing> ringsToSpin;

	[SerializeField]
	private float overallSpinSpeed = 10;

	[Header("Elevator Music")]
	[SerializeField]
	private AudioSource melodyLayer;
	[SerializeField]
	private AudioSource padLayer;
	[SerializeField]
	private AudioSource bassLayer;
	[SerializeField]
	private float overallVolume = 0.25f;

	[Header("Random Tips")]
	[SerializeField]
	private RandomTip randomTip;
	[SerializeField]
	private Timer randomTipTimer = new Timer(10);

	private float progressDisplay;
	private float progressRaw;

	private bool updateProgress = false;
	private bool almostDone = false;

	private void Awake()
	{
		generatingUI.SetActive(false);

		UpdateBackground(false);
		UpdateProgress(0);

		randomTip.Randomize();
		randomTipTimer.Reset();
	}

	private void Update()
	{
		// Pick a new random tip
		randomTipTimer.Increment(Time.deltaTime);
		if (randomTipTimer.Expired())
		{
			randomTip.Randomize();

			randomTipTimer.Reset();
		}

		if (!updateProgress)
			return;

		if (!almostDone)
		{
			// Retrieve raw progress
			if (World.WorldBuilder.genStage >= WorldBuilder.GenStage.GenerateChunks)
				progressRaw = Mathf.Clamp01(World.WorldBuilder.GenProgress() / World.WorldBuilder.targetProgress);
			else
				progressRaw = 0;

			// Get display progress
			progressDisplay = Mathf.Lerp(progressDisplay, progressRaw, Time.deltaTime);
		}
		else
			progressDisplay = Mathf.Clamp01(progressDisplay + Time.deltaTime * 0.05f);

		UpdateProgress(progressDisplay);
	}

	private void UpdateProgress(float progress)
	{
		// Loading bar
		loadingBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, progress * 150);

		// Play button
		bool tooEarly = progress < 0.2f;

		playButton.interactable = !tooEarly;

		playButtonText.text = progress < 0.5f ? (tooEarly ? "Not Ready" : "Early Play") : "Play";
		playButtonText.color = tooEarly ? fadedText : normalText;

		// Spin the rings
		float spinSpeed = Mathf.Clamp01(2 * progress) * (progress);
		spinSpeed *= spinSpeed * spinSpeed;
		foreach (SpinRing spin in ringsToSpin)
			spin.toRotate.Rotate(Vector3.forward * overallSpinSpeed * spin.speedMult * spinSpeed * Time.deltaTime);

		// Elevator music
		UpdateMusic(progress);

		// Group
		float fade = Mathf.Clamp01((1 - progress) * 20);
		group.alpha = fade;
		if (updateProgress)
		{
			UIManager.SetDeathPostProcess(fade);
			AudioManager.SetAmbientVolume(1 - fade);
		}
	}

	private void UpdateMusic(float progress)
	{
		float fade = Mathf.Clamp01((1 - progress) * 10);
		melodyLayer.volume = (1 - progress) * overallVolume * fade;

		float middleToEnd = Mathf.Clamp01(2 * progress);
		padLayer.volume = middleToEnd * middleToEnd * overallVolume * fade;

		bassLayer.volume = progress * progress * progress * overallVolume * fade;
	}

	private void UpdateBackground(bool seeThrough)
	{
		if (background)
			background.sprite = seeThrough ? transparentBkg : opaqueBkg;
	}

	public async void StartedGenerating()
	{
		UpdateProgress(0);

		await Task.Delay(10);

		updateProgress = true;
	}

	public void SeeThrough()
	{
		if (generatingUI)
			generatingUI.SetActive(true);

		UpdateBackground(true);
	}

	public void AlmostDone()
	{
		almostDone = true;
	}

	public void Hide()
	{
		UpdateProgress(1);

		updateProgress = false;

		gameObject.SetActive(false);
	}

	public float GetDisplayProgress()
	{
		return progressDisplay;
	}
}
