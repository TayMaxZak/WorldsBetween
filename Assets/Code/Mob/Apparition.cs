using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Apparition : MonoBehaviour
{
	public PlayerVitals playerVitals;
	public PlayerMover playerMover;

	public float speed = 1;
	public float dashSpeed = 10;

	public float damage = 10;

	public float range = 16;

	public float maxDistance = 160;

	private float intensity;

	private Timer damageTimer = new Timer(0.1f);

	private Timer dashTimer = new Timer(10f);

	private void Update()
	{
		if (!GameManager.Instance.finishedLoading)
			return;

		Vector3 diff = playerMover.locator.transform.position - transform.position;
		float distance = diff.magnitude;
		intensity = Mathf.Clamp01(1 - Mathf.Max(distance - 5, 0) / range);
		intensity *= intensity;

		damageTimer.Increment(Time.deltaTime);
		if (damageTimer.Expired())
		{
			playerVitals.DealDamage(damage * intensity);

			damageTimer.Reset();
		}

		if (distance <= maxDistance)
		{
			dashTimer.Increment(Time.deltaTime);
			if (dashTimer.Expired())
			{
				transform.Translate(diff.normalized * dashSpeed);

				dashTimer.Reset(dashTimer.maxTime * (0.5f + Random.value));
			}
			else
			transform.Translate(diff.normalized * speed * Time.deltaTime);
		}
		else
			transform.Translate(diff.normalized * dashSpeed * Time.deltaTime);
	}
}
