using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Threading.Tasks;

public partial class PhysicsManager : MonoBehaviour
{
	public static PhysicsManager Instance;

	private List<Actor> actors = new List<Actor>();

	[SerializeField]
	private Timer physicsTickTimer = new Timer(0.2f);
	public float epsilon = 0.1f;

	public bool physicsTicking = false;
	public float tickingDelta;

	public Vector3 gravity = new Vector3(0, -20, 0);

	private void Awake()
	{
		// Ensure singleton
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}
		else
			Instance = this;
	}

	private void Update()
	{
		physicsTicking = false;

		physicsTickTimer.Increment(Time.deltaTime);

		if (physicsTickTimer.Expired())
		{
			physicsTicking = true;

			physicsTickTimer.Reset();
		}

		foreach (Actor actor in actors)
			actor.Tick(physicsTickTimer.maxTime, physicsTickTimer.currentTime, physicsTicking);
	}

	public void Register(Actor actor)
	{
		actors.Add(actor);
	}

	public void Activate()
	{
		foreach (Actor actor in actors)
			actor.Init();
	}
}
