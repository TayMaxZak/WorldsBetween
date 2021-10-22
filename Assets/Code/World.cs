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

		if (lightUpdateTimer.Expired())
		{
			foreach (Chunk chunk in chunks)
				chunk.UpdateLight();

			lightUpdateTimer.Reset();
		}
	}
}
