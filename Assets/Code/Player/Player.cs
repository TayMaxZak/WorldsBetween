using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
	public static Player Instance;

	public PlayerVitals vitals;
	public PlayerMover mover;
	public MouseLook mouseLook;
	public Transform hand;

	public Item heldItem;

	private Vector3 initPos;
	private Timer activate = new Timer(1);
	private Timer restart = new Timer(1);

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
	}

	public void Update()
	{
		// Debug respawn
		if (Input.GetButton("Astrum"))
		{
			activate.Increment(Time.deltaTime);

			if (activate.Expired())
				Respawn();
		}
		else
			activate.currentTime = activate.maxTime;

		if (vitals.dead && Input.GetButtonDown("Quit"))
			Respawn();

		// Debug restart
		if (Input.GetButton("Restart"))
		{
			restart.Increment(Time.deltaTime);

			if (restart.Expired())
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;

				SceneLoader.Remove();
				SceneManager.LoadScene(0);
			}
		}
		else
			restart.currentTime = restart.maxTime;

		// Only handle inputs if alive
		if (vitals.dead)
			return;

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
		//if (vitals.dead)
		vitals.Respawn();

		mover.position = initPos;
		mover.SetVelocity(Vector3.zero);
		mover.UpdateBlockPosition();

		World.WaterFollow(mover.blockPosition);

		mouseLook.SetXRotation(0);

		//hook.ReleaseHook();
	}
}
