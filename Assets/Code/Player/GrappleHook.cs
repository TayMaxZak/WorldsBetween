using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleHook : MonoBehaviour
{
	public Transform body;
	public PlayerMover mover;

	public Vector3Int attachBlock;
	public Vector3 attachBlockPos;

	public bool isAttached = false;

	public bool isLocked = false;

	public float length;

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
			length -= Time.deltaTime;
		}



		if (isAttached && isLocked)
		{
			Debug.DrawLine(body.position, attachBlockPos);

			//if (Vector3.SqrMagnitude(body.position - attachBlockPos) > length * length)
			//	body.position = attachBlockPos + (body.position - attachBlockPos).normalized * length;

			if (mover.ticking)
			{
				float dist = Vector3.Distance(body.position, attachBlockPos);
				if (dist > length)
					mover.AddVelocity(-(body.position - attachBlockPos).normalized * (dist - length) * (dist - length) * 20 * mover.tickingDelta);
			}
		}
	}

	private void ShootHook()
	{
		attachBlock = BlockCast();
		attachBlockPos = attachBlock + Vector3.one * 0.5f;

		isAttached = true;
	}

	private void LockHook()
	{
		length = Vector3.Distance(body.position, attachBlockPos);

		isLocked = true;
	}

	private void ReleaseHook()
	{
		isAttached = false;
		isLocked = false;
	}

	public Vector3Int BlockCast()
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
				return new Vector3Int((int)(blockPos.x + direction.x * i + adj), (int)(blockPos.y + direction.y * i + adj), (int)(blockPos.z + direction.z * i + adj));
		}

		return blockPos;
	}
}
