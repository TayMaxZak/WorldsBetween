using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cephapath : MonoBehaviour
{
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
	[Range(0,1)]
	public float dioramaSharpness = 0;
	[Range(0, 1)]
	public float dioramaWiggle = 0.1f;

	[Header("References")]
	public AudioSource grabLoop;
	public Sound dashSound;
	public Transform target;

	[SerializeField]
	private LineRenderer tentaclePrefab;
	[SerializeField]
	private Transform tentacleRoot;
	private List<Tentacle> tentacles = new List<Tentacle>();

	public float damage = 10;
	public float grabChance = 0.75f;

	[Header("Ranges")]
	public int tentacleCount = 20;
	public float grabDistance = 32;
	public float damageDistance = 16;
	public float maxDistance = 400;

	[Header("Timings")]
	[SerializeField]
	private float timeScale = 1;

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

	private float vibrateMult;

	// State //
	[SerializeField]
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

		bool moveTentacles = false;

		if (!target)
			return;

		// Vector towards player
		Vector3 diff = target.position - transform.position;
		float distance = diff.magnitude;
		Vector3 dir = diff.normalized;
		if (smoothDir == Vector3.zero)
			smoothDir = dir;

		// Handle player interaction
		if (!Player.Instance.vitals.dead)
		{
			// Close enough to do bad things
			float badnessStrength = 1 - Mathf.Clamp01(Mathf.Max(distance - damageDistance, 0) / damageDistance);

			// Update audio
			grabLoop.volume = (1 - distance / grabDistance) * 0.5f;
			grabLoop.pitch = 1 + badnessStrength * 0.3f;

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

		// Handle all tentacles
		for (int i = 0; i < tentacles.Count; i++)
		{
			Tentacle t = tentacles[i];

			// Main tentacle logic
			bool inWater = transform.position.y < World.GetWaterHeight();
			float wallsOrWater = inWater ? 1 : 0;
			if (distance <= maxDistance)
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
				if (moveNow)
				{
					// If even one tentacle is attached, no longer true
					MoveTentacle(t, dir, true);
				}
			}

			float delta = DeltaTime();

			// Move towards new target point
			t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, delta * 2);

			// Overall smoothing
			t.targetPoint = Vector3.Lerp(t.targetPoint, t.targetPointNew, delta);
			t.offsetDir = Vector3.Lerp(t.offsetDir, t.offsetDirNew, DeltaTime());

			// Drag behind tips to make a curve
			t.curveTipPoint = Vector3.Lerp(t.curveTipPoint, t.targetPoint, delta);

			RenderTentacle(t, dir, smoothDir, (1 - wallsOrWater) * (1 - wallsOrWater));
		}
	}

	private void PlayerDeadReset()
	{
		grabLoop.volume = 0;

		transform.position = initPosition;

		foreach (Tentacle t in tentacles)
			MoveTentacle(t);
	}

	private void MoveTentacle(Tentacle t)
	{
		MoveTentacle(t, Vector3.zero, false);
	}

	private bool MoveTentacle(Tentacle t, Vector3 dir, bool adjust)
	{
		Vector3 newOffset = SeedlessRandom.RandomPoint(1).normalized;
		newOffset *= Mathf.Sign(Vector3.Dot((newOffset - dir * 0.1f).normalized, dir));
		t.targetPointNew = Vector3.Lerp(t.targetPointNew, transform.position + newOffset * grabDistance, 0.7f);

		t.offsetDirNew = Vector3.Lerp(t.offsetDirNew, SeedlessRandom.RandomPoint(6), 0.5f);

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
			Vector3 vibrate = dashVibrate * sharpness * vibrateMult * vibrateMult * SeedlessRandom.NextFloatInRange(-1, 1) * perpin;

			Vector3 actualTip = Vector3.Lerp(t.targetPoint, t.curveTipPoint, curveStrength);

			Vector3 baseShape = Vector3.Lerp(surfacePoint, actualTip, percent);

			Vector3 position = baseShape + kneeStrength * kneeStrength * t.offsetDir + waveyStrength * perpin + vibrate;

			t.line.SetPosition(i, position);
		}
	}
}
