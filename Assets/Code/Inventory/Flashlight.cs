using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Flashlight : Item
{
	[Header("")]

	public bool on;

	public float range = 20;

	private Vector3 targetPosRaw;
	private Vector3 targetPos;

	private static readonly Vector3 offPosA = new Vector3(10000, 10000, 10000);
	private static readonly Vector3 offPosB = new Vector3(10000, 10000, 10001);

	public override void Use()
	{
		base.Use();

		on = !on;

		UpdateShader();
	}

	// Turn off when put away
	public override void Unequip()
	{
		base.Unequip();

		on = false;

		UpdateShader();
	}

	public override void Update()
	{
		base.Update();

		if (PhysicsManager.Instance.ticking)
		{
			BlockCastHit hit = PhysicsManager.BlockCastAxial(hand.position, hand.position + hand.forward * range);
			if (hit.hit)
				targetPosRaw = hand.position + hand.forward * (hit.worldPos - hand.position).magnitude;
			else
				targetPosRaw = hand.position + hand.forward * range;
		}
		targetPos = Vector3.Lerp(targetPos, targetPosRaw, Time.deltaTime * 5);

		UpdateShader();
	}

	private void UpdateShader()
	{
		if (on)
		{
			Shader.SetGlobalVector("FlashlightA", hand.position);

			Shader.SetGlobalVector("FlashlightB", targetPos);
		}
		else
		{
			Shader.SetGlobalVector("FlashlightA", offPosA);

			Shader.SetGlobalVector("FlashlightB", offPosB);
		}
	}
}
