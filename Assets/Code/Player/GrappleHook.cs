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

		if (isAttached && isLocked)
		{
			Debug.DrawLine(body.position, attachBlockPos);

			//if (Vector3.SqrMagnitude(body.position - attachBlockPos) > length * length)
			//	body.position = attachBlockPos + (body.position - attachBlockPos).normalized * length;

			float dist = Vector3.Distance(body.position, attachBlockPos);
			if (dist > length)
				mover.AddVelocity(-(body.position - attachBlockPos).normalized * (dist - length));
		}
	}

	private void ShootHook()
	{
		attachBlock = new Vector3Int(Mathf.FloorToInt(body.position.x), Mathf.FloorToInt(body.position.y), Mathf.FloorToInt(body.position.z));
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
}
