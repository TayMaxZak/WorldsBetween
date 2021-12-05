using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Apparition : MonoBehaviour
{
	[System.Serializable]
	public class Tentacle
	{
		public LineRenderer line;

		public Vector3 targetPoint;
		public Vector3 targetPointNew;

		public Vector3 offsetDir;
		public Vector3 offsetDirNew;

		public Vector3 curveTipPoint;

		public bool grabbing;
		public bool movedOutOfSync;
	}

	[Header("References")]
	public AudioSource grabLoop;
	public AudioSource damageLoop;
	public Sound dashSound;

	public PlayerVitals playerVitals;
	public PlayerMover playerMover;

	[SerializeField]
	private List<Tentacle> tentacles;

	[Header("Stats")]
	public float speed = 1;
	public float dashSpeed = 10;
	public float slowSpeed = 1; // When past engage range

	public float damage = 10;

	[SerializeField]
	private Vector3 knockbackReelYankSpeeds = new Vector3(2, 0.5f, 2); // X = push forward speed at start, Y = constant pull back speed in middle, Z = pull back speed at end
	private float reelStrength;

	[Header("Ranges")]
	public float maxDistance = 200;
	public float engageDistance = 100;
	public float grabDistance = 32;
	public float damageDistance = 16;
	public float stopDistance = 5;

	private float intensity;

	// For dashing or wandering randomly
	private Vector3 randomMoveDir;
	private Vector3 randomMoveDirNew;
	private Vector3 smoothDir;

	[Header("Timings")]
	[SerializeField]
	private Timer damageTimer = new Timer(0.1f);

	[SerializeField]
	private Timer dashTimer = new Timer(11f);

	[SerializeField]
	private Timer reconfigureTimer = new Timer(5f);

	[SerializeField]
	private Timer soonestGrabTimer = new Timer(3f);

	// Handles smooth dashing
	private float curDashFuel = 0;
	private float backdashFuel = 0;
	private bool dashBlink = false;

	// Actual timer max times, in case they get randomized
	private float dashMaxTime;
	private float reconfigureMaxTime;

	private float slowDownMult = 1; // Goes into effect when it gets close to the player

	private Vector3 initPosition; // Where did it start? Returns here after killing the player

	private void Awake()
	{
		initPosition = transform.position;

		grabLoop.volume = 0;
		damageLoop.volume = 0;
		foreach (Tentacle t in tentacles)
		{
			// Initialize previous values manually
			t.targetPointNew = transform.position;
			t.offsetDirNew = SeedlessRandom.RandomPoint(6);

			MoveTentacle(t);

			// Manually fully lerp over to the new target point
			t.targetPoint = t.targetPointNew;
			t.curveTipPoint = t.targetPoint + SeedlessRandom.RandomPoint(2);

			RenderTentacle(t, Vector3.zero);
		}

		// Reset all timers
		damageTimer.Reset();
		dashTimer.Reset();
		reconfigureTimer.Reset();
		soonestGrabTimer.Reset();
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
		if (smoothDir == Vector3.zero)
			smoothDir = dir;

		// Too far, don't do anything
		if (distance > maxDistance)
		{
			return;
		}

		// Close enough to do bad things
		intensity = Mathf.Clamp01(1 - Mathf.Max(distance - stopDistance, 0) / damageDistance);
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
			grabLoop.volume = (1 - distance / grabDistance) * 0.5f;
			grabLoop.pitch = 1 + intensity * 0.3f;
			damageLoop.volume = intensity * 0.3f;
		}
		// Player is dead
		else
		{
			grabLoop.volume = 0;
			damageLoop.volume = 0;

			// Reset
			transform.position = initPosition;

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
		if (playerMover.grabbed && playerMover.ticking && reelStrength > 0.02f)
		{
			playerMover.AddVelocity(-dir * knockbackReelYankSpeeds.y * reelStrength * playerMover.tickingDelta);
			// Gradually reel less
			reelStrength = Mathf.Lerp(reelStrength, 0, playerMover.tickingDelta / 4);
		}

		// Use all moves and go forward at normal speed
		if (distance <= engageDistance)
		{
			// Dash twice as far if perpindicular to dir
			Vector3 dashDir = (dir / 2 + randomMoveDir * 2).normalized;
			float strafe = 1 - Mathf.Abs(Vector3.Dot(dashDir, dir));
			float dashMult = Mathf.Lerp(strafe, 1, 0.25f);

			// Use dash move
			dashTimer.Increment(Time.deltaTime);
			if (dashTimer.Expired())
			{
				if (dashSound && dashMult > 0.5f)
					AudioManager.PlaySound(dashSound, transform.position);

				// Reset remaining dash
				curDashFuel = 0.5f + SeedlessRandom.NextFloat() * 1.5f;
				dashBlink = true;

				//randomMoveDir = SeedlessRandom.RandomPoint(1).normalized;
				randomMoveDirNew = SeedlessRandom.RandomPoint(1).normalized;

				dashMaxTime = dashTimer.maxTime * (0.5f + SeedlessRandom.NextFloat());
				dashTimer.Reset(dashMaxTime);
			}

			// Smoothly wander
			randomMoveDir = Vector3.Lerp(randomMoveDir, randomMoveDirNew, Time.deltaTime).normalized;

			//float backdashCutoff = 1f;
			float fuelConsume = 1f;

			// Translate by dash speed
			if (curDashFuel > 0)
			{
				// Also slow down dash (but not as much)
				float slowDownDashMult = Mathf.Clamp01(distance - stopDistance);
				slowDownDashMult = Mathf.Lerp(slowDownDashMult, 1, strafe);

				// Blink forwards at start of dash
				float blink = dashBlink ? dashSpeed * 0.2f : 0;
				dashBlink = false;

				// Slow down towards end of dash
				transform.Translate(Mathf.Clamp01(distance) * dashDir * slowDownDashMult * dashMult * (blink + dashSpeed * Mathf.Lerp(Mathf.Min(curDashFuel, 1), 0.5f, 0.5f) * Time.deltaTime));
			}

			// Don't get too close
			float newSlowdownMult = Mathf.Clamp01(distance - (playerMover.grabbed ? stopDistance : damageDistance / 2));
			slowDownMult = Mathf.Lerp(slowDownMult, newSlowdownMult, Time.deltaTime * 2);

			// Normal movement
			smoothDir = Vector3.Lerp(smoothDir, dir, Time.deltaTime * 2);
			transform.Translate(Mathf.Clamp01(distance) * smoothDir * slowDownMult * speed * Time.deltaTime);

			// Use up dash
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

			soonestGrabTimer.Increment(Time.deltaTime);

			// Reconfigure tentacles
			reconfigureTimer.Increment(Time.deltaTime);
			if (reconfigureTimer.Expired())
			{
				moveTentacles = true;

				// Finally, yank player back one last time
				if (playerMover.grabbed)
					playerMover.AddVelocity(-dir * knockbackReelYankSpeeds.z);
				playerMover.grabbed = false;

				// Was last one fast? Then, make this slow
				if (reconfigureMaxTime < reconfigureTimer.maxTime / 2)
					reconfigureMaxTime = reconfigureTimer.maxTime * (1 + 2 * SeedlessRandom.NextFloat());
				// Otherwise, highly random timing
				else
				{
					float min = 0.1f;
					reconfigureMaxTime = reconfigureTimer.maxTime * (min + SeedlessRandom.NextFloat() * (1 - min));
				}
				reconfigureTimer.Reset(reconfigureMaxTime);
			}
		}
		// Slowly approach instead
		else
		{
			transform.Translate(dir * slowSpeed * Time.deltaTime);

			for (int i = 0; i < tentacles.Count; i++)
			{
				Tentacle t = tentacles[i];

				t.targetPointNew += dir * slowSpeed * Time.deltaTime;

				// Rotate tentacles around dir axis
				float spinSpeed = 60;

				float distMult = (distance - engageDistance) / (maxDistance - engageDistance);

				float sizeMult = Mathf.Clamp01((t.targetPoint - transform.position).magnitude / grabDistance);
				sizeMult = 1 - (1 - sizeMult) * (1 - sizeMult);

				float randomDirection = i % 3 == 0 ? -1 : 1;

				t.targetPointNew = transform.position + Quaternion.AngleAxis(spinSpeed * distMult * sizeMult * randomDirection * Time.deltaTime, dir) * (t.targetPointNew - transform.position);
			}
		}

		// Determine if player was just released
		bool wasGrabbed = playerMover.grabbed;
		bool nowNotGrabbed = true;

		// Handle all tentacles
		foreach (Tentacle t in tentacles)
		{
			// Does this tentacle need to be adjusted now?
			bool moveNow = (t.targetPoint - transform.position).sqrMagnitude > grabDistance * grabDistance;
			// If not moving all of them, remember that this adjusted out of sync
			if (!moveTentacles)
				t.movedOutOfSync |= moveNow;
			// Set new adjusted state
			else
				t.movedOutOfSync = false;

			// Change tentacle positions
			if (moveTentacles || moveNow)
			{
				// If even one tentacle is attached, no longer true
				nowNotGrabbed &= !MoveTentacle(t, !playerVitals.dead && distance < grabDistance, dir);
			}

			//// Also, yank player back now that this one broke off
			//if (wasGrabbed && nowNotGrabbed)
			//{
			//	playerMover.AddVelocity(-dir * pushReelYankSpeeds.z);
			//}

			// Move towards new target point
			if (!t.movedOutOfSync)
				t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, 1 - reconfigureTimer.currentTime / reconfigureMaxTime);
			else
				t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, Time.deltaTime * 2);

			// Overall smoothing
			t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, Time.deltaTime);
			t.offsetDir = Vector3.Lerp(t.offsetDir, t.offsetDirNew, Time.deltaTime);

			// Drag behind tips to make a curve
			t.curveTipPoint = Vector3.Lerp(t.curveTipPoint, t.targetPoint, Time.deltaTime / 2);


			RenderTentacle(t, dir);
		}
	}

	private void MoveTentacle(Tentacle t)
	{
		MoveTentacle(t, false, Vector3.zero);
	}

	private bool MoveTentacle(Tentacle t, bool doGrab, Vector3 dir)
	{
		bool oldGrab = t.grabbing;
		if (doGrab)
		{
			// Roughly 100% chance to grab with at least one tentacle
			t.grabbing |= SeedlessRandom.NextFloat() < 1f / tentacles.Count;

			// This tentacle is grabbing, player isn't already grabbed, not repeating this grab, and grab timer is ready
			if (t.grabbing && !playerMover.grabbed && !oldGrab && soonestGrabTimer.Expired())
			{
				soonestGrabTimer.Reset();

				playerMover.grabbed = true;

				// First, knockback player (pull in later)
				playerMover.AddVelocity(dir * knockbackReelYankSpeeds.x);
				// Reset reel strength
				reelStrength = 1;

				// Feedback that player is now in combat
				AudioManager.PlayMusicCue();
			}
		}
		else
			t.grabbing = false;

		t.targetPointNew = Vector3.Lerp(t.targetPointNew, transform.position + SeedlessRandom.RandomPoint(grabDistance), 0.8f);

		t.offsetDirNew = Vector3.Lerp(t.offsetDirNew, SeedlessRandom.RandomPoint(6), 0.4f);

		return t.grabbing;
	}

	private void RenderTentacle(Tentacle t, Vector3 dir)
	{
		//float vibrateMult = 1 - Mathf.Abs(2 * Mathf.Clamp01(dashTimer.currentTime / dashMax) - 1);
		float vibrateMult = 0;

		for (int i = 0; i < t.line.positionCount; i++)
		{
			float percent = (float)i / t.line.positionCount;

			float kneeStrength = (1 - Mathf.Abs(2 * percent - 1));
			float curveStrength = percent * percent;
			float wavey = Mathf.Sin(percent * grabDistance - Time.time * (2.5f + vibrateMult * 1.5f)) * 0.75f;
			float waveyStrength = Mathf.Lerp(curveStrength, 1, 0.33f);

			Vector3 surfacePoint = transform.position + (t.targetPoint - transform.position).normalized * 1.5f;

			// Attach to player
			if (t.grabbing)
			{
				t.line.SetPosition(i, Vector3.Lerp(surfacePoint, playerMover.body.transform.position - Vector3.up * 0.75f + dir, percent) + kneeStrength * kneeStrength * t.offsetDir + waveyStrength * wavey * 0.5f * t.offsetDir.normalized);
			}
			// Attach to world position
			else
			{
				//Vector3 vibrate = Mathf.Clamp01(dashTimer.currentTime / dashMaxTime) * (SeedlessRandom.RandomPoint(0.3f) * vibrateMult * vibrateMult + t.offsetDir) * mid * mid;
				Vector3 vibrate = Vector3.zero;
				//Vector3 curve = t.curveDir.normalized * curveStrength;
				Vector3 actualTip = Vector3.Lerp(t.targetPoint, t.curveTipPoint, curveStrength);

				t.line.SetPosition(i, Vector3.Lerp(surfacePoint, actualTip, percent) + kneeStrength * kneeStrength * t.offsetDir + waveyStrength * wavey * t.offsetDir.normalized + vibrate);
			}
		}
	}
}
