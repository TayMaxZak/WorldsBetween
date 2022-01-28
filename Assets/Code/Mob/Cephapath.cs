using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cephapath : MonoBehaviour
{
	[System.Serializable]
	public class Tentacle
	{
		public LineRenderer line;

		public float width = 1;

		[System.NonSerialized]
		public Vector3 targetPoint;
		[System.NonSerialized]
		public Vector3 targetPointNew;

		[System.NonSerialized]
		public Vector3 offsetDir;
		[System.NonSerialized]
		public Vector3 offsetDirNew;

		[System.NonSerialized]
		public Vector3 curveTipPoint;

		[System.NonSerialized]
		public bool grabbing;
		[System.NonSerialized]
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
	public float farSpeed = 1; // When past engage range
	public float speed = 1;
	public float dashSpeed = 10;

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

	[Header("Timings")]
	[SerializeField]
	private Timer damageTimer = new Timer(0.1f);

	[SerializeField]
	private Timer dashTimer = new Timer(11f);

	[SerializeField]
	private Timer reconfigureTimer = new Timer(5f);

	[SerializeField]
	private Timer soonestGrabTimer = new Timer(3f);

	[Header("Tweaks")]
	[SerializeField]
	private float dashVibrate = 0.12f;
	[SerializeField]
	private float tentacleWaveSpeed = 2.5f;

	// For dashing or wandering randomly
	private Vector3 randomMoveDir;
	private Vector3 randomMoveDirNew;

	private Vector3 smoothDir;
	private Vector3 lookDir;

	private float vibrateMult;

	// Handles smooth dashing
	private float curDashFuel = 0;
	private float backdashFuel = 0;
	private bool dashBlink = false;

	// Actual timer max times, in case they get randomized
	private float dashMaxTime;
	private float reconfigureMaxTime;

	private float slowDownMult = 1; // Goes into effect when it gets close to the player

	// State //
	private Vector3 initPosition; // Where did it start? Returns here after killing the player

	private bool playerDeadReset = true;

	private void Awake()
	{
		initPosition = transform.position;

		grabLoop.volume = 0;
		damageLoop.volume = 0;
		foreach (Tentacle t in tentacles)
		{
			t.width = SeedlessRandom.NextFloatInRange(0.4f, 0.8f);

			InitTentacle(t);
		}

		// Reset all timers
		dashTimer.Reset(1 + SeedlessRandom.NextFloat() * dashTimer.maxTime);
		reconfigureTimer.Reset(1 + SeedlessRandom.NextFloat() * reconfigureTimer.maxTime);
	}

	private void InitTentacle(Tentacle t)
	{
		// Initialize previous values manually
		t.targetPointNew = transform.position + SeedlessRandom.RandomPoint(grabDistance).normalized;
		t.offsetDirNew = SeedlessRandom.RandomPoint(6);

		t.targetPoint = transform.position + SeedlessRandom.RandomPoint(grabDistance).normalized;
		t.curveTipPoint = transform.position + SeedlessRandom.RandomPoint(grabDistance);

		MoveTentacle(t);

		// Manually lerp over to the new target point
		t.targetPoint = t.targetPointNew;
		// Same for curve
		t.curveTipPoint = Vector3.Lerp(t.curveTipPoint, t.targetPoint, 0.4f);

		RenderTentacle(t);
	}

	[ContextMenu("Position Tentacles")]
	private void SceneInitTentacles()
	{
		foreach (Tentacle t in tentacles)
		{
			InitTentacle(t);
		}
	}

	// TODO: Add "split" move, where it becomes smaller and makes copies of itself!
	private void Update()
	{
		if (!GameManager.Instance.finishedLoading)
			return;

		bool moveTentacles = false;

		// Vector towards player
		Vector3 diff = playerMover.position - transform.position;
		float distance = diff.magnitude;
		Vector3 dir = diff.normalized;
		if (smoothDir == Vector3.zero)
			smoothDir = dir;

		// Close enough to do bad things
		intensity = Mathf.Clamp01(1 - Mathf.Max(distance - stopDistance, 0) / damageDistance);
		intensity *= intensity;

		// Deal continuous damage
		damageTimer.Increment(Time.deltaTime);
		if (damageTimer.Expired())
		{
			playerVitals.DealDamage(damage * intensity);

			damageTimer.Reset();
		}

		// Handle player interaction
		if (!playerVitals.dead)
		{
			// Then, gradually reel them in
			if (playerMover.grabbed && PhysicsManager.Instance.physicsTicking)
			{
				playerMover.AddVelocity(-dir * knockbackReelYankSpeeds.y * Mathf.Lerp(0.5f + reelStrength, 1, 0.5f) * PhysicsManager.Instance.tickingDelta);
				// Reel less over time
				reelStrength = Mathf.Lerp(reelStrength, 0, PhysicsManager.Instance.tickingDelta / 2);
			}

			// Update audio
			grabLoop.volume = (1 - distance / grabDistance) * 0.5f;
			grabLoop.pitch = 1 + intensity * 0.3f;
			damageLoop.volume = intensity * 0.3f;

			playerDeadReset = true;
		}
		// Player is dead
		else if (playerDeadReset)
		{
			playerDeadReset = false;

			grabLoop.volume = 0;
			damageLoop.volume = 0;

			// Reset
			transform.position = initPosition;

			playerMover.grabbed = false;

			foreach (Tentacle t in tentacles)
				MoveTentacle(t);

			// Recalc vectors after reset
			diff = playerMover.position - transform.position;
			distance = diff.magnitude;
			dir = diff.normalized;
		}

		// For a more curved path towards player
		smoothDir = Vector3.Lerp(smoothDir, dir, Time.deltaTime);

		// Use all moves and go forward at normal speed
		float backdashCutoff = 1f;
		if (!playerVitals.dead)
		{
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
					dashBlink = true;

					randomMoveDirNew = SeedlessRandom.RandomPoint(1).normalized;

					// Double dash!
					if (dashMaxTime >= 0.99f && !playerMover.grabbed && SeedlessRandom.NextFloat() < 0.1f)
						dashMaxTime = 0.9f;
					// Normal dashing
					else
						dashMaxTime = dashTimer.maxTime * (0.5f + SeedlessRandom.NextFloat());
					dashTimer.Reset(dashMaxTime);
				}

				// Which way to dash
				Vector3 dashDir = dashMaxTime >= 0.99f ? (dir + randomMoveDir * 0.5f).normalized : randomMoveDir;

				// Strafing dash mult
				float strafe = 1 - Mathf.Abs(Vector3.Dot(dashDir, dir));
				float dashMult = Mathf.Lerp(strafe, 1, 1f);

				// Smoothly change random dash direction
				randomMoveDir = Vector3.Lerp(randomMoveDir, randomMoveDirNew, Time.deltaTime).normalized;

				float fuelConsume = 1f;

				// Translate by dash speed
				if (curDashFuel > 0)
				{
					// Also slow down dash (but not as much)
					float slowDownDashMult = Mathf.Clamp01(distance - stopDistance);
					slowDownDashMult = Mathf.Lerp(slowDownDashMult, 1, strafe);

					// Blink forwards at start of dash
					float blinkLength = 0;
					if (dashBlink)
					{
						dashBlink = false;

						blinkLength = 0.08f;
						curDashFuel -= blinkLength / 2;
					}

					// Slow down towards end of dash
					float deccelMult = Mathf.Lerp(Mathf.Min(curDashFuel, 1), 0.5f, 0.5f);
					transform.position += (Mathf.Clamp01(distance) * dashDir * slowDownDashMult * dashMult * dashSpeed * (blinkLength + deccelMult * Time.deltaTime));
				}

				// Don't get too close
				float newSlowdownMult = Mathf.Clamp01(distance - (playerMover.grabbed ? stopDistance : damageDistance / 2));
				slowDownMult = Mathf.Lerp(slowDownMult, newSlowdownMult, Time.deltaTime * 2);

				// Normal movement
				//if (!playerMover.grabbed)
				transform.position += (Mathf.Clamp01(distance) * smoothDir * slowDownMult * speed * Time.deltaTime);

				// Use up dash
				curDashFuel -= Time.deltaTime * fuelConsume;

				// "Backdash" after reaching destination
				if (curDashFuel <= 0 && (curDashFuel > -backdashCutoff || backdashFuel > 0.005f))
				{
					// Ramp up
					if (curDashFuel > -backdashCutoff / 2)
						backdashFuel = Mathf.Min(-curDashFuel, 0);
					// Slow down
					else
						backdashFuel = Mathf.Lerp(backdashFuel, 0, Time.deltaTime);

					transform.position += (Mathf.Clamp01(distance) * dashDir * dashMult * dashSpeed * backdashFuel * Time.deltaTime);
				}

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
					if (reconfigureMaxTime < reconfigureTimer.maxTime / 3)
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
			// Approach in a straight line instead
			else if (distance <= maxDistance)
			{
				transform.position += (smoothDir * farSpeed * Time.deltaTime);
			}
		}

		// When iterating over tentacles, determine if player was just released
		bool wasGrabbed = playerMover.grabbed;
		bool nowNotGrabbed = true;

		// Vibrate when about to dash
		float newVibrateMult = distance <= engageDistance ? 1 - Mathf.Clamp01(dashTimer.currentTime / dashMaxTime) : 0;
		newVibrateMult *= newVibrateMult;
		vibrateMult = Mathf.Lerp(vibrateMult, newVibrateMult, Time.deltaTime * 3);

		// Handle all tentacles
		for (int i = 0; i < tentacles.Count; i++)
		{
			Tentacle t = tentacles[i];

			// Main tentacle logic
			float closeOrFar = Mathf.Clamp01((distance - engageDistance) / (maxDistance - engageDistance));
			if (distance <= maxDistance)
			{
				// Move tips along if still just approaching player
				if (distance > engageDistance)
					t.targetPointNew += dir * 0.5f * farSpeed * Time.deltaTime;

				// Rotate tentacles around dir axis
				float rotSpeed = Mathf.Lerp(25, 50, closeOrFar);

				// Spin it faster if shorter
				float sizeMult = 1 - Mathf.Clamp01((t.targetPoint - transform.position).magnitude / grabDistance);
				sizeMult = Mathf.Max(1 + sizeMult, 2f);

				// Some tentacles go slower or faster, some in opposite directions
				int mod = (i % 5);
				float randomDirection = mod == 0 ? -0.25f : (mod / 4f);

				t.targetPointNew = transform.position + Quaternion.AngleAxis(rotSpeed * sizeMult * randomDirection * Time.deltaTime, smoothDir) * (t.targetPointNew - transform.position);


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
					nowNotGrabbed &= !MoveTentacle(t, !playerVitals.dead && distance < grabDistance, moveTentacles, dir);
				}

				//// Also, yank player back now that this one broke off
				//if (wasGrabbed && nowNotGrabbed)
				//{
				//	playerMover.AddVelocity(-dir * pushReelYankSpeeds.z);
				//}
			}

			// Move towards new target point
			if (!t.movedOutOfSync)
				t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, 1 - reconfigureTimer.currentTime / reconfigureMaxTime);
			else
				t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, Time.deltaTime * 2);

			// Overall smoothing
			t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, Time.deltaTime);
			t.offsetDir = Vector3.Lerp(t.offsetDir, t.offsetDirNew, Time.deltaTime);

			// Drag behind tips to make a curve
			t.curveTipPoint = Vector3.Lerp(t.curveTipPoint, t.targetPoint, Time.deltaTime);


			RenderTentacle(t, dir, smoothDir, (1 - closeOrFar) * (1 - closeOrFar));
		}


		// Change body rotation
		Vector3 goalDir = curDashFuel > 0 ? randomMoveDir : dir;

		float rotAccel = Mathf.Lerp(7, 0.33f, 1 - Mathf.Clamp01(-curDashFuel));

		lookDir = Vector3.Lerp(lookDir, goalDir, rotAccel * Time.deltaTime * (1 + 3 * intensity));
		if (lookDir != Vector3.zero)
			transform.rotation = Quaternion.LookRotation(lookDir);
	}

	private void MoveTentacle(Tentacle t)
	{
		MoveTentacle(t, false, false, Vector3.zero);
	}

	private bool MoveTentacle(Tentacle t, bool doGrab, bool moveAll, Vector3 dir)
	{
		bool oldGrab = t.grabbing;
		if (doGrab)
		{
			// Roughly 100% chance to grab with at least one tentacle
			if (moveAll)
				t.grabbing = SeedlessRandom.NextFloat() < 1f / tentacles.Count;
			else
				t.grabbing &= SeedlessRandom.NextFloat() < 1f / tentacles.Count;

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

		t.offsetDirNew = Vector3.Lerp(t.offsetDirNew, SeedlessRandom.RandomPoint(6), 0.5f);

		return t.grabbing;
	}

	private void RenderTentacle(Tentacle t)
	{
		RenderTentacle(t, Vector3.zero, Vector3.zero, 0);
	}

	private void RenderTentacle(Tentacle t, Vector3 dir, Vector3 smoothDir, float sharpness)
	{
		// Perpindicular to dir
		Vector3 perpin = Vector3.Cross(dir, (t.targetPoint - transform.position).normalized);

		for (int i = 0; i < t.line.positionCount; i++)
		{
			float percent = (float)i / t.line.positionCount;

			float kneeStrength = (1 - Mathf.Abs(2 * percent - 1)) * sharpness;
			float curveStrength = percent * percent;
			float waveyStrength = 0.5f * Mathf.Lerp(curveStrength, 1, 0.25f) * Mathf.Sin(percent * grabDistance - Time.time * (tentacleWaveSpeed));

			Vector3 surfacePoint = transform.position + (t.targetPoint - transform.position).normalized * 0.5f;

			// Attach to player
			if (t.grabbing)
			{
				t.line.widthMultiplier = 0.6f * t.width;

				Vector3 baseShape = Vector3.Lerp(surfacePoint, playerMover.transform.position - Vector3.up * 0.75f + smoothDir, percent);

				t.line.SetPosition(i, baseShape + kneeStrength * kneeStrength * t.offsetDir + (1 - percent) * waveyStrength * perpin + kneeStrength * curveStrength * perpin);
			}
			// Attach to world position
			else
			{
				t.line.widthMultiplier = t.width;

				Vector3 vibrate = dashVibrate * sharpness * vibrateMult * vibrateMult * SeedlessRandom.NextFloatInRange(-1, 1) * perpin;

				Vector3 actualTip = Vector3.Lerp(t.targetPoint, t.curveTipPoint, curveStrength);

				Vector3 baseShape = Vector3.Lerp(surfacePoint, actualTip, percent);

				t.line.SetPosition(i, baseShape + kneeStrength * kneeStrength * t.offsetDir + waveyStrength * perpin + vibrate);
			}
		}
	}
}
