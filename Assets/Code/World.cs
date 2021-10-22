using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
	[SerializeField]
	private Timer lightUpdateTimer = new Timer(1);

	private List<Chunk> chunks;

	private void Start()
	{
		chunks = new List<Chunk>(GetComponentsInChildren<Chunk>());
	}

	private void Update()
	{
		lightUpdateTimer.Increment(Time.deltaTime);

		// Is this a major light update?
		bool doLightUpdate = lightUpdateTimer.Expired();

		// Reset timer for next update
		if (doLightUpdate)
			lightUpdateTimer.Reset();

		// Find partial time for blending light
		float partialTime = Mathf.Clamp01(1 - lightUpdateTimer.currentTime / lightUpdateTimer.maxTime);

		foreach (Chunk chunk in chunks)
		{
			if (doLightUpdate)
				chunk.UpdateLight();

			chunk.InterpLight(partialTime);
		}
	}
}
