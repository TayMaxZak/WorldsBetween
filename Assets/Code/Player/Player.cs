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
	public Transform head;
	public Transform hand;
	public Light flashlight;
	public Camera cam;

	public Item heldItem;
	public GameObject itemModel;

	private Vector3 initPos;
	private Timer hintTimer = new Timer(1);
	private Timer quit = new Timer(1);

	private bool firstOrSecond = true;


	public GameObject hint;

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

		if (heldItem)
		{
			// Make temp copy of item asset
			heldItem = Instantiate(heldItem);

			ChangeHeldItem(heldItem);
		}
	}


	public void InitPlayerActor(Vector3 blockPos)
	{
		// Set physical position
		transform.position = blockPos + new Vector3(0.5f, 1, 0.5f);
		initPos = transform.position;

		// Init actor
		mover.Init();
	}

	public void ActivatePlayer()
	{
		// Enable related components
		mover.enabled = true;
		vitals.enabled = true;

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

		if ((vitals.dead && Input.GetButtonDown("Quit")) || Input.GetButtonDown("Restart"))
		{
			if (PersistentData.GetInstanceForRead().IsDebugMode())
				Respawn();
			else
				SceneManager.LoadScene(0);
		}


		// Only handle inputs if activated and alive
		if (!activated)
			return;
		if (vitals.dead)
			return;


		// Debug respawn
		hintTimer.Increment(Time.deltaTime);
		if (hintTimer.Expired() && Input.GetButton("Astrum"))
		{
			GoalPointHint();
			hintTimer.Reset();
		}


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
		heldItem.ModelUpdate(itemModel);

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

	private void GoalPointHint()
	{
		Instantiate(hint, Player.Instance.head.position - Vector3.up * 0.5f, hand.rotation);
	}

	public void Die()
	{
		if (heldItem)
			heldItem.Die();
	}

	private void Respawn()
	{
		vitals.Respawn();
		mover.Respawn();

		//mover.UpdateBlockPosition();

		//World.WaterFollow(mover.blockPosition);

		//mouseLook.SetXRotation(0);

		if (heldItem)
			heldItem.Init();

		//hook.ReleaseHook();
	}
}
