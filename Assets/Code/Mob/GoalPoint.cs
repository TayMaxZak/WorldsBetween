﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class GoalPoint : MonoBehaviour
{
	public static GoalPoint Instance;

	private bool activated = false;

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

	public void InitGoalActor(Vector3 blockPos)
	{
		// Set physical position
		transform.position = blockPos + new Vector3(0.5f, 1f, 0.5f);

		NewPosition();
	}

	public void ActivateGoal()
	{
		activated = true;
	}

	public void OnEnable()
	{
		NewPosition();
	}

	private void NewPosition()
	{
		World.SetGoalPoint(transform.position);
	}

	public void Update()
	{
		if (!activated)
			return;

		if (Vector3.SqrMagnitude(transform.position - Player.Instance.transform.position) < 1 * 1)
		{
			PersistentData pd = PersistentData.GetInstanceForRead();
			if (pd)
			{
				pd.SetSeed(pd.GetSeed() + 1);
				pd.IncreaseDepth(1);
			}

			GameManager.FinishLevel();
		}
	}
}
