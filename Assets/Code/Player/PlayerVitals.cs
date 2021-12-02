using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVitals : MonoBehaviour
{
	public PlayerMover mover;

	public bool dead = false;

	public float currentHealth = 100;
	public float maxHealth = 100;

	public float healthRegen = 2;

	public float currentStamina = 100;
	public float maxStamina = 100;

	public float staminaRegen = 10;
	public float slowStaminaRegen = 1;

	public Timer vitalsTimer = new Timer(0.05f);

	public Sound deathSound;

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

		vitalsTimer.Increment(Time.deltaTime);
		if (vitalsTimer.Expired())
		{
			VitalsTick(vitalsTimer.maxTime);

			vitalsTimer.Reset();
		}
	}

	private void VitalsTick(float deltaTime)
	{
		// Stamina only regens when the player can breathe
		if (!mover.underWater && !mover.sprinting)
		{
			// Stamina only regens up to current health or max stamina
			float staminaCap = Mathf.Min(currentHealth, maxStamina);
			if (currentStamina < staminaCap)
				currentStamina += Mathf.Min(staminaRegen * deltaTime, staminaCap - currentStamina);
			else if (currentStamina < maxStamina)
				currentStamina += Mathf.Min(slowStaminaRegen * deltaTime, maxStamina - currentStamina);
		}

		// Health only regens up to current stamina or max health
		float healthCap = Mathf.Min(currentStamina, maxHealth);
		if (currentHealth < healthCap)
			currentHealth += Mathf.Min(healthRegen * deltaTime, healthCap - currentHealth);

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

			UIManager.SetDeathUI(true);
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

		currentStamina -= amount / 2;
		if (currentStamina < 0)
			currentStamina = 0;

		UpdateUIUX();

		//// More damage if out of stamina
		//if (currentStamina < 0)
		//	amount -= currentStamina;

		HurtHealth(amount);
	}

	public bool UseStamina(float amount, bool hurtWhenEmpty)
	{
		if (dead)
			return false;

		if (!hurtWhenEmpty)
		{
			if (currentStamina < amount)
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
		if (dead)
			return;

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
		AudioManager.PlaySound(deathSound, transform.position);

		dead = true;

		UpdateUIUX();
	}

	private void NearDie()
	{
		//AudioManager.PlaySound(deathSound, transform.position);

		//dead = true;

		currentHealth = 1;
		currentStamina = 0;

		UpdateUIUX();
	}

	public void Respawn()
	{
		dead = false;

		showVitals = false;

		UIManager.SetDeathUI(false);

		currentHealth = maxHealth;
		currentStamina = maxStamina;
	}
}
