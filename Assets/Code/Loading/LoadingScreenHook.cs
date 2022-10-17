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
	private float groupOpacity = 1;

	[Header("Main")]
	[SerializeField]
	private GameObject generatingUI;

	[SerializeField]
	private Image loadingBar;
	[SerializeField]
	private float loadingBarWidth = 100;
	[SerializeField]
	private TMPro.TextMeshProUGUI loadingText;

	[Header("Background")]
	[SerializeField]
	private Image bkg;
	[SerializeField]
	private CanvasGroup bkgBlack;

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

	private bool updateProgress = false;
	private bool fadingOut = false;
	private bool fadingIn = false;

	private void Awake()
	{
		generatingUI.SetActive(false);

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

		// Retrieve progress
		if (World.WorldBuilder.genStage >= WorldBuilder.GenStage.EnqueueChunks)
			progressDisplay = GameManager.GetSmoothLoadingProgress();
		else
			progressDisplay = 0;

		UpdateProgress(progressDisplay);

		if (fadingOut || fadingIn)
		{
			groupOpacity = Mathf.Lerp(groupOpacity, fadingOut ? 0 : 1, Time.unscaledDeltaTime * 2);

			group.alpha = groupOpacity;
			if (updateProgress)
				AudioManager.SetAmbientVolume(1 - groupOpacity);
		}
		else
		{
			group.alpha = 1;
			AudioManager.SetAmbientVolume(0);
		}
	}

	private void UpdateProgress(float progress)
	{
		// Loading bar
		loadingBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, progress * loadingBarWidth);

		loadingText.text = "" + Mathf.CeilToInt(progress * 100);

		// Spin the rings
		float spinSpeed = Mathf.Clamp01(2 * progress) * (progress);
		spinSpeed *= spinSpeed * spinSpeed;
		foreach (SpinRing spin in ringsToSpin)
			spin.toRotate.Rotate(overallSpinSpeed * spin.speedMult * spinSpeed * Time.deltaTime * Vector3.forward);

		// Fade black
		bkgBlack.alpha = 1 - 2 * Mathf.Clamp01(progress - 0.5f);

		// Elevator music
		UpdateMusic(progress);
	}

	private void UpdateMusic(float progress)
	{
		float fade = groupOpacity;
		melodyLayer.volume = (1 - progress) * overallVolume * fade;

		float middleToEnd = Mathf.Clamp01(2 * progress);
		padLayer.volume = middleToEnd * middleToEnd * overallVolume * fade;

		bassLayer.volume = progress * progress * progress * overallVolume * fade;
	}

	public void ShowProgressBar()
	{
		if (generatingUI)
			generatingUI.SetActive(true);

		UpdateProgress(0);

		updateProgress = true;
	}

	public void StartFadingOut()
	{
		fadingOut = true;
	}

	public void Hide()
	{
		UpdateProgress(1);

		updateProgress = false;

		gameObject.SetActive(false);
	}

	public void Activate()
	{
		fadingOut = false;
		fadingIn = true;

		UpdateProgress(0);

		foreach (SpinRing spin in ringsToSpin)
			spin.toRotate.rotation = Quaternion.identity;
		generatingUI.SetActive(false);
		updateProgress = true;

		gameObject.SetActive(true);
	}
}
