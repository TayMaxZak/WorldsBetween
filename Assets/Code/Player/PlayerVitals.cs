using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVitals : MonoBehaviour
{
	public PlayerMover mover;

	public bool dead = false;
	public Timer vitalsTickTimer = new Timer(0.05f);
	private Timer takeDamageTimer = new Timer(0.5f);

	[Header("Health")]
	public float currentHealth = 100;
	public float maxHealth = 100;
	public bool canNearDie = true;

	public Timer stopHealthRegen = new Timer(5);
	public float healthRegen = 2;

	[Header("Stamina")]
	public float currentStamina = 100;
	public float maxStamina = 100;

	public Timer stopStaminaRegen = new Timer(3);
	public float staminaRegen = 10;


	[Header("UI/UX")]
	public Sound damagedSound;

	public Sound nearDeathSound;

	public bool showVitals;

	public AudioSource heartbeatLoop;
	public AudioSource breathingLoop;

	private void Update()
	{
		if (dead)
			return;

		//if (Input.GetButtonDown("Vitals"))
		//	showVitals = !showVitals;

		//UIManager.SetWatchRaised(showVitals);

		stopHealthRegen.Increment(Time.deltaTime);
		stopStaminaRegen.Increment(Time.deltaTime);

		takeDamageTimer.Increment(Time.deltaTime);

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
			if (currentStamina < maxStamina)
				currentStamina += Mathf.Min(staminaRegen * deltaTime, maxStamina - currentStamina);
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
		UIManager.SetCurrentHealth(currentHealth / maxHealth);
		UIManager.SetCurrentStamina(currentStamina / maxStamina);

		if (dead)
		{
			heartbeatLoop.volume = 0;
			breathingLoop.volume = 0;

			UIManager.SetDie(true);
			UIManager.SetShowVitals(false);
		}
		else
		{
			float heartbeatVolume = (1 - currentHealth / maxHealth);
			heartbeatVolume = Mathf.Clamp01((heartbeatVolume - 0.5f) * 2);
			float heartbeatPitch = heartbeatVolume * heartbeatVolume;
			float damageAmount = 1 - (1 - heartbeatVolume) * (1 - heartbeatVolume) * (1 - heartbeatVolume);
			heartbeatVolume = 1 - (1 - heartbeatVolume) * (1 - heartbeatVolume);

			heartbeatLoop.volume = heartbeatVolume * 0.6f;
			heartbeatLoop.pitch = 0.99f + heartbeatPitch * 0.21f;

			float breathingVolume = (1 - currentStamina / maxStamina);
			breathingVolume = Mathf.Clamp01((breathingVolume - 0.5f) * 2f);
			breathingVolume = 1 - (1 - breathingVolume) * (1 - breathingVolume);

			breathingLoop.volume = breathingVolume * 0.15f;
			breathingLoop.pitch = 1.04f;

			UIManager.SetDie(false);
			if (currentStamina >= maxStamina - 0.01)
				UIManager.SetShowVitals(false);
			else
				UIManager.SetShowVitals(true);
		}
	}

	public void DealDamage(float amount)
	{
		if (dead)
			return;

		//UseStamina(amount, false, true);

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

		if (takeDamageTimer.Expired())
		{
			takeDamageTimer.Reset();
			AudioManager.PlaySound(damagedSound, transform.position);
		}
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
		if (!canNearDie || finalHealth < -initialHealth || initialHealth < maxHealth / 5f)
			Die();
		else
			NearDie();
	}

	private void Die()
	{
		AudioManager.PlayMusicCue(AudioManager.CueType.Die);

		Player.Instance.Die();

		dead = true;

		UpdateUIUX();
	}

	private void NearDie()
	{
		if (nearDeathSound)
			AudioManager.PlaySound(nearDeathSound, transform.position);

		canNearDie = false;

		currentHealth = 1;
		//currentStamina = 0;
		stopHealthRegen.Reset(0);
		stopStaminaRegen.Reset(0.4f);

		UpdateUIUX();
	}

	public void Respawn()
	{
		dead = false;

		currentHealth = maxHealth;
		currentStamina = maxStamina;
		canNearDie = true;

		UpdateUIUX();
	}
}
