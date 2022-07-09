using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Astrum : Item
{
	[Header("")] // Separate from Item fields

	public float turnLerpSpeed = 20;

	private Vector3 goalPos;

	private Vector3 targetForward;

	public override void Use(UseHow useHow)
	{
		base.Use(useHow);
	}

	public override void Equip(Transform hand)
	{
		base.Equip(hand);
	}

	public override void ModelUpdate(GameObject model)
	{
		base.ModelUpdate(model);

		if (!equipped)
			return;

		goalPos = new Vector3(World.GetGoalPoint().x, Player.Instance.transform.position.y, World.GetGoalPoint().z);

		targetForward = (goalPos - Player.Instance.transform.position).normalized;

		float floatAmt = 0.01f;
		float floatFreq = 3f;
		model.transform.localPosition = new Vector3(0, floatAmt + Mathf.Sin(floatFreq * Time.time) * floatAmt, 0);
		model.transform.rotation = Quaternion.Slerp(model.transform.rotation, Quaternion.LookRotation(targetForward), Time.deltaTime * turnLerpSpeed);
	}
}
