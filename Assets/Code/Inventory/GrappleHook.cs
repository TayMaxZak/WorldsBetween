using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleHook : Item
{
	[Header("")] // Separate from Item fields

	public float maxRange = 20;

	private BlockCastHit hit;

	private Vector3Int cornerPos;

	private bool attached = false;

	public override void Init()
	{
		base.Init();

		attached = false;
	}

	public override void Use(UseHow useHow)
	{
		base.Use(useHow);

		if (useHow == UseHow.Main && hit.hit)
			attached = true;

		else if (useHow == UseHow.Alt && attached)
			attached = false;
	}

	public override void Unequip()
	{
		base.Unequip();
		attached = false;
	}

	public override void Update()
	{
		base.Update();

		if (!equipped)
			return;

		if (PhysicsManager.Instance.ticking)
		{
			if (!attached)
			{
				hit = PhysicsManager.BlockCastAxial(hand.position, hand.position + hand.forward * maxRange);

				if (hit.hit)
				{
					if (World.GetCorner(hit.blockPos.x, hit.blockPos.y, hit.blockPos.z))
					{
						cornerPos = hit.blockPos;
					}
				}
			}
			else
			{
				Player.Instance.mover.AddVelocity((cornerPos - Player.Instance.mover.position) * PhysicsManager.Instance.tickingDelta);
			}

			Debug.DrawLine(hand.position, cornerPos, !attached ? Utils.colorCyan : Utils.colorDarkGrayBlue, !attached ? 0.01f : PhysicsManager.Instance.tickingDelta);
		}
	}
}
