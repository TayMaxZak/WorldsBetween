using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class UIManager : MonoBehaviour
{
	private static UIManager Instance;

	private bool watchRaised;

	public Animator watchAnim;

	private bool meterRaised;

	public Animator meterAnim;

	public TMPro.TextMeshProUGUI healthText;

	public Image staminaSlider;

	public Volume deathPostProcess;

	public Volume damagePostProcess;

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

		Instance.watchAnim.SetBool("Raised", raised);
	}

	public static void SetMeterRaised(bool raised)
	{
		Instance.meterRaised = raised;

		Instance.meterAnim.SetBool("Raised", raised);
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