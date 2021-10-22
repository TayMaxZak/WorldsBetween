using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
	[Header("Lighting")]
	[SerializeField]
	private Timer lightUpdateTimer = null;

	[SerializeField]
	private Transform lightRoot = null;
	private List<LightSource> lightSources;

	[Header("Generators")]
	[SerializeField]
	private Transform carverRoot = null;
	private List<Carver> carvers;

	// Chunks
	private List<Chunk> chunks;

	private static World Instance;

	private void Awake()
	{
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}
		else
			Instance = this;

		chunks = new List<Chunk>(GetComponentsInChildren<Chunk>());

		lightSources = new List<LightSource>(lightRoot.GetComponentsInChildren<LightSource>());

		carvers = new List<Carver>(carverRoot.GetComponentsInChildren<Carver>());
	}

	private void Start()
	{
		Generate();
	}

	private void Generate()
	{
		foreach (Chunk chunk in chunks)
		{
			for (int i = 0; i < carvers.Count; i++)
			{
				carvers[i].UpdatePos();

				chunk.ApplyCarver(carvers[i], i == 0);
			}

			chunk.UpdateOpacityVisuals();
		}
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
		Shader.SetGlobalFloat("PartialTime", partialTime);

		if (!doLightUpdate)
			return;

		// Apply lights
		for (int i = 0; i < lightSources.Count; i++)
		{
			foreach (Chunk chunk in chunks)
			{
				chunk.AddLight(lightSources[i], i == 0);

				chunk.UpdateLightVisuals();
			}
		}
	}

	public static Block GetBlockFor(Vector3Int pos)
	{
		Vector3Int dummy;

		foreach (Chunk chunk in Instance.chunks)
		{
			dummy = pos - chunk.position;

			if (chunk.ContainsPos(dummy.x, dummy.y, dummy.z))
				return chunk.GetBlock(dummy.x, dummy.y, dummy.z);
		}

		return null;
	}

	public static int DistanceSqr(int xa, int ya, int za, int xb, int yb, int zb)
	{
		//return (int)Vector3.SqrMagnitude(new Vector3(xa - xb, ya - yb, za - zb));
		return (xa - xb) * (xa - xb) + (ya - yb) * (ya - yb) + (za - zb) * (za - zb);
	}
}
