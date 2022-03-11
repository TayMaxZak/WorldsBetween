using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVitals : MonoBehaviour
{
	public PlayerMover mover;

	public bool dead = false;
	public Timer vitalsTickTimer = new Timer(0.05f);

	[Header("Health")]
	public float currentHealth = 100;
	public float maxHealth = 100;

	public Timer stopHealthRegen = new Timer(5);
	public float healthRegen = 2;

	[Header("Stamina")]
	public float currentStamina = 100;
	public float maxStamina = 100;

	public Timer stopStaminaRegen = new Timer(3);
	public float staminaRegen = 10;
	public float staminaRegenSlow = 1;


	[Header("UI/UX")]
	public Sound deathSound;

	public Sound nearDeathSound;

	public bool showVitals;

	public AudioSource heartbeatLoop;
	public AudioSource breathingLoop;

	private void Update()
	{
		if (dead)
			return;

		if (Input.GetButtonDown("Vitals"))
			showVitals = !showVitals;

		UIManager.SetWatchRaised(showVitals);

		stopHealthRegen.Increment(Time.deltaTime);
		stopStaminaRegen.Increment(Time.deltaTime);

		vitalsTickTimer.Increment(Time.deltaTime);
		if (vitalsTickTimer.Expired())
		{
			VitalsTick(vitalsTickTimer.maxTime);

			vitalsTickTimer.Reset();
		}
	}

	private void VitalsTick(float deltaTime)
	{
		if (stopStaminaRegen.Expired())
		{
			// Stamina only regens up to current health or max stamina
			float staminaCap = Mathf.Min(currentHealth, maxStamina);
			if (currentStamina < staminaCap)
				currentStamina += Mathf.Min(staminaRegen * deltaTime, staminaCap - currentStamina);
			else if (currentStamina < maxStamina)
				currentStamina += Mathf.Min(staminaRegenSlow * deltaTime, maxStamina - currentStamina);
		}

		if (stopHealthRegen.Expired())
		{
			if (currentHealth < maxHealth)
				currentHealth += Mathf.Min(healthRegen * deltaTime, maxHealth - currentHealth);
		}

		UpdateUIUX();
	}

	private void UpdateUIUX()
	{
		UIManager.SetCurrentHealth(Mathf.RoundToInt(currentHealth));
		UIManager.SetCurrentStamina(currentStamina / maxStamina);

		if (dead)
		{
			heartbeatLoop.volume = 0;
			breathingLoop.volume = 0;

			UIManager.SetDeathPostProcess(1);

			UIManager.SetDamagePostProcess(1);

			UIManager.SetWatchRaised(false);
			UIManager.SetMeterRaised(false);

			UIManager.SetDeathUI(true);

			AudioManager.StopMusicCue();
		}
		else
		{
			float heartbeatVolume = (1 - currentHealth / maxHealth);
			heartbeatVolume = Mathf.Clamp01((heartbeatVolume - 0.5f) * 2);
			float heartbeatPitch = heartbeatVolume * heartbeatVolume;
			float damageAmount = 1 - (1 - heartbeatVolume) * (1 - heartbeatVolume) * (1 - heartbeatVolume);
			heartbeatVolume = 1 - (1 - heartbeatVolume) * (1 - heartbeatVolume);

			UIManager.SetDamagePostProcess(damageAmount);

			heartbeatLoop.volume = heartbeatVolume * 0.4f;
			heartbeatLoop.pitch = 0.99f + heartbeatPitch * 0.21f;

			float breathingVolume = (1 - currentStamina / maxStamina);
			breathingVolume = Mathf.Clamp01((breathingVolume - 0.5f) * 2f);
			breathingVolume = 1 - (1 - breathingVolume) * (1 - breathingVolume);

			breathingLoop.volume = breathingVolume * 0.2f;
			breathingLoop.pitch = 1.04f;

			float nearDeath = (1 - currentHealth / maxHealth);
			nearDeath = Mathf.Clamp01((nearDeath - 0.75f) * 4);
			nearDeath *= nearDeath * nearDeath * nearDeath;
			UIManager.SetDeathPostProcess(0.7f * nearDeath);
		}
	}

	public void DealDamage(float amount)
	{
		if (dead)
			return;

		UseStamina(amount, false, true);

		HurtHealth(amount);
	}

	public bool UseStamina(float amount, bool hurtWhenEmpty, bool useAnyway)
	{
		if (dead || amount <= 0)
			return false;

		stopStaminaRegen.Reset();

		if (!hurtWhenEmpty)
		{
			if (!useAnyway && currentStamina < amount)
				return false;

			currentStamina -= amount;

			UpdateUIUX();

			if (currentStamina > 0)
			{
				return true;
			}
			else
			{
				currentStamina = 0;

				UpdateUIUX();
				return false;
			}
		}
		else
		{
			currentStamina -= amount;

			// Leftover stamina
			if (currentStamina < 0)
			{
				HurtHealth(-currentStamina);

				currentStamina = 0;
			}

			UpdateUIUX();

			if (currentStamina <= 0)
				return false;
			else
				return true;
		}
	}

	private void HurtHealth(float amount)
	{
		if (dead || amount <= 0)
			return;

		stopHealthRegen.Reset();

		float initialHealth = currentHealth;

		currentHealth -= amount;

		float finalHealth = currentHealth;

		if (currentHealth < 0)
			currentHealth = 0;

		UpdateUIUX();

		if (currentHealth <= 0)
			TryDie(initialHealth, finalHealth);
	}

	private void TryDie(float initialHealth, float finalHealth)
	{
		if (initialHealth <= 25 || finalHealth < -initialHealth)
			Die();
		else
			NearDie();
	}

	private void Die()
	{
		if (deathSound)
			AudioManager.PlaySound(deathSound, transform.position);

		dead = true;

		UpdateUIUX();
	}

	private void NearDie()
	{
		if (nearDeathSound)
			AudioManager.PlaySound(nearDeathSound, transform.position);

		currentHealth = 1;
		//currentStamina = 0;

		UpdateUIUX();
	}

	public void Respawn()
	{
		dead = false;

		UIManager.SetDeathUI(false);

		currentHealth = maxHealth;
		currentStamina = maxStamina;

		UpdateUIUX();
	}
}
