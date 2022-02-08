using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITimeScale : MonoBehaviour
{
	public float timeScale = 0.01f;

	void Update()
	{
		Time.timeScale = timeScale;
	}

	void OnDestroy()
	{
		Time.timeScale = 1;
	}
}
