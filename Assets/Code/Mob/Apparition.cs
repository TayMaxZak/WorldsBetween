using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Apparition : MonoBehaviour
{
	[System.Serializable]
	public class Tentacle
	{
		public Vector3 targetPoint;
		public Vector3 offsetDir;
		public Vector3 offsetDir2;
		public LineRenderer line;
		public bool grabbing;
	}

	public AudioSource grabLoop;
	public AudioSource damageLoop;
	public Sound dash;

	public PlayerVitals playerVitals;
	public PlayerMover playerMover;

	public float speed = 1;
	public float dashSpeed = 10;

	public float damage = 10;

	public float damageRange = 16;
	public float grabRange = 32;

	public float maxDistance = 200;
	public float permaDashDistance = 120;

	private float intensity;

	public Vector3 randomMoveDir;
	public Vector3 randomMoveDir2;

	[SerializeField]
	private Timer damageTimer = new Timer(0.1f);

	[SerializeField]
	private Timer dashTimer = new Timer(10f);

	[SerializeField]
	private Timer grabTimer = new Timer(10f);

	[SerializeField]
	private List<Tentacle> tentacles;

	private void Awake()
	{
		grabLoop.volume = 0;
		damageLoop.volume = 0;
		foreach (Tentacle t in tentacles)
		{
			t.targetPoint = transform.position;
			for (int i = 0; i < t.line.positionCount; i++)
			{
				float percent = (float)i / t.line.positionCount;
				t.line.SetPosition(i, Vector3.Lerp(transform.position, t.targetPoint, percent));
			}
		}
	}

	private void Update()
	{
		if (!GameManager.Instance.finishedLoading)
			return;

		bool moveTentacles = false;

		Vector3 diff = playerMover.locator.transform.position - transform.position;
		float distance = diff.magnitude;

		if (distance > maxDistance)
			return;

		intensity = Mathf.Clamp01(1 - Mathf.Max(distance - 2, 0) / damageRange);
		intensity *= intensity;

		damageTimer.Increment(Time.deltaTime);
		if (damageTimer.Expired())
		{
			playerVitals.DealDamage(damage * intensity * intensity);

			damageTimer.Reset();
		}

		if (!playerVitals.dead)
		{
			grabLoop.volume = (1 - distance / grabRange) * 0.5f;
			damageLoop.volume = intensity * 0.5f;
		}
		else
		{
			grabLoop.volume = 0;
			damageLoop.volume = 0;
		}


		if (distance <= permaDashDistance)
		{
			dashTimer.Increment(Time.deltaTime);
			if (dashTimer.Expired())
			{
				AudioManager.PlaySound(dash, transform.position);

				transform.Translate(diff.normalized * dashSpeed);

				randomMoveDir = SeedlessRandom.RandomPoint(1);
				randomMoveDir2 = SeedlessRandom.RandomPoint(1);

				dashTimer.Reset(dashTimer.maxTime * (0.5f + Random.value));
			}
			else
			{
				randomMoveDir = Vector3.Lerp(randomMoveDir, randomMoveDir2, Time.deltaTime);

				transform.Translate((diff.normalized + randomMoveDir / 2) * speed * Time.deltaTime);
			}

			grabTimer.Increment(Time.deltaTime);
			if (grabTimer.Expired())
			{
				moveTentacles = true;

				playerMover.grabbed = false;

				grabTimer.Reset(grabTimer.maxTime * (0.5f + Random.value));
			}
		}
		else
			transform.Translate(diff.normalized * dashSpeed * Time.deltaTime);

		foreach (Tentacle t in tentacles)
		{
			if (moveTentacles)
			{
				if (!playerVitals.dead && distance < grabRange)
				{
					t.grabbing |= SeedlessRandom.NextFloat() < 1f / tentacles.Count;
					if (t.grabbing)
					{
						playerMover.grabbed = true;
						playerMover.SetVelocity(Vector3.zero);

						AudioManager.PlayMusicCue();
					}
				}
				else
					t.grabbing = false;

				t.targetPoint = transform.position + SeedlessRandom.RandomPoint(grabRange);
				t.offsetDir = SeedlessRandom.RandomPoint(4);
				t.offsetDir2 = SeedlessRandom.RandomPoint(7);
			}
			else
				t.offsetDir = Vector3.Lerp(t.offsetDir, t.offsetDir2, Time.deltaTime);

			float mult = Mathf.Clamp01(dashTimer.currentTime / dashTimer.maxTime);

			if (t.grabbing)
			{
				for (int i = 0; i < t.line.positionCount; i++)
				{
					float percent = (float)i / t.line.positionCount;
					float mid = (1 - Mathf.Abs(2 * percent - 1));
					t.line.SetPosition(i, Vector3.Lerp(transform.position, diff.normalized * 3 + playerMover.body.transform.position - Vector3.up, percent));
				}
			}
			else
			{
				for (int i = 0; i < t.line.positionCount; i++)
				{
					float percent = (float)i / t.line.positionCount;
					float mid = (1 - Mathf.Abs(2 * percent - 1));
					t.line.SetPosition(i, (SeedlessRandom.RandomPoint(0.5f) * mult + t.offsetDir) * mid * mid + Vector3.Lerp(transform.position, t.targetPoint, percent));
				}
			}
		}
	}
}
