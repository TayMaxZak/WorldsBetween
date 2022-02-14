using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
	public static Player Instance;

	private bool activated = false;
	public PlayerVitals vitals;
	public PlayerMover mover;
	public MouseLook mouseLook;
	public Transform hand;

	public Item heldItem;

	private Vector3 initPos;
	private Timer respawn = new Timer(1);
	private Timer quit = new Timer(1);

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

		vitals.enabled = false;
		mover.enabled = false;
		mouseLook.enabled = false;

		if (heldItem)
		{
			// Make temp copy of item asset
			heldItem = Instantiate(heldItem);

			heldItem.Equip(hand);
		}
	}

	public void ActivatePlayer()
	{
		vitals.enabled = true;
		mover.enabled = true;
		mouseLook.enabled = true;

		initPos = transform.position;

		activated = true;
	}

	public void Update()
	{
		// Debug quit
		if (Input.GetButton("Quit"))
		{
			quit.Increment(Time.deltaTime);

			if (quit.Expired())
			{
				SceneLoader.Remove();
				SceneManager.LoadScene(0);
			}
		}
		else
			quit.currentTime = quit.maxTime;


		// Only handle inputs if alive
		if (!activated)
			return;
		if (vitals.dead)
			return;


		// Debug respawn
		if (Input.GetButton("Astrum"))
		{
			respawn.Increment(Time.deltaTime);

			if (respawn.Expired())
				Respawn();
		}
		else
			respawn.currentTime = respawn.maxTime;

		if (vitals.dead && Input.GetButtonDown("Quit"))
			Respawn();

		// Item inputs
		if (!heldItem)
			return;

		heldItem.Update();

		bool lmb = Input.GetButtonDown("Use Item Main");

		if (lmb)
		{
			heldItem.Use();
		}
	}

	private void Respawn()
	{
		vitals.Respawn();

		mover.position = initPos;
		mover.SetVelocity(Vector3.zero);
		mover.UpdateBlockPosition();

		World.WaterFollow(mover.blockPosition);

		mouseLook.SetXRotation(0);

		//hook.ReleaseHook();
	}
}
