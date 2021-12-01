using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;

public class UIManager : MonoBehaviour
{
	private static UIManager Instance;

	private bool watchRaised;

	public Animator watchAnim;

	public TMPro.TextMeshProUGUI healthText;

	public UnityEngine.UI.Image staminaSlider;

	public UnityEngine.Rendering.Volume deathPostProcess;

	public UnityEngine.Rendering.Volume damagePostProcess;

	public GameObject deathCanvas;

	private void Awake()
	{
		if (!Instance)
			Instance = this;
		else
			Destroy(gameObject);

		SetDeathUI(false);
	}

	public static void SetWatchRaised(bool raised)
	{
		Instance.watchRaised = raised;

		Instance.watchAnim.SetBool("WatchRaised", raised);
	}

	public static void SetCurrentHealth(int health)
	{
		Instance.healthText.text = "" + health;
	}

	public static void SetCurrentStamina(float stamina)
	{
		Instance.staminaSlider.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, stamina * 100);
	}

	public static void SetDeathPostProcess(float death)
	{
		Instance.deathPostProcess.weight = death;
	}

	public static void SetDamagePostProcess(float damage)
	{
		Instance.damagePostProcess.weight = damage;
	}

	public static void SetDeathUI(bool show)
	{
		Instance.deathCanvas.SetActive(show);
	}
}