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

		if (Input.GetButtonDown("Fire1"))
		{
			if (!isAttached)
				ShootHook();
			else if (!isLocked)
				LockHook();
			else if (isLocked)
				ReleaseHook();
		}
		if (isAttached && Input.GetButtonDown("Fire2"))
		{
			ReleaseHook();
		}
		if (isAttached && isLocked)
		{
			float direction = Input.GetAxis("Mouse ScrollWheel");
			if (direction > 0)
				direction *= 2;

			float simple = Input.GetMouseButton(2) ? -0.25f : 0;

			float delta = (direction + simple) * Time.deltaTime * 10;

			length += delta;
			if (length <= 0.1f)
				ReleaseHook();

			if (Mathf.Abs(delta) > 0.1f && scrollSound && SeedlessRandom.NextFloat() < 1f * Mathf.Lerp(Time.deltaTime, 1, 0.5f))
				AudioManager.PlaySound(scrollSound, transform.position);
		}

		if (isAttached)
		{
			Debug.DrawLine(mover.locator.position, attachBlockPos);

			line.gameObject.SetActive(true);
			line.SetPosition(0, mover.body.position);
			line.SetPosition(1, attachBlockPos);

			//if (Vector3.SqrMagnitude(body.position - attachBlockPos) > length * length)
			//	body.position = attachBlockPos + (body.position - attachBlockPos).normalized * length;

			float strength = 20;

			if (isLocked && mover.ticking)
			{
				float dist = Vector3.Distance(mover.locator.position, attachBlockPos);

				if (dist > length * 2)
				{
					ReleaseHook();
					return;
				}

				if (dist > length)
				{
					mover.AddVelocity(-(mover.locator.position - attachBlockPos).normalized * (dist - length) * strength * mover.tickingDelta);
				}
				
				//mover.AddVelocity(-(mover.locator.position - attachBlockPos).normalized * (dist / length) * strength * 2 * mover.tickingDelta);
			}
		}
		else
			line.gameObject.SetActive(false);
	}

	private void ShootHook()
	{
		if (shootSound)
			AudioManager.PlaySound(shootSound, transform.position);

		BlockCastHit hit = BlockCast();
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
		length = Vector3.Distance(mover.locator.position, attachBlockPos);

		isLocked = true;
	}

	public void ReleaseHook()
	{
		ChangeAttached(false);
		isLocked = false;
	}

	private void ChangeAttached(bool val)
	{
		isAttached = val;
		mover.onRope = val;
	}

	public BlockCastHit BlockCast()
	{
		Transform camTran = mover.cam.transform;
		Vector3Int blockPos = new Vector3Int(Mathf.FloorToInt(camTran.position.x), Mathf.FloorToInt(camTran.position.y), Mathf.FloorToInt(camTran.position.z));
		Vector3 direction = camTran.forward;

		float adj = 0.5f;

		for (int i = 1; i <= 16; i++)
		{
			bool occluded = !World.GetBlockFor(
				(int)(blockPos.x + direction.x * i + adj),
				(int)(blockPos.y + direction.y * i + adj),
				(int)(blockPos.z + direction.z * i + adj)
			).IsAir();

			if (occluded)
				return new BlockCastHit(new Vector3Int((int)(blockPos.x + direction.x * i + adj), (int)(blockPos.y + direction.y * i + adj), (int)(blockPos.z + direction.z * i + adj)));
		}

		return new BlockCastHit();
	}
}

public struct BlockCastHit
{
	public Vector3Int blockPos;
	public bool hit;

	public BlockCastHit(Vector3Int blockPos)
	{
		this.blockPos = blockPos;
		hit = true;
	}
}
