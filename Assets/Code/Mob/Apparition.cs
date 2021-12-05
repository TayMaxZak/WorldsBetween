using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Apparition : MonoBehaviour
{
	[System.Serializable]
	public class Tentacle
	{
		public Vector3 targetPoint;
		public Vector3 targetPointNew;
		public Vector3 offsetDir;
		public Vector3 offsetDirNew;
		public LineRenderer line;
		public bool grabbing;
		public bool movedOutOfSyc;
	}

	public AudioSource grabLoop;
	public AudioSource damageLoop;
	public Sound dashSound;

	public PlayerVitals playerVitals;
	public PlayerMover playerMover;

	public float speed = 1;
	public float dashSpeed = 10;
	public float slowSpeed = 1;

	public float damage = 10;

	public float damageRange = 16;
	public float grabRange = 32;

	public float maxDistance = 200;
	public float engageDistance = 100;

	private float intensity;

	public Vector3 randomMoveDir;
	public Vector3 randomMoveDirNew;

	[SerializeField]
	private Timer damageTimer = new Timer(0.1f);

	[SerializeField]
	private Timer dashTimer = new Timer(11f);

	[SerializeField]
	private Timer reconfigureTent = new Timer(5f);

	[SerializeField]
	private List<Tentacle> tentacles;

	private float curDashFuel = 0;
	private float backdashFuel = 0;

	private float dashMaxTime;
	private float reconfigureTentMax;

	private Vector3 initialSpot;

	private void Awake()
	{
		initialSpot = transform.position;

		grabLoop.volume = 0;
		damageLoop.volume = 0;
		foreach (Tentacle t in tentacles)
		{
			// Initialize previous target point manually
			t.targetPoint = transform.position;

			MoveTentacle(t);

			// Manually fully lerp over to the new target point
			t.targetPoint = t.targetPointNew;

			RenderTentacle(t, Vector3.zero);
		}
	}

	// TODO: Add "split" move, where it becomes smaller and makes copies of itself!
	private void Update()
	{
		if (!GameManager.Instance.finishedLoading)
			return;

		bool moveTentacles = false;

		// Vector towards player
		Vector3 diff = playerMover.locator.transform.position - transform.position;
		float distance = diff.magnitude;
		Vector3 dir = diff.normalized;

		// Too far, don't do anything
		if (distance > maxDistance)
		{
			return;
		}

		// Close enough to do bad things
		intensity = Mathf.Clamp01(1 - Mathf.Max(distance - 2, 0) / damageRange);
		intensity *= intensity;

		// Deal continuous damage
		damageTimer.Increment(Time.deltaTime);
		if (damageTimer.Expired())
		{
			playerVitals.DealDamage(damage * intensity * intensity);

			damageTimer.Reset();
		}

		// Update audio
		if (!playerVitals.dead)
		{
			grabLoop.volume = (1 - distance / grabRange) * 0.5f;
			grabLoop.pitch = 1 + intensity * 0.3f;
			damageLoop.volume = intensity * 0.5f;
		}
		// Player is dead
		else
		{
			grabLoop.volume = 0;
			damageLoop.volume = 0;

			// Reset
			transform.position = initialSpot;

			playerMover.grabbed = false;

			// Recalc vectors after reset
			diff = playerMover.locator.transform.position - transform.position;
			distance = diff.magnitude;

			// Too far, don't do anything
			if (distance > maxDistance)
			{
				return;
			}
		}

		// Then, gradually reel them in
		if (playerMover.grabbed && playerMover.ticking)
		{
			playerMover.AddVelocity(-dir * playerMover.tickingDelta * 0.5f);
		}

		// Use all moves and go forward at normal speed
		if (distance <= engageDistance)
		{
			// Use dash move
			dashTimer.Increment(Time.deltaTime);
			if (dashTimer.Expired())
			{
				if (dashSound)
					AudioManager.PlaySound(dashSound, transform.position);

				// Reset remaining dash
				curDashFuel = 0.5f + SeedlessRandom.NextFloat() * 1.5f;

				//randomMoveDir = SeedlessRandom.RandomPoint(1).normalized;
				randomMoveDirNew = SeedlessRandom.RandomPoint(1).normalized;

				dashMaxTime = dashTimer.maxTime * (0.5f + SeedlessRandom.NextFloat());
				dashTimer.Reset(dashMaxTime);
			}
			// Not dashing
			else
			{
				randomMoveDir = Vector3.Lerp(randomMoveDir, randomMoveDirNew, Time.deltaTime).normalized;

				float backdashCutoff = 1f;
				float fuelConsume = 1f;

				// Main dash
				if (curDashFuel > 0)
				{
					transform.Translate((dir + randomMoveDir).normalized * dashSpeed * Mathf.Lerp(Mathf.Min(curDashFuel, 1), 0.5f, 0.5f) * Time.deltaTime);
				}
				else
				{
					transform.Translate(dir * speed * Time.deltaTime);
				}
				curDashFuel -= Time.deltaTime * fuelConsume;

				//// "Backdash" after reaching destination
				//if (curDashFuel <= 0 && (curDashFuel > -backdashCutoff || backdashFuel > 0.005f))
				//{
				//	// Ramp up
				//	if (curDashFuel > -backdashCutoff / 2)
				//		backdashFuel = Mathf.Min(-curDashFuel, 0);
				//	// Slow down
				//	else
				//		backdashFuel = Mathf.Lerp(backdashFuel, 0, Time.deltaTime);

				//	transform.Translate(-(dir + randomMoveDir.normalized) * dashSpeed * backdashFuel * Time.deltaTime);
				//}
			}

			// Reconfigure tentacles
			reconfigureTent.Increment(Time.deltaTime);
			if (reconfigureTent.Expired())
			{
				moveTentacles = true;

				// Finally, yank them back one last time
				if (playerMover.grabbed)
					playerMover.AddVelocity(-dir);
				playerMover.grabbed = false;

				reconfigureTentMax = reconfigureTent.maxTime * (0.5f + SeedlessRandom.NextFloat());
				reconfigureTent.Reset(reconfigureTentMax);
			}
		}
		// Slowly approach instead
		else
		{
			transform.Translate(dir * slowSpeed * Time.deltaTime);

			foreach (Tentacle t in tentacles)
			{
				t.targetPointNew += dir * slowSpeed * Time.deltaTime;
				t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, Time.deltaTime);
			}
		}

		// Handle all tentacles
		foreach (Tentacle t in tentacles)
		{
			// Does this tentacle need to be adjusted now?
			bool moveNow = (t.targetPoint - transform.position).sqrMagnitude > grabRange * grabRange;
			// If not moving all of them, remember that this adjusted out of sync
			if (!moveTentacles)
				t.movedOutOfSyc |= moveNow;
			// Set new adjusted state
			else
				t.movedOutOfSyc = false;


			// Change tentacle positions
			if (moveTentacles || moveNow)
				MoveTentacle(t, !playerVitals.dead && distance < grabRange, dir);

			// Move towards new target point
			if (!t.movedOutOfSyc)
				t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, 1 - reconfigureTent.currentTime / reconfigureTentMax);
			else
				t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, Time.deltaTime * 2);

			// Overall smoothing
			t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, Time.deltaTime);
			t.offsetDir = Vector3.Lerp(t.offsetDir, t.offsetDirNew, Time.deltaTime);


			RenderTentacle(t, dir);
		}
	}

	private void MoveTentacle(Tentacle t)
	{
		MoveTentacle(t, false, Vector3.zero);
	}

	private void MoveTentacle(Tentacle t, bool doGrab, Vector3 dir)
	{
		if (doGrab)
		{
			t.grabbing |= SeedlessRandom.NextFloat() < 0.5f / tentacles.Count;
			if (t.grabbing)
			{
				playerMover.grabbed = true;

				// First, push player forward (pull in later)
				playerMover.AddVelocity(dir);

				// Player in combat
				AudioManager.PlayMusicCue();
			}
		}
		else
			t.grabbing = false;

		t.targetPointNew = Vector3.Lerp(t.targetPointNew, transform.position + SeedlessRandom.RandomPoint(grabRange), 0.5f);

		t.offsetDirNew = Vector3.Lerp(t.offsetDir, SeedlessRandom.RandomPoint(6), 0.4f);
	}

	private void RenderTentacle(Tentacle t, Vector3 dir)
	{
		//float vibrateMult = 1 - Mathf.Abs(2 * Mathf.Clamp01(dashTimer.currentTime / dashMax) - 1);
		float vibrateMult = 0;

		for (int i = 0; i < t.line.positionCount; i++)
		{
			float percent = (float)i / t.line.positionCount;
			float mid = (1 - Mathf.Abs(2 * percent - 1));
			float wavey = Mathf.Sin(percent * grabRange + Time.time * 3) * 0.6f;

			Vector3 surface = transform.position + (t.targetPoint - transform.position).normalized * 0.5f;

			if (t.grabbing)
			{
				t.line.SetPosition(i, Vector3.Lerp(surface, playerMover.body.transform.position + dir, percent) + wavey * 0.25f * t.offsetDir.normalized);
			}
			else
			{
				Vector3 vibrate = 0 * Mathf.Clamp01(dashTimer.currentTime / dashMaxTime) * (SeedlessRandom.RandomPoint(0.3f) * vibrateMult * vibrateMult + t.offsetDir) * mid * mid;

				t.line.SetPosition(i, Vector3.Lerp(surface, t.targetPoint, percent) + wavey * t.offsetDir.normalized + vibrate);
			}
		}
	}
}
