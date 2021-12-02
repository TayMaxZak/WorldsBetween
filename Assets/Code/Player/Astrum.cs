using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Astrum : MonoBehaviour
{
	public PlayerVitals vitals;
	public PlayerMover mover;
	public MouseLook look;
	public GrappleHook hook;

	public Vector3 initPos;

	public Timer activate;
	public Timer quit;
	public Timer restart;

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

		if (Input.GetButton("Quit"))
		{
			quit.Increment(Time.deltaTime);

			if (quit.Expired())
				Application.Quit();
		}
		else
			quit.currentTime = activate.maxTime;

		if (Input.GetButton("Restart"))
		{
			restart.Increment(Time.deltaTime);

			if (restart.Expired())
				SceneManager.LoadScene(0);
		}
		else
			restart.currentTime = activate.maxTime;
	}

	private void Use()
	{
		if (vitals.dead)
			vitals.Respawn();

		mover.locator.position = initPos;
		mover.SetVelocity(Vector3.zero);

		look.SetXRotation(0);

		hook.ReleaseHook();
	}
}

