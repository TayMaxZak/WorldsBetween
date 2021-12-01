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

	public AudioSource heartbeatLoop;
	public AudioSource breathingLoop;

	private void Update()
	{
		if (dead)
			return;

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
		if (!mover.underWater)
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

			UIManager.SetDeath(1);
		}
		else
		{
			float heartbeatVolume = (1 - currentHealth / maxHealth);
			heartbeatVolume = Mathf.Clamp01((heartbeatVolume - 0.5f) * 2);
			float heartbeatPitch = heartbeatVolume * heartbeatVolume;
			heartbeatVolume = 1 - (1 - heartbeatVolume) * (1 - heartbeatVolume);

			heartbeatLoop.volume = heartbeatVolume * 0.4f;
			heartbeatLoop.pitch = 0.99f + heartbeatPitch * 0.21f;

			float breathingVolume = (1 - currentStamina / maxStamina);
			breathingVolume = Mathf.Clamp01((breathingVolume - 0.5f) * 2);
			float breathingPitch = breathingVolume * breathingVolume;
			breathingVolume = 1 - (1 - breathingVolume) * (1 - breathingVolume);

			breathingLoop.volume = breathingVolume * 0.25f;
			breathingLoop.pitch = 1.0f + breathingPitch * 0.04f;

			float nearDeath = (1 - currentHealth / maxHealth);
			nearDeath = Mathf.Clamp01((nearDeath - 0.75f) * 4);
			nearDeath *= nearDeath * nearDeath * nearDeath;
			UIManager.SetDeath(0.7f * nearDeath);
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

		currentHealth -= amount;
		if (currentHealth < 0)
			currentHealth = 0;

		UpdateUIUX();

		if (currentHealth <= 0)
			Die();
	}

	private void Die()
	{
		AudioManager.PlaySound(deathSound, transform.position);

		dead = true;

		UpdateUIUX();
	}

	public void Respawn()
	{
		dead = false;

		currentHealth = maxHealth;
		currentStamina = maxStamina;
	}
}
