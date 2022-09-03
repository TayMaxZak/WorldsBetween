using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class UIManager : MonoBehaviour
{
	private static UIManager Instance;

	//private bool watchRaised;

	//public Animator watchAnim;

	private bool showVitals;
	public GameObject vitalsCanvas;

	public Image staminaSlider;
	public float staminaWidth = 1;


	public Image heldItemImage;


	public Volume deathPostProcess;

	public Volume damagePostProcess;

	public GameObject deathCanvas;

	private void Awake()
	{
		if (!Instance)
			Instance = this;
		else
			Destroy(gameObject);

		SetDie(false);
	}

	public static void SetDie(bool isDie)
	{
		SetDeathUI(isDie);
		//SetDeathPostProcess(isDie ? 1 : 0);
	}

	public static void SetShowVitals(bool show)
	{
		Instance.showVitals = show;
		Instance.vitalsCanvas.SetActive(show);
	}

	//public static void SetWatchRaised(bool raised)
	//{
	//	//Instance.watchRaised = raised;

	//	//Instance.watchAnim.SetBool("Raised", raised);
	//}

	public static void SetCurrentHealth(float health)
	{
		SetDamagePostProcess(1 - health);

		float nearDeath = 1 - health;
		nearDeath = Mathf.Clamp01((nearDeath - 0.75f) * 4);
		nearDeath *= nearDeath * nearDeath * nearDeath;
		SetDeathPostProcess(0.7f * nearDeath);
	}

	public static void SetCurrentStamina(float stamina)
	{
		Instance.staminaSlider.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, stamina * Instance.staminaWidth);
	}

	private static void SetDeathPostProcess(float death)
	{
		Instance.deathPostProcess.weight = death;
	}

	private static void SetDamagePostProcess(float damage)
	{
		Instance.damagePostProcess.weight = damage;
	}

	private static void SetDeathUI(bool show)
	{
		Instance.deathCanvas.SetActive(show);
	}

	public static void SetHeldItem(Item heldItem)
	{
		Instance.heldItemImage.sprite = heldItem.icon;
	}
}