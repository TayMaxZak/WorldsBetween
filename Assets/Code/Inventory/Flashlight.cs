﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Flashlight : Item
{
	[Header("")] // Separate from Item fields

	public bool on;

	public float maxRange = 20;

	public float distLerpSpeed = 5;
	public float turnLerpSpeed = 20;

	private Vector3 beamEndPos;

	private Vector3 targetForward;
	private Vector3 currentForward;

	private float targetDist;
	private float currentDist;

	private static readonly Vector3 offPosA = new Vector3(10000, 10000, 10000);
	private static readonly Vector3 offPosB = new Vector3(10000, 10000, 10001);

	public override void Use(UseHow useHow)
	{
		base.Use(useHow);

		on = !on;

		UpdateShader(on);
	}

	// Turn off when put away
	public override void Unequip()
	{
		base.Unequip();

		UpdateShader(false);
	}

	public override void Update()
	{
		base.Update();

		if (!equipped || !on)
			return;

		currentForward = Vector3.Lerp(currentForward, targetForward, Time.deltaTime * turnLerpSpeed);
		currentDist = Mathf.Lerp(currentDist, targetDist, Time.deltaTime * distLerpSpeed);

		if (PhysicsManager.Instance.ticking)
		{
			BlockCastHit hit = PhysicsManager.BlockCastAxial(hand.position, hand.position + hand.forward * maxRange);

			targetForward = hand.forward;

			if (hit.hit)
				targetDist = (hit.worldPos - hand.position).magnitude;
			else
				targetDist = maxRange;
		}

		// Smoothed position is sent to shader
		beamEndPos = hand.position + currentForward * currentDist;
		UpdateShader(on);
	}

	private void UpdateShader(bool display)
	{
		if (display)
		{
			Shader.SetGlobalVector("FlashlightA", hand.position);

			Shader.SetGlobalVector("FlashlightB", beamEndPos);
		}
		else
		{
			Shader.SetGlobalVector("FlashlightA", offPosA);

			Shader.SetGlobalVector("FlashlightB", offPosB);
		}
	}
}
