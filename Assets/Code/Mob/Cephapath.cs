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
	public Sound dashSound;
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
	private Timer dashTimer = new Timer(11f);

	[SerializeField]
	private Timer randomDirTimer = new Timer(1f);

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

	// For dashing or wandering randomly
	private Vector3 randomMoveDir;
	private Vector3 randomMoveDirNew;

	private Vector3 smoothDir;

	private float vibrateMult;

	// State //
	[SerializeField]
	private Vector3 initPosition; // Where did it start? Returns here after killing the player

	private bool playerDeadReset = true;

	private bool hasBlinked = false;

	private bool hasBeenSpotted = false;

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
			dashTimer.Reset(1 + SeedlessRandom.NextFloat() * dashTimer.maxTime);

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

		if (!GameManager.GetFinishedLoading())
			return;

		grabTimer.Increment(DeltaTime());

		bool moveAllTentacles = SeedlessRandom.NextFloat() < DeltaTime() * 0.02f;

		if (!target)
			return;

		// Vector towards player
		Vector3 diff = target.position - transform.position;
		float distance = diff.magnitude;
		Vector3 dir = diff.normalized;
		if (smoothDir == Vector3.zero)
			smoothDir = dir;

		Move(dir, distance);

		// Handle player interaction
		if (!Player.Instance.vitals.dead)
		{
			if (!hasBeenSpotted)
			{
				if (distance < encounterDistance/* && Vector3.Dot(Player.Instance.head.forward, -dir) > 0.6f*/ && !Physics.Raycast(transform.position, dir, distance, blockRayMask))
				{
					hasBeenSpotted = true;
					AudioManager.PlayMusicCue(AudioManager.CueType.EncounterStarting);
				}
			}

			// Close enough to do bad things
			float badnessStrength = 1 - Mathf.Clamp01(Mathf.Max(distance - tentacleLength, 0) / tentacleLength);

			// Update audio
			//grabLoop.volume = (1 - distance / grabDistance) * 0.5f;
			grabLoop.pitch = 1 + badnessStrength * 0.5f;

			playerDeadReset = true;

			// Deal damage
			if (distance < damageDistance)
				Player.Instance.vitals.DealDamage(grabDamage * DeltaTime());
		}
		// Player is dead
		else if (playerDeadReset)
		{
			hasBeenSpotted = false;

			transform.position = initPosition;

			playerDeadReset = false;
		}

		// For a more curved path towards player
		smoothDir = Vector3.Lerp(smoothDir, dir, DeltaTime());

		// Handle all tentacles
		for (int i = 0; i < tentacles.Count; i++)
		{
			Tentacle t = tentacles[i];

			// Main tentacle logic
			if (hasBeenSpotted)
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
			t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, delta * 4);

			// Overall smoothing
			t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, delta / 2);
			t.offsetDir = Vector3.Lerp(t.offsetDir, t.offsetDirNew, delta / 2);

			// Drag behind tips to make a curve
			t.curveTipPoint = Vector3.Lerp(t.curveTipPoint, t.targetPoint, delta * 4);

			bool inWater = transform.position.y < World.GetWaterHeight();
			//float smoothOrSharp = inWater ? 1 : 0;
			float smoothOrSharp = sharpness;
			RenderTentacle(t, dir, smoothDir, (1 - smoothOrSharp) * (1 - smoothOrSharp));
		}
	}

	private void Move(Vector3 dir, float distance)
	{
		float deltaTime = Time.deltaTime;

		transform.forward = smoothDir;
		if (hasBeenSpotted/* && !Physics.Raycast(transform.position, dir, distance, rayMask)*/)
			curSpeed = Mathf.Lerp(curSpeed, maxSpeed, deltaTime * accel);
		else
			curSpeed = Mathf.Lerp(curSpeed, 0, deltaTime * accel);

		transform.position += curSpeed * deltaTime * smoothDir;

		Vector3 avgTentaclePos = Vector3.zero;
		foreach (Tentacle t in tentacles)
		{
			avgTentaclePos += t.curveTipPoint / tentacleCount;
		}

		transform.position = Vector3.Lerp(transform.position, avgTentaclePos, Time.deltaTime);

		//// Blinking
		//float dotCutoff = 0.5f;
		//randomDirTimer.Increment(deltaTime);
		//if (randomDirTimer.Expired() || !hasBlinked)
		//{
		//	randomDirTimer.Reset();

		//	if (Vector3.Dot(Player.Instance.cam.transform.forward, (transform.position - Player.Instance.transform.position).normalized) < dotCutoff || !hasBlinked)
		//	{
		//		Vector3 newPos = new Vector3(
		//			SeedlessRandom.NextFloatInRange(-0.5f, 0.5f) * World.GetWorldSize(),
		//			0.5f * World.GetWorldSize(),
		//			SeedlessRandom.NextFloatInRange(-0.5f, 0.5f) * World.GetWorldSize()
		//		);

		//		if (Vector3.Dot(Player.Instance.cam.transform.forward, (newPos - Player.Instance.transform.position).normalized) < dotCutoff || !hasBlinked)
		//		{
		//			transform.position = newPos;

		//			transform.eulerAngles = new Vector3(
		//				SeedlessRandom.NextFloatInRange(-10, 10),
		//				SeedlessRandom.NextFloatInRange(0, 360),
		//				0
		//			);

		//			foreach (Tentacle t in tentacles)
		//				InitTentacle(t);

		//			hasBlinked = true;
		//		}
		//	}
		//}
	}

	private void MoveTentacle(Tentacle t)
	{
		MoveTentacle(t, Vector3.zero, false);
	}

	private bool MoveTentacle(Tentacle t, Vector3 dir, bool adjust)
	{
		Vector3 newTargetPos = SeedlessRandom.RandomPoint().normalized * curSpeed + moveTestDistance * curSpeed * dir + tentacleRoot.position;
		Vector3 perpinOffset = dir == Vector3.zero ? Vector3.zero : (Quaternion.LookRotation(dir) * Vector3.right * SeedlessRandom.NextFloatInRange(-1, 1) + Quaternion.LookRotation(dir) * transform.up * SeedlessRandom.NextFloatInRange(-1, 1)).normalized;
		Vector3 newOffset = (newTargetPos - tentacleRoot.position).normalized + perpinOffset;
		//newOffset *= Mathf.Sign(Vector3.Dot((newOffset - dir * 0.1f).normalized, dir));

		Physics.Raycast(tentacleRoot.position, newOffset, out RaycastHit hit, tentacleLength, blockRayMask);

		if (hit.collider)
		{
			t.targetPointNew = Vector3.Lerp(hit.point, tentacleRoot.position, -0.05f);
			t.offsetDirNew = Vector3.Lerp(t.offsetDirNew, SeedlessRandom.RandomPoint(6), 0.5f);
		}

		return false;
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
