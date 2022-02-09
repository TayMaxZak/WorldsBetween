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

		public Vector3 targetPoint;
		public Vector3 targetPointNew;

		public Vector3 offsetDir;
		public Vector3 offsetDirNew;

		public Vector3 curveTipPoint;

		public Actor toPull;

		public bool movedOutOfSync;
	}

	public bool dioramaMode = false;
	[Range(0,1)]
	public float dioramaSharpness = 0;
	[Range(0, 1)]
	public float dioramaWiggle = 0.1f;

	[Header("References")]
	public AudioSource grabLoop;
	public AudioSource damageLoop;
	public Sound dashSound;
	public Transform target;

	[SerializeField]
	[HideInInspector]
	private List<Tentacle> tentacles = new List<Tentacle>();

	[Header("Stats")]
	public float farSpeed = 1; // When past engage range
	public float speed = 1;
	public float dashSpeed = 10;

	public float damage = 10;
	public float grabChance = 0.75f;

	[SerializeField]
	private Vector3 knockbackReelYankSpeeds = new Vector3(2, 0.5f, 2); // X = push forward speed at start, Y = constant pull back speed in middle, Z = pull back speed at end
	private float reelStrength;

	[Header("Ranges")]
	public float maxDistance = 200;
	public float engageDistance = 100;
	public float grabDistance = 32;
	public float damageDistance = 16;
	public float stopDistance = 5;

	[Header("Timings")]
	[SerializeField]
	private float timeScale = 1;

	[SerializeField]
	private Timer damageTimer = new Timer(0.1f);

	[SerializeField]
	private Timer dashTimer = new Timer(11f);

	[SerializeField]
	private Timer randomDirTimer = new Timer(3f);

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
	private Vector3 dashDir;

	private float vibrateMult;

	// Handles smooth dashing
	private float curDashFuel = 0;

	// Actual timer max times, in case they get randomized
	private float dashMaxTime;

	private float slowDownMult = 1; // Goes into effect when it gets close to the player

	// State //
	private Vector3 initPosition; // Where did it start? Returns here after killing the player

	private bool playerDeadReset = true;

	private void Awake()
	{
		if (!enabled)
			return;

		if (!dioramaMode)
		{
			initPosition = transform.position;
			if (!target)
				target = Player.Instance.mover.transform;

			grabLoop.volume = 0;
			damageLoop.volume = 0;

			// Reset all timers
			dashTimer.Reset(1 + SeedlessRandom.NextFloat() * dashTimer.maxTime);
		}

		foreach (Tentacle t in tentacles)
		{
			InitTentacle(t);
		}
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
		tentacles.Clear();
		foreach (LineRenderer l in GetComponentsInChildren<LineRenderer>())
		{
			Tentacle t = new Tentacle() { line = l, width = l.widthMultiplier };
			tentacles.Add(t);
			InitTentacle(t);
		}
	}

	// TODO: Add "split" move, where it becomes smaller and makes copies of itself!
	private void Update()
	{
		if (dioramaMode)
		{
			for (int i = 0; i < tentacles.Count; i++)
			{
				Tentacle t = tentacles[i];

				if (true)
				{
					// Does this tentacle need to be adjusted now?
					bool moveNow = SeedlessRandom.NextFloat() > 1 - DeltaTime() * dioramaWiggle;

					// Change tentacle positions
					if (moveNow)
						MoveTentacle(t, transform.forward, true);
				}

				float delta = DeltaTime() * (t.toPull ? 5 : 1);

				// Move towards new target point
				t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, delta * 2);

				// Overall smoothing
				t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, delta);
				t.offsetDir = Vector3.Lerp(t.offsetDir, t.offsetDirNew, DeltaTime());

				// Drag behind tips to make a curve
				t.curveTipPoint = Vector3.Lerp(t.curveTipPoint, t.targetPoint, delta);

				RenderTentacle(t, transform.forward, transform.forward, dioramaSharpness);
			}

			return;
		}

		if (!GameManager.Instance.finishedLoading)
			return;

		bool moveTentacles = false;

		// Vector towards player
		if (!target)
			return;

		Vector3 diff = target.position - transform.position;
		float distance = diff.magnitude;
		Vector3 dir = diff.normalized;
		if (smoothDir == Vector3.zero)
			smoothDir = dir;

		// Close enough to do bad things
		float damageCloseness = 1 - Mathf.Clamp01(Mathf.Max(distance - stopDistance, 0) / damageDistance);
		float grabCloseness = 1 - Mathf.Clamp01(Mathf.Max(distance - grabDistance, 0) / grabDistance);
		//intensity *= intensity;

		// Deal continuous damage
		damageTimer.Increment(DeltaTime());
		if (damageTimer.Expired() && damageCloseness > 0.5f)
		{
			Player.Instance.vitals.DealDamage(damage);

			damageTimer.Reset();
		}

		// Handle player interaction
		// TODO: Repeated code
		if (!Player.Instance.vitals.dead)
		{
			// Update audio
			grabLoop.volume = (1 - distance / grabDistance) * 0.5f;
			grabLoop.pitch = 1 + damageCloseness * 0.3f;
			damageLoop.volume = damageCloseness * 0.3f;

			playerDeadReset = true;
		}
		// Player is dead
		else if (playerDeadReset)
		{
			playerDeadReset = false;

			PlayerDeadReset();

			// Recalc vectors after reset
			diff = target.position - transform.position;
			distance = diff.magnitude;
			dir = diff.normalized;
		}

		// For a more curved path towards player
		smoothDir = Vector3.Lerp(smoothDir, dir, DeltaTime());

		// Use all moves and go forward at normal speed
		// TODO: Repeated code
		if (!Player.Instance.vitals.dead)
		{
			if (distance <= engageDistance)
			{
				// Use dash move if not toooo close
				if (distance > grabDistance * 0.67f)
					dashTimer.Increment(DeltaTime());
				if (dashTimer.Expired())
				{
					if (dashSound)
						AudioManager.PlaySound(dashSound, transform.position);

					// Reset remaining dash
					curDashFuel = 1;

					dashMaxTime = dashTimer.maxTime * (0.5f + SeedlessRandom.NextFloat());
					dashTimer.Reset(dashMaxTime);

					dashDir = ((target.position + SeedlessRandom.RandomPoint(1).normalized * 5) - transform.position).normalized;

					foreach (Tentacle t in tentacles)
					{
						// Tentacles in front of us pull back, tentacles behind us push forward
						Vector3 inward = -(t.curveTipPoint - transform.position).normalized;
						Vector3 backward = -transform.forward;
						//float dirMult = -Vector3.Dot((t.curveTipPoint - transform.position).normalized, transform.forward);

						t.targetPointNew += 0.5f * dashSpeed * (inward + 2 * backward);
					}
				}

				// Which way to dash

				float fuelConsume = 1;

				// Translate by dash speed
				if (curDashFuel > 0)
				{
					// Also slow down dash (but not as much)
					float slowDownDashMult = Mathf.Clamp01(distance - stopDistance);

					// Slow down towards end of dash
					float deccelMult = Mathf.Lerp(Mathf.Min(curDashFuel, 1), 0.5f, 0.0f);
					transform.position += (deccelMult * deccelMult) * dashSpeed * Mathf.Clamp01(distance) * slowDownDashMult * DeltaTime() * dashDir;
				}

				// Don't get too close
				float newSlowdownMult = Mathf.Clamp01(distance - stopDistance);
				slowDownMult = Mathf.Lerp(slowDownMult, newSlowdownMult, DeltaTime() * 2);

				// Smoothly change random move dir direction
				randomDirTimer.Increment(DeltaTime());
				if (randomDirTimer.Expired())
				{
					// Orient closer to player as intensity increases
					randomMoveDirNew = (SeedlessRandom.RandomPoint(1) * (1 - grabCloseness) + dir * (1 + grabCloseness)).normalized;
					randomDirTimer.Reset();
				}
				randomMoveDir = Vector3.Lerp(randomMoveDir, randomMoveDirNew, DeltaTime()).normalized;

				// Normal movement
				//if (!playerMover.grabbed)
				float oscill = 0.9f + Mathf.Sin(TotalTime() * Mathf.PI * 1.5f) * 0.5f;
				transform.position += Mathf.Clamp01(distance) * slowDownMult * (speed * oscill) * DeltaTime() * randomMoveDir;

				// Use up dash
				curDashFuel -= DeltaTime() * fuelConsume;
			}
			// Approach in a straight line instead
			else if (distance <= maxDistance)
			{
				transform.position += farSpeed * DeltaTime() * smoothDir;
			}
		}

		// Vibrate when about to dash
		float newVibrateMult = distance <= engageDistance ? 1 - Mathf.Clamp01(dashTimer.currentTime / dashMaxTime) : 0;
		newVibrateMult *= newVibrateMult;
		vibrateMult = Mathf.Lerp(vibrateMult, newVibrateMult, DeltaTime() * 3);

		// Handle all tentacles
		bool alreadyPulled = false;
		for (int i = 0; i < tentacles.Count; i++)
		{
			Tentacle t = tentacles[i];

			// Main tentacle logic
			float closeOrFar = Mathf.Clamp01((distance - engageDistance) / (maxDistance - engageDistance));
			if (distance <= maxDistance)
			{
				//// Move tips along if still far
				//if (distance > engageDistance)
				//	t.targetPointNew += 0.1f * farSpeed * DeltaTime() * dir;

				// Does this tentacle need to be adjusted now?
				bool moveNow = (t.targetPoint - transform.position).sqrMagnitude > grabDistance * grabDistance;
				// If not moving all of them, remember that this adjusted out of sync
				if (!moveTentacles)
					t.movedOutOfSync |= moveNow;
				// Set new adjusted state
				else
					t.movedOutOfSync = false;

				// Change tentacle positions
				if (moveNow)
				{
					// If even one tentacle is attached, no longer true
					MoveTentacle(t, dir, true);
				}

				// Reel in target actor
				if (t.toPull)
				{
					t.targetPointNew = t.toPull.position + dir * 1.5f;

					// Don't stack pulling
					if (!alreadyPulled && PhysicsManager.Instance.ticking)
					{
						t.toPull.AddVelocity(PhysicsManager.Instance.tickingDelta * knockbackReelYankSpeeds.x * -dir);

						alreadyPulled = true;
					}
				}
			}

			float delta = DeltaTime() * (t.toPull ? 5 : 1);

			// Move towards new target point
			t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, delta * 2);

			// Overall smoothing
			t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, delta);
			t.offsetDir = Vector3.Lerp(t.offsetDir, t.offsetDirNew, DeltaTime());

			// Drag behind tips to make a curve
			t.curveTipPoint = Vector3.Lerp(t.curveTipPoint, t.targetPoint, delta);

			RenderTentacle(t, dir, smoothDir, (1 - closeOrFar) * (1 - closeOrFar));
		}


		// Change body rotation
		Vector3 goalDir = dir;

		float rotAccel = Mathf.Lerp(2, 8, curDashFuel);

		lookDir = Vector3.Lerp(lookDir, goalDir, rotAccel * DeltaTime() * (1 + 3 * damageCloseness));
		if (lookDir != Vector3.zero)
			transform.rotation = Quaternion.LookRotation(lookDir);
	}

	private void PlayerDeadReset()
	{
		grabLoop.volume = 0;
		damageLoop.volume = 0;

		transform.position = initPosition;

		dashTimer.Reset();
		curDashFuel = 0;

		foreach (Tentacle t in tentacles)
			MoveTentacle(t);
	}

	private void MoveTentacle(Tentacle t)
	{
		MoveTentacle(t, Vector3.zero, false);
	}

	private bool MoveTentacle(Tentacle t, Vector3 dir, bool adjust)
	{
		bool doGrab = Player.Instance && !Player.Instance.vitals.dead && (target.position - transform.position).sqrMagnitude < (grabDistance * 0.67f) * (grabDistance * 0.67f);
		if (doGrab && SeedlessRandom.NextFloat() > grabChance)
		{
			// Yank player and remember them for later
			if (!t.toPull)
			{
				t.toPull = target.GetComponentInChildren<Actor>();
				t.toPull.AddVelocity(dir * -knockbackReelYankSpeeds.z);
			}
			// Was already pulling
			else
			{
				doGrab = false;
				t.toPull = null;
			}
		}
		else
		{
			doGrab = false;
			t.toPull = null;
		}

		if (!doGrab)
		{
			Vector3 newOffset = SeedlessRandom.RandomPoint(1).normalized;
			newOffset *= Mathf.Sign(Vector3.Dot((newOffset - dir * 0.1f).normalized, dir));
			t.targetPointNew = Vector3.Lerp(t.targetPointNew, transform.position + newOffset * grabDistance, 0.7f);
		}
		else
			t.targetPointNew = target.position;

		if (!doGrab)
			t.offsetDirNew = Vector3.Lerp(t.offsetDirNew, SeedlessRandom.RandomPoint(6), 0.5f);
		else
			t.offsetDirNew = SeedlessRandom.RandomPoint(1).normalized * 7;

		return t.toPull;
	}

	private Vector3 PositionOnPlane(Vector3 source)
	{
		// create a plane object representing the Plane
		var plane = new Plane(-transform.forward, transform.position);

		// get the closest point on the plane for the Source position
		Vector3 mirrorPoint = plane.ClosestPointOnPlane(source);

		return mirrorPoint;
	}

	private void RenderTentacle(Tentacle t)
	{
		RenderTentacle(t, Vector3.zero, Vector3.zero, 0);
	}

	private float DeltaTime()
	{
		return Time.deltaTime * timeScale;
	}

	private float TotalTime()
	{
		return Time.time * timeScale;
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
			float waveyStrength = 0.5f * Mathf.Lerp(curveStrength, 1, 0.25f) * Mathf.Sin(percent * grabDistance - TotalTime() * (tentacleWaveSpeed));

			Vector3 surfacePoint = transform.position + (t.targetPoint - transform.position).normalized * 0.5f;

			// Attach to world position
			Vector3 vibrate = t.toPull ? Vector3.zero : (dashVibrate * sharpness * vibrateMult * vibrateMult * SeedlessRandom.NextFloatInRange(-1, 1) * perpin);

			Vector3 actualTip = Vector3.Lerp(t.targetPoint, t.curveTipPoint, curveStrength);

			Vector3 baseShape = Vector3.Lerp(surfacePoint, actualTip, percent);

			Vector3 position = baseShape + kneeStrength * kneeStrength * t.offsetDir + waveyStrength * perpin + vibrate;

			t.line.SetPosition(i, position);
		}
	}
}
