using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleHook : MonoBehaviour
{
	public PlayerMover mover;

	public Vector3Int attachBlock;
	public Vector3 attachBlockPos;

	public bool isAttached = false;

	public bool isLocked = false;

	public float length;

	public LineRenderer line;

	public void Update()
	{
		if (!isAttached && Input.GetButtonDown("Equipment"))
		{
			ShootHook();
		}
		if (isAttached && Input.GetButtonUp("Equipment"))
		{
			if (!isLocked)
				LockHook();
			else
				ReleaseHook();
		}
		if (isAttached && isLocked && Input.GetButton("Equipment"))
		{
			length -= Time.deltaTime * 2;
			if (length <= 0.1f)
				ReleaseHook();
		}



		if (isAttached)
		{
			Debug.DrawLine(mover.locator.position, attachBlockPos);

			line.gameObject.SetActive(true);
			line.SetPosition(0, mover.body.position);
			line.SetPosition(1, attachBlockPos);

			//if (Vector3.SqrMagnitude(body.position - attachBlockPos) > length * length)
			//	body.position = attachBlockPos + (body.position - attachBlockPos).normalized * length;

			if (isLocked && mover.ticking)
			{
				float dist = Vector3.Distance(mover.locator.position, attachBlockPos);
				if (dist > length)
				{
					mover.AddVelocity(-(mover.locator.position - attachBlockPos).normalized * (dist - length) * (dist - length) * 20 * mover.tickingDelta);

					if (dist > length * 2)
						ReleaseHook();
				}
			}
		}
		else
			line.gameObject.SetActive(false);
	}

	private void ShootHook()
	{
		BlockCastHit hit = BlockCast();
		if (!hit.hit)
			return;

		attachBlock = hit.blockPos;
		attachBlockPos = attachBlock + Vector3.one * 0.5f;

		isAttached = true;
	}

	private void LockHook()
	{
		length = Vector3.Distance(mover.locator.position, attachBlockPos);

		isLocked = true;
	}

	private void ReleaseHook()
	{
		isAttached = false;
		isLocked = false;
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

		ReleaseHook();
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
