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

	private bool firstLightPass = true;

	[Header("Generators")]
	[SerializeField]
	private Transform carverRoot = null;
	private List<Carver> carvers;

	// Chunks
	private List<Chunk> chunks;

	private static World Instance;

	public List<Chunk> chunksToLightUpdate = new List<Chunk>();

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

		CalculateLighting();
		firstLightPass = false;
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
		if (firstLightPass)
			return;

		CalculateLighting();
	}

	private void CalculateLighting()
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

		chunksToLightUpdate.Clear();

		// Apply lights
		for (int i = 0; i < lightSources.Count; i++)
		{
			if (lightSources[i].dirty)
				lightSources[i].UpdatePos();

			for (int j = 0; j < lightSources[i].affectedChunks.Count; j++)
			{
				// First pass on this chunk. Reset "canvas" and apply light from scratch
				bool firstPass = !chunksToLightUpdate.Contains(lightSources[i].affectedChunks[j]);

				if (firstPass)
					chunksToLightUpdate.Add(lightSources[i].affectedChunks[j]);

				// Mark chunks as dirty
				if (lightSources[i].dirty)
					lightSources[i].affectedChunks[j].MarkAllAsDirty();

				// Consider this chunk handled
				lightSources[i].affectedChunks[j].lightsToHandle--;

				// Last pass on this chunk. Begin applying vertex colors
				bool lastPass = lightSources[i].affectedChunks[j].lightsToHandle == 0;

				lightSources[i].affectedChunks[j].AddLight(lightSources[i], firstPass, lastPass);	
			}

			foreach (Chunk chunk in chunksToLightUpdate)
				chunk.UpdateLightVisuals();

			if (lightSources[i].dirty)
				lightSources[i].dirty = false;
		}
	}

	public static Chunk GetChunkFor(int x, int y, int z)
	{
		Vector3Int pos = new Vector3Int(x, y, z);
		Vector3Int dummy;

		foreach (Chunk chunk in Instance.chunks)
		{
			dummy = pos - chunk.position;

			if (chunk.ContainsPos(dummy.x, dummy.y, dummy.z))
				return chunk;
		}

		return null;
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
}
