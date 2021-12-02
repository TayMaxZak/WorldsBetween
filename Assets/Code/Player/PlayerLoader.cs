using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLoader : MonoBehaviour
{
	public MonoBehaviour[] toActivate;

	private void Awake()
	{
		foreach (MonoBehaviour c in toActivate)
			c.enabled = false;
	}

	public void ActivatePlayer()
	{
		foreach (MonoBehaviour c in toActivate)
			c.enabled = true;
	}
}
