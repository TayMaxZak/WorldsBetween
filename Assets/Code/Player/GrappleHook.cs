using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleHook : MonoBehaviour
{
	public PlayerVitals vitals;
	public PlayerMover mover;

	public Vector3Int attachBlock;
	public Vector3 attachBlockPos;

	public bool isAttached = false;

	public bool isLocked = false;

	public float maxLength = 20;
	[System.NonSerialized]
	public float length;

	public LineRenderer line;

	public Sound shootSound;
	public Sound hitSound;
	public Sound scrollSound;

	public void Update()
	{
		if (vitals.dead)
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
				AudioManager.PlaySound(scrollSound, transform.position);
		}

		if (isAttached)
		{
			Debug.DrawLine(mover.position, attachBlockPos);

			line.gameObject.SetActive(true);
			line.SetPosition(0, mover.transform.position);
			line.SetPosition(1, attachBlockPos);

			//if (Vector3.SqrMagnitude(body.position - attachBlockPos) > length * length)
			//	body.position = attachBlockPos + (body.position - attachBlockPos).normalized * length;

			float strength = 20;

			if (isLocked && PhysicsManager.Instance.physicsTicking)
			{
				float dist = Vector3.Distance(mover.position, attachBlockPos);

				if (dist > 200)
				{
					ReleaseHook();
					return;
				}

				if (dist > length)
				{
					mover.AddVelocity(-(mover.position - attachBlockPos).normalized * (dist - length) * strength * PhysicsManager.Instance.tickingDelta);
				}
			}
		}
		else
			line.gameObject.SetActive(false);
	}

	private void ShootHook()
	{
		if (shootSound)
			AudioManager.PlaySound(shootSound, transform.position);

		BlockCastHit hit = PhysicsManager.BlockCastAxial(mover.cam.transform.position, mover.cam.transform.position + mover.cam.transform.forward * maxLength);
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
		length = Vector3.Distance(mover.position, attachBlockPos);

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
		mover.onRope = val;
	}

	private void ChangeLocked(bool val)
	{
		isLocked = val && isAttached;
	}
}
