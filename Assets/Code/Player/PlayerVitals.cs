using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVitals : MonoBehaviour
{
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
		// Stamina only regens up to current health or max stamina
		float staminaCap = Mathf.Min(currentHealth, maxStamina);
		if (currentStamina < staminaCap)
			currentStamina += Mathf.Min(staminaRegen * deltaTime, staminaCap - currentStamina);
		else if (currentStamina < maxStamina)
			currentStamina += Mathf.Min(slowStaminaRegen * deltaTime, maxStamina - currentStamina);

		// Health only regens up to current stamina or max health
		float healthCap = Mathf.Min(currentStamina, maxHealth);
		if (currentHealth < healthCap)
			currentHealth += Mathf.Min(healthRegen * deltaTime, healthCap - currentHealth);

		UpdateUI();
	}

	private void UpdateUI()
	{
		UIManager.SetCurrentHealth(Mathf.RoundToInt(currentHealth));
		UIManager.SetCurrentStamina(currentStamina / maxStamina);
	}

	public void DealDamage(float amount)
	{
		if (dead)
			return;

		currentStamina -= amount / 2;

		//// More damage if out of stamina
		//if (currentStamina < 0)
		//	amount -= currentStamina;

		currentHealth -= amount;

		UpdateUI();

		if (currentHealth <= 0)
			Die();
	}

	private void Die()
	{
		AudioManager.PlaySound(deathSound, transform.position);
		dead = true;
	}

	public void Respawn()
	{
		dead = false;

		currentHealth = maxHealth;
		currentStamina = maxStamina;
	}
}
