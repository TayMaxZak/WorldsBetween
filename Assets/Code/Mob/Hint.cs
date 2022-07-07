using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Hint : MonoBehaviour
{
	private float speed;
	[SerializeField]
	private float minSpeed = 3;
	[SerializeField]
	private float maxSpeed = 6;
	[SerializeField]
	private float acceleration = 3;
	[SerializeField]
	private float turnSpeed = 90;
	[SerializeField]
	private Timer lifeTimer = new Timer(2.5f);

	[SerializeField]
	private float randomSpeed = 1;
	private Timer randomize = new Timer(0.5f);
	private Vector3 randomDir;
	private Vector3 randomDirPrev;
	private Vector3 randomDirNext;

	private void Awake()
	{
		speed = minSpeed;
		lifeTimer.Reset();
		randomize.Reset(0);
	}

	private void Update()
	{
		lifeTimer.Increment(Time.deltaTime);
		if (lifeTimer.Expired())
			Destroy(gameObject);

		speed = Mathf.Clamp(speed + acceleration * Time.deltaTime, 0, maxSpeed);

		transform.position += speed * Time.deltaTime * transform.forward;

		randomize.Increment(Time.deltaTime);
		if (randomize.Expired())
		{
			randomDirPrev = randomDir;
			randomDirNext = SeedlessRandom.RandomPoint(1);
			randomize.Reset();
		}
		randomDir = Vector3.Lerp(randomDirPrev, randomDirNext, 1 - randomize.currentTime / randomize.maxTime);
		transform.position += randomSpeed * Time.deltaTime * randomDir;


		transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(World.GetGoalPoint() - transform.position), turnSpeed * Time.deltaTime);
	}
}
