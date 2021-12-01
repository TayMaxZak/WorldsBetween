using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Astrum : MonoBehaviour
{
	public PlayerVitals vitals;
	public PlayerMover mover;

	public Vector3 initPos;

	public Timer activate;

	private void Awake()
	{
		initPos = mover.transform.position;
	}

	public void Update()
	{
		if (Input.GetButton("Astrum"))
		{
			activate.Increment(Time.deltaTime);

			if (activate.Expired())
				Use();
		}
		else
			activate.currentTime = activate.maxTime;
	}

	private void Use()
	{
		if (vitals.dead)
			vitals.Respawn();

		mover.locator.position = initPos;
		mover.SetVelocity(Vector3.zero);
	}
}

