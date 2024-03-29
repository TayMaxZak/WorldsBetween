using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cephapath : Actor
{
	[SerializeField]
	private LayerMask blockRayMask;
	[SerializeField]
	private LayerMask blockOrPlayerRayMask;

	public class Tentacle
	{
		public LineRenderer line;

		public float width = 1;

		public Vector3 targetPoint;
		public Vector3 targetPointNew;

		public Vector3 offsetDir;
		public Vector3 offsetDirNew;

		public Vector3 curveTipPoint;

		public bool movedOutOfSync;
	}

	public bool dioramaMode = false;
	[Range(0, 1)]
	public float dioramaSharpness = 0;
	[Range(0, 1)]
	public float dioramaWiggle = 0.1f;

	[Header("References")]
	public AudioSource grabLoop;
	public Sound ambientSound;
	public Transform target;
	public Vector3 targetPoint;

	[SerializeField]
	private LineRenderer tentaclePrefab;
	[SerializeField]
	private Transform tentacleRoot;
	private List<Tentacle> tentacles = new List<Tentacle>();

	[Header("Stats")]
	[SerializeField]
	private float maxSpeed = 5;
	[SerializeField]
	private float accel = 0.3f;
	private float curSpeed = 0;
	public float damagePerSecond = 10;
	public float grabDamage = 20;
	public float grabPull = 5f;

	[Header("Ranges")]
	public int tentacleCount = 20;
	public float encounterDistance = 48;
	public float tentacleLength = 24;
	public float damageDistance = 16;

	[Header("Timings")]
	[SerializeField]
	private float timeScale = 1;

	[SerializeField]
	private Timer ambSoundtimer = new Timer(4);
	[SerializeField]
	private float ambSoundTimerMin = 4;
	[SerializeField]
	private float ambSoundTimerMax = 10;

	[SerializeField]
	private Timer grabTimer = new Timer(1f);

	[Header("Tweaks")]
	[SerializeField]
	private float dashVibrate = 0.12f;
	[SerializeField]
	private float tentacleWaveSpeed = 2.5f;
	[SerializeField]
	private float sharpness = 0;
	private float moveTestDistance = 5;

	private Vector3 smoothDir;

	private float vibrateMult;

	// State //
	[SerializeField]
	private Vector3 initPosition; // Where did it start? Returns here after killing the player

	private bool playerDeadReset = true;

	private bool hasBeenSpotted = false;

	protected override void Awake()
	{
		if (!dioramaMode)
			base.Awake();
	}

	public override void Init()
	{
		if (!enabled)
			return;

		if (!dioramaMode)
		{
			initPosition = transform.position;
			if (!target)
				target = Player.Instance.mover.transform;

			// Reset all timers
			ambSoundtimer.Reset(SeedlessRandom.NextFloatInRange(ambSoundTimerMin, ambSoundTimerMax));

			Move(transform.forward, 1);
		}
		else
		{
			grabLoop.volume = 0;
		}

		foreach (Tentacle t in tentacles)
		{
			InitTentacle(t);
		}
	}

	private void InitTentacle(Tentacle t)
	{
		// Initialize previous values manually
		t.targetPointNew = tentacleRoot.position + SeedlessRandom.RandomPoint(tentacleLength).normalized;
		t.offsetDirNew = SeedlessRandom.RandomPoint(6);

		t.targetPoint = tentacleRoot.position + SeedlessRandom.RandomPoint(tentacleLength).normalized;
		t.curveTipPoint = tentacleRoot.position + SeedlessRandom.RandomPoint(tentacleLength);

		MoveTentacle(t);

		// Manually lerp over to the new target point
		t.targetPoint = t.targetPointNew;
		// Same for curve
		t.curveTipPoint = Vector3.Lerp(t.curveTipPoint, t.targetPoint, 0.4f);

		RenderTentacle(t);
	}

	private void OnEnable()
	{
		if (tentacles.Count > 0)
			return;

		for (int i = 0; i < tentacleCount; i++)
		{
			GameObject go = Instantiate(tentaclePrefab, transform.position, Quaternion.identity, tentacleRoot).gameObject;
			go.hideFlags = HideFlags.HideAndDontSave;
		}

		foreach (LineRenderer l in GetComponentsInChildren<LineRenderer>())
		{
			Tentacle t = new Tentacle() { line = l, width = l.widthMultiplier };
			tentacles.Add(t);
			InitTentacle(t);
		}
	}

	[ContextMenu("Position Tentacles")]
	private void SceneInitTentacles()
	{
		foreach (LineRenderer l in GetComponentsInChildren<LineRenderer>())
		{
			DestroyImmediate(l.gameObject);
		}

		tentacles.Clear();
		OnEnable();
	}

	private void Update()
	{
		if (dioramaMode)
			UpdateTick(false, DeltaTime(), 0);
	}

	public override void UpdateTick(bool isPhysicsTick, float tickDeltaTime, float tickPartialTime)
	{
		if (dioramaMode)
		{
			for (int i = 0; i < tentacles.Count; i++)
			{
				Tentacle t = tentacles[i];

				// Does this tentacle need to be adjusted now?
				bool moveNow = SeedlessRandom.NextFloat() > 1 - DeltaTime() * dioramaWiggle;

				// Change tentacle positions
				if (moveNow)
					MoveTentacle(t, transform.forward, true);

				float delta = DeltaTime();

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

		if (!GameManager.IsFinishedLoading())
			return;

		grabTimer.Increment(DeltaTime());

		bool moveAllTentacles = SeedlessRandom.NextFloat() < DeltaTime() * 0.1f;

		if (!target)
			return;

		// Vector towards player
		Vector3 diff = (!hasBeenSpotted ? target.position : targetPoint) - transform.position;
		float distance = diff.magnitude;
		Vector3 dir = diff.normalized;
		if (dir == Vector3.zero)
			dir = transform.forward;
		if (smoothDir == Vector3.zero)
			smoothDir = dir;

		float minDistance = 1f;

		if (distance > minDistance)
			Move(dir, distance);

		// Handle player interaction
		if (!Player.Instance.vitals.dead)
		{
			Vector3 playerDiff = (target.position - transform.position);
			float playerDistance = playerDiff.magnitude;

			bool seesTarget = distance < encounterDistance && !Physics.Raycast(transform.position, playerDiff.normalized, playerDistance, blockRayMask)/* && Vector3.Dot(Player.Instance.head.forward, -dir) > 0.6f*/;

			if (seesTarget)
			{
				targetPoint = target.position;

				if (!hasBeenSpotted)
				{
					hasBeenSpotted = true;
					AudioManager.PlayMusicCue(AudioManager.CueType.EncounterStarting);
				}
			}

			// Close enough to do bad things
			// TODO: Doesn't find right amount

			float badnessStrength = 1 - Mathf.Clamp01(Mathf.Max(playerDistance - damageDistance, 0) / damageDistance);

			// Update audio
			//grabLoop.volume = (1 - distance / grabDistance) * 0.5f;
			grabLoop.pitch = 1 + badnessStrength * 0.5f;

			// Deal damage
			if (playerDistance < damageDistance)
				Player.Instance.vitals.DealDamage(grabDamage * DeltaTime());

			playerDeadReset = true;
		}
		// Player is dead
		else if (playerDeadReset)
		{
			playerDeadReset = false;

			transform.position = initPosition;
			foreach (Tentacle t in tentacles)
			{
				InitTentacle(t);
			}
			hasBeenSpotted = false;
		}

		if (hasBeenSpotted)
		{
			ambSoundtimer.Increment(DeltaTime());
			if (ambSoundtimer.Expired())
			{
				ambSoundtimer.Reset(SeedlessRandom.NextFloatInRange(ambSoundTimerMin, ambSoundTimerMax));

				AudioManager.PlaySound(ambientSound, transform.position);
			}
		}

		// For a more curved path towards player
		smoothDir = Vector3.Lerp(smoothDir, dir, DeltaTime());

		// Handle all tentacles
		for (int i = 0; i < tentacles.Count; i++)
		{
			Tentacle t = tentacles[i];

			// Main tentacle logic
			if (hasBeenSpotted && distance > minDistance)
			{
				Physics.Raycast(tentacleRoot.position, (t.targetPoint - tentacleRoot.position).normalized, out RaycastHit hit, (t.targetPoint - tentacleRoot.position).magnitude * 0.9f, blockOrPlayerRayMask);
				// Does this tentacle need to be adjusted now?
				bool moveNow = moveAllTentacles ||
					(Vector3.Dot(dir, (t.targetPoint - tentacleRoot.position).normalized) < 0 && SeedlessRandom.NextFloat() < DeltaTime()) ||
					(t.targetPoint - (tentacleRoot.position + moveTestDistance * curSpeed * dir)).sqrMagnitude > tentacleLength * tentacleLength ||
					hit.collider;
				// If not moving all of them, remember that this adjusted out of sync
				if (!moveAllTentacles)
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
				if (hit.collider && hit.collider.gameObject.CompareTag("Player") && grabTimer.Expired())
				{
					grabTimer.Reset();

					Player.Instance.mover.AddVelocity(-dir * grabPull);
					Player.Instance.vitals.DealDamage(grabDamage);
				}
			}

			float delta = DeltaTime();

			// Move towards new target point
			t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, delta * (10f / Vector3.Distance(t.targetPointNew, t.targetPoint)));

			// Overall smoothing
			t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, delta / 2);
			t.offsetDir = Vector3.Lerp(t.offsetDir, t.offsetDirNew, delta / 2);

			// Drag behind tips to make a curve
			t.curveTipPoint = Vector3.Lerp(t.curveTipPoint, t.targetPoint, delta * (10f / Vector3.Distance(t.curveTipPoint, t.targetPoint)));

			bool inWater = transform.position.y < World.GetWaterHeight();
			//float smoothOrSharp = inWater ? 1 : 0;
			float smoothOrSharp = sharpness;
			RenderTentacle(t, dir, smoothDir, (1 - smoothOrSharp) * (1 - smoothOrSharp));
		}
	}

	private void Move(Vector3 dir, float distance)
	{
		float deltaTime = Time.deltaTime;

		if (smoothDir != Vector3.zero)
			transform.forward = smoothDir;

		if (hasBeenSpotted/* && !Physics.Raycast(transform.position, dir, distance, rayMask)*/)
			curSpeed = Mathf.Lerp(curSpeed, maxSpeed, deltaTime * accel);
		else
			curSpeed = Mathf.Lerp(curSpeed, 0, deltaTime * accel);

		transform.position += (curSpeed / 2f) * deltaTime * dir;

		Vector3 avgTentaclePos = Vector3.zero;
		foreach (Tentacle t in tentacles)
		{
			avgTentaclePos += t.curveTipPoint / tentacleCount;
		}

		transform.position = Vector3.Lerp(transform.position, avgTentaclePos, Time.deltaTime);
	}

	private void MoveTentacle(Tentacle t)
	{
		MoveTentacle(t, Vector3.zero, false);
	}

	private bool MoveTentacle(Tentacle t, Vector3 dir, bool adjust)
	{
		Vector3 newTargetPos = SeedlessRandom.RandomPoint().normalized * curSpeed + moveTestDistance * curSpeed * dir + tentacleRoot.position;
		Vector3 perpinOffset = dir == Vector3.zero ? SeedlessRandom.RandomPoint().normalized : (Quaternion.LookRotation(dir) * Vector3.right * SeedlessRandom.NextFloatInRange(-1, 1) + Quaternion.LookRotation(dir) * transform.up * SeedlessRandom.NextFloatInRange(-1, 1)).normalized;
		Vector3 newOffset = (newTargetPos - tentacleRoot.position).normalized + perpinOffset;

		Physics.Raycast(tentacleRoot.position, newOffset, out RaycastHit hit, tentacleLength, blockRayMask);

		if (hit.collider)
		{
			t.targetPointNew = Vector3.Lerp(hit.point, tentacleRoot.position, -0.05f);
			t.offsetDirNew = Vector3.Lerp(t.offsetDirNew, SeedlessRandom.RandomPoint(6), 0.5f);
		}

		return false;
	}

	private void RenderTentacle(Tentacle t)
	{
		RenderTentacle(t, Vector3.zero, Vector3.zero, sharpness);
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
		Vector3 perpin = Vector3.Cross(dir, (t.targetPoint - tentacleRoot.position).normalized);

		for (int i = 0; i < t.line.positionCount; i++)
		{
			float percent = (float)i / t.line.positionCount;

			float kneeStrength = (1 - Mathf.Abs(2 * percent - 1)) * sharpness;
			float curveStrength = percent * percent;
			float waveyStrength = 0.25f * Mathf.Lerp(curveStrength, 1, 0) * Mathf.Sin(percent * tentacleLength - TotalTime() * (tentacleWaveSpeed));

			Vector3 surfacePoint = tentacleRoot.position + (t.targetPoint - tentacleRoot.position).normalized * 0.5f;

			// Attach to world position
			Vector3 vibrate = dashVibrate * sharpness * vibrateMult * vibrateMult * SeedlessRandom.NextFloatInRange(-1, 1) * perpin;

			Vector3 actualTip = Vector3.Lerp(t.targetPoint, t.curveTipPoint, curveStrength);

			Vector3 baseShape = Vector3.Lerp(surfacePoint, actualTip, percent);

			Vector3 position = baseShape + kneeStrength * kneeStrength * t.offsetDir * 0.33f + waveyStrength * perpin + vibrate;

			t.line.SetPosition(i, position);
		}
	}
}
