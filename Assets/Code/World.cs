﻿using System.Collections;
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
	[SerializeField]
	private Chunk chunkPrefab;
	private Dictionary<Vector3Int, Chunk> chunks;

	private static World Instance;

	public List<Chunk> chunksToLightUpdate = new List<Chunk>();
	public List<Chunk> chunksToLightCleanup = new List<Chunk>();

	private void Awake()
	{
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}
		else
			Instance = this;

		chunks = new Dictionary<Vector3Int, Chunk>();

		lightSources = new List<LightSource>(lightRoot.GetComponentsInChildren<LightSource>());

		carvers = new List<Carver>(carverRoot.GetComponentsInChildren<Carver>());
	}

	private void Start()
	{
		CreateChunks();

		Generate();

		CalculateLighting();
		firstLightPass = false;
	}

	private void CreateChunks()
	{
		int extent = 4;
		int size = 8;

		for (int x = -extent; x <= extent; x++)
		{
			for (int y = -extent; y <= extent; y++)
			{
				for (int z = -extent; z <= extent; z++)
				{
					Chunk chunk = Instantiate(chunkPrefab, new Vector3(x * size, y * size, z * size), Quaternion.identity, transform);

					chunk.UpdatePos();
					chunks.Add(chunk.position, chunk);
				}
			}
		}
	}

	private void Generate()
	{
		foreach (KeyValuePair<Vector3Int, Chunk> entry in chunks)
		{
			for (int i = 0; i < carvers.Count; i++)
			{
				carvers[i].UpdatePos();

				entry.Value.ApplyCarver(carvers[i], i == 0);
			}

			entry.Value.UpdateOpacityVisuals();
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
			lightSources[i].UpdatePosition();

			// Update affected chunks and clean up old ones
			if (lightSources[i].dirty)
			{
				chunksToLightCleanup = lightSources[i].FindAffectedChunks();

				//foreach (Chunk chunk in chunksToLightCleanup)
				//	chunk.CleanupLight();
			}

			// Go through each affected chunk
			foreach (Chunk chunk in lightSources[i].affectedChunks)
			{
				// First pass on this chunk. Reset "canvas" and apply light from scratch
				bool firstPass = !chunksToLightUpdate.Contains(chunk);

				if (firstPass)
					chunksToLightUpdate.Add(chunk);

				// Mark chunk as dirty
				if (lightSources[i].dirty)
				{
					if (firstPass)
						chunk.MarkAsDirtyForLight();

					// This chunk can consider this light handled
					chunk.lightsToHandle--;

					// Last pass on this chunk? If so, begin applying vertex colors
					bool lastPass = chunk.lightsToHandle == 0;

					chunk.AddLight(lightSources[i], firstPass, lastPass);
				}
			}

			if (lightSources[i].dirty)
				lightSources[i].dirty = false;
		}

		foreach (Chunk chunk in chunksToLightUpdate)
		{
			chunk.UpdateLightVisuals();
		}
	}

	public static Chunk GetChunkFor(int x, int y, int z)
	{
		float chunkSize = 8;

		Instance.chunks.TryGetValue(new Vector3Int(
			Mathf.FloorToInt(x / chunkSize) * (int)chunkSize,
			Mathf.FloorToInt(y / chunkSize) * (int)chunkSize,
			Mathf.FloorToInt(z / chunkSize) * (int)chunkSize
		),
		out Chunk chunk);

		return chunk;
	}

	public static Block GetBlockFor(Vector3Int pos)
	{
		Chunk chunk = GetChunkFor(pos.x, pos.y, pos.z);

		if (chunk == null)
			return null;

		Vector3Int dummy;

		dummy = pos - chunk.position;

		if (chunk.ContainsPos(dummy.x, dummy.y, dummy.z))
			return chunk.GetBlock(dummy.x, dummy.y, dummy.z);
		else
			return null;
	}
}
