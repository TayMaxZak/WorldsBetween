using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Apparition : MonoBehaviour
{
	[System.Serializable]
	public class Tentacle
	{
		public Vector3 targetPoint;
		public LineRenderer line;
		public bool grabbing;
	}

	public PlayerVitals playerVitals;
	public PlayerMover playerMover;

	public float speed = 1;
	public float dashSpeed = 10;

	public float damage = 10;

	public float damageRange = 16;
	public float grabRange = 32;

	public float maxDistance = 160;

	private float intensity;

	[SerializeField]
	private Timer damageTimer = new Timer(0.1f);

	[SerializeField]
	private Timer dashTimer = new Timer(10f);

	[SerializeField]
	private List<Tentacle> tentacles;

	private void Awake()
	{
		foreach (Tentacle t in tentacles)
		{
			t.targetPoint = transform.position;
		}
	}

	private void Update()
	{
		if (!GameManager.Instance.finishedLoading)
			return;

		bool moveTentacles = false;

		Vector3 diff = playerMover.locator.transform.position - transform.position;
		float distance = diff.magnitude;
		intensity = Mathf.Clamp01(1 - Mathf.Max(distance - 2, 0) / damageRange);
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

				moveTentacles = true;

				playerMover.grabbed = false;

				dashTimer.Reset(dashTimer.maxTime * (0.5f + Random.value));
			}
			else
				transform.Translate(diff.normalized * speed * Time.deltaTime);
		}
		else
			transform.Translate(diff.normalized * dashSpeed * Time.deltaTime);

		foreach (Tentacle t in tentacles)
		{
			if (moveTentacles)
			{
				if (distance < grabRange)
				{
					t.grabbing = SeedlessRandom.NextFloat() < 0.2f;
					if (t.grabbing)
						playerMover.grabbed = true;
				}
				else
					t.grabbing = false;

				t.targetPoint = transform.position + SeedlessRandom.RandomPoint(grabRange);
			}

			if (t.grabbing)
			{
				t.line.SetPosition(0, transform.position);
				t.line.SetPosition(1, playerMover.body.transform.position - Vector3.down * 0.5f);
			}
			else
			{
				t.line.SetPosition(0, transform.position);
				t.line.SetPosition(1, t.targetPoint);
			}
		}
	}
}
