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

	private float progress;
	private float newProgress;

	private bool updateProgress = false;

	private void Awake()
	{
		UpdateBackground(false);
		UpdateProgress(0);
	}

	private void Update()
	{
		if (!updateProgress)
			return;

		if (World.Generator.genStage >= WorldGenerator.GenStage.GenerateChunks)
			newProgress = Mathf.Clamp01(World.Generator.GenProgress() / 0.67f);
		else
			newProgress = 0;

		progress = Mathf.Lerp(progress, newProgress, Time.deltaTime);

		UpdateProgress(progress);
	}

	public void UpdateProgress(float progress)
	{
		// Loading bar
		loadingBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, progress * 150);

		// Play button
		bool tooEarly = progress < 0.2f;

		playButton.interactable = !tooEarly;

		playButtonText.text = progress < 0.5f ? (tooEarly ? "Not Ready" : "Early Play") : "Play";
		playButtonText.color = tooEarly ? fadedText : normalText;

		// Spin the rings
		float middleToEnd = Mathf.Clamp01(2 * progress);
		foreach (SpinRing spin in ringsToSpin)
			spin.toRotate.Rotate(Vector3.forward * overallSpinSpeed * spin.speedMult * middleToEnd * Time.deltaTime);

		// Elevator music
		UpdateMusic(progress);
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
		background.sprite = seeThrough ? transparentBkg : opaqueBkg;
	}

	public async void Show()
	{
		UpdateProgress(0);

		await Task.Delay(10);

		updateProgress = true;
	}

	public void SeeThrough()
	{
		UpdateBackground(true);
	}

	public async void Hide()
	{
		updateProgress = false;

		UpdateMusic(0.99f);

		await Task.Delay(10);

		UpdateProgress(1);

		await Task.Delay(10);

		gameObject.SetActive(false);
	}
}
