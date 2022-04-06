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
	public Camera cam;

	public Item heldItem;

	private Vector3 initPos;
	private Timer respawn = new Timer(1);
	private Timer quit = new Timer(1);

	private bool firstOrSecond = true;

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

			ChangeHeldItem(heldItem);
		}
	}

	public void InitPlayerActor(Vector3 blockPos)
	{
		Debug.Log("Init player actor at " + blockPos);

		// Set physical position
		transform.position = blockPos + new Vector3(0.5f, 1, 0.5f);
		initPos = transform.position;

		// Init actor
		mover.Init();
	}

	public void ActivatePlayer()
	{
		Debug.Log("Activated player");

		// Enable related components
		mover.enabled = true;
		vitals.enabled = true;
		mouseLook.enabled = true;

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
				SceneManager.LoadScene(0);
			}
		}
		else
			quit.currentTime = quit.maxTime;

		if (vitals.dead && (Input.GetButtonDown("Quit") || Input.GetButtonDown("Astrum")))
			Respawn();


		// Only handle inputs if activated and alive
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


		// Open equipment menu
		if (Input.GetButtonDown("Equipment"))
		{
			int index = firstOrSecond ? 0 : 1;
			firstOrSecond = !firstOrSecond;

			if (PersistentData.GetInstanceForRead())
			{
				Inventory inv = PersistentData.GetInstanceForRead().GetPlayerInventory();

				if (inv != null)
					ChangeHeldItem(inv.GetNth(index));
			}
		}


		// Item inputs
		if (!heldItem)
			return;

		heldItem.Update();

		bool lmb = Input.GetButtonDown("Use Item Main");
		bool rmb = Input.GetButtonDown("Use Item Alt");

		if (lmb)
			heldItem.Use(Item.UseHow.Main);
		else if (rmb)
			heldItem.Use(Item.UseHow.Alt);
	}

	private void ChangeHeldItem(Item newItem)
	{
		if (heldItem)
			heldItem.Unequip();

		heldItem = newItem;

		if (heldItem)
		{
			heldItem.Equip(hand);
			UIManager.SetHeldItem(heldItem);
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

		if (heldItem)
			heldItem.Init();

		//hook.ReleaseHook();
	}
}
