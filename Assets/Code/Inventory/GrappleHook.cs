using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleHook : Item
{
	private Vector3Int attachBlock;
	private Vector3 attachBlockPos;

	private bool isAttached = false;

	private bool isLocked = false;

	public float maxLength = 20;
	private float length;

	[SerializeField]
	private LineRenderer linePrefab;
	private LineRenderer line;

	[SerializeField]
	public Sound shootSound;
	[SerializeField]
	public Sound hitSound;
	[SerializeField]
	public Sound scrollSound;

	public override void Equip(Transform hand)
	{
		line = Instantiate(linePrefab);
	}

	public override void Unequip()
	{
		Destroy(line);
	}

	public override void Update()
	{
		if (Player.Instance.vitals.dead)
		{
			if (isAttached)
				ReleaseHook();
			else
				return;
		}

		bool lmb = Input.GetButtonDown("Use Item Main");
		if (lmb || Input.GetButtonDown("Use Item Alt"))
		{
			if (!isAttached)
			{
				ShootHook();
				LockHook();
			}
			else if (lmb)
			{
				if (isLocked)
					UnLockHook();
				else if (!isLocked)
					LockHook();
			}
		}
		if (isAttached && Input.GetButtonUp("Use Item Alt"))
		{
			ReleaseHook();
		}
		if (isAttached && isLocked)
		{
			float direction = Input.GetAxisRaw("Mouse ScrollWheel");
			if (direction > 0)
				direction *= 2;

			bool rmb = Input.GetButton("Use Item Alt");
			float simple = rmb ? -0.05f : (Input.GetButton("Use Item Scroll Click") ? -0.02f : 0);

			float speed = (direction + simple) * 100;

			length += speed * Time.deltaTime;

			length = Mathf.Max(length, 0.5f);

			if (Mathf.Abs(speed) > 0.02f && scrollSound && SeedlessRandom.NextFloat() < 10 * Time.deltaTime)
				AudioManager.PlaySound(scrollSound, hand.position);
		}

		if (isAttached)
		{
			Debug.DrawLine(Player.Instance.mover.position, attachBlockPos);

			line.gameObject.SetActive(true);
			line.SetPosition(0, Player.Instance.mover.transform.position);
			line.SetPosition(1, attachBlockPos);

			//if (Vector3.SqrMagnitude(body.position - attachBlockPos) > length * length)
			//	body.position = attachBlockPos + (body.position - attachBlockPos).normalized * length;

			float strength = 20;

			if (isLocked && PhysicsManager.Instance.ticking)
			{
				float dist = Vector3.Distance(Player.Instance.mover.position, attachBlockPos);

				if (dist > 200)
				{
					ReleaseHook();
					return;
				}

				if (dist > length)
				{
					Player.Instance.mover.AddVelocity(-(Player.Instance.mover.position - attachBlockPos).normalized * (dist - length) * strength * PhysicsManager.Instance.tickingDelta);
				}
			}
		}
		else
			line.gameObject.SetActive(false);
	}

	private void ShootHook()
	{
		if (shootSound)
			AudioManager.PlaySound(shootSound, hand.position);

		BlockCastHit hit = PhysicsManager.BlockCastAxial(Player.Instance.mover.cam.transform.position, Player.Instance.mover.cam.transform.position + Player.Instance.mover.cam.transform.forward * maxLength);
		if (!hit.hit)
		{
			ReleaseHook();
			return;
		}
		if (hitSound)
			AudioManager.PlaySound(hitSound, hit.blockPos);

		attachBlock = hit.blockPos;
		attachBlockPos = attachBlock + Vector3.one * 0.5f;

		ChangeAttached(true);
	}

	private void LockHook()
	{
		length = Vector3.Distance(Player.Instance.mover.position, attachBlockPos);

		ChangeLocked(true);
	}

	private void UnLockHook()
	{
		ChangeLocked(false);
	}

	public void ReleaseHook()
	{
		ChangeAttached(false);
		ChangeLocked(false);
	}

	private void ChangeAttached(bool val)
	{
		isAttached = val;
	}

	private void ChangeLocked(bool val)
	{
		isLocked = val && isAttached;
	}
}
