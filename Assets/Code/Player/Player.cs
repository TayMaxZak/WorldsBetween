using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
	public static Player Instance;

	public PlayerVitals vitals;
	public PlayerMover mover;

	public Item heldItem;

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
	}

	public void ActivatePlayer()
	{
		vitals.enabled = true;
		mover.enabled = true;
	}

	public void Update()
	{
		if (vitals.dead)
		{
			return;
		}

		bool lmb = Input.GetButtonDown("Use Item Main");

		if (lmb && heldItem)
		{
			heldItem.Use();
		}
	}
}
