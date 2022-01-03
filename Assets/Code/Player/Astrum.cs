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

		if (vitals.dead && Input.GetButtonDown("Quit"))
			Use();

		if (Input.GetButton("Restart"))
		{
			restart.Increment(Time.deltaTime);

			if (restart.Expired())
			{
				SceneLoader.Remove();
				SceneManager.LoadScene(0);
			}
		}
		else
			restart.currentTime = restart.maxTime;
	}

	private void Use()
	{
		if (vitals.dead)
			vitals.Respawn();

		mover.position = initPos;
		mover.SetVelocity(Vector3.zero);
		mover.UpdateBlockPosition();

		World.WaterFollow(mover.blockPosition);

		look.SetXRotation(0);

		hook.ReleaseHook();
	}
}

