using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;

public class UIManager : MonoBehaviour
{
	private static UIManager Instance;

	private bool watchRaised;

	public Animator watchAnim;

	private void Awake()
	{
		if (!Instance)
			Instance = this;
		else
			Destroy(gameObject);
	}

	public static void SetWatchRaised(bool raised)
	{
		Instance.watchRaised = raised;

		Instance.watchAnim.SetBool("WatchRaised", raised);
	}
}