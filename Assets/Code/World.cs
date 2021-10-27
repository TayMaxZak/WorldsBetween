﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
	[SerializeField]
	private Transform player;

	[Header("World Settings")]
	[SerializeField]
	private bool randomizeSeed = false;
	[SerializeField]
	private int seed = 0;
	[SerializeField]
	private int nearPlayerGenRange = 4;
	[SerializeField]
	private int chunkSize = 8;

	[Header("Lighting")]
	[SerializeField]
	private Transform lightRoot;
	[SerializeField]
	private Timer lightUpdateTimer = null;
	[SerializeField]
	private int lightUpdateSize = 32;

	[SerializeField]
	private LightSource prefabLight;
	private List<LightSource> lightSources = new List<LightSource>();

	private bool firstLightPass = true;

	[Header("Generation")]
	private List<Modifier> modifiers;

	// Chunks
	[SerializeField]
	private Timer chunkGenTimer = null;

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

		if (randomizeSeed)
			seed = Random.Range(int.MinValue, int.MaxValue);
		Random.InitState(seed);

		chunks = new Dictionary<Vector3Int, Chunk>();

		modifiers = new List<Modifier>();

		chunkGenTimer.Reset();
	}

	private void Start()
	{
		CreateChunksNearPlayer(nearPlayerGenRange, true);

		ApplyModifiers();

		CalculateLighting();
		firstLightPass = false;
	}

	private void CreateChunksNearPlayer(int range, bool dod)
	{
		range *= chunkSize;

		Vector3Int startPos = new Vector3Int(
					Mathf.FloorToInt(player.position.x / chunkSize) * (int)chunkSize,
					Mathf.FloorToInt(player.position.y / chunkSize) * (int)chunkSize,
					Mathf.FloorToInt(player.position.z / chunkSize) * (int)chunkSize
		);

		for (int x = startPos.x - range; x <= startPos.x + range; x += chunkSize)
		{
			for (int y = startPos.y - range; y <= startPos.y + range; y += chunkSize)
			{
				for (int z = startPos.z - range; z <= startPos.z + range; z += chunkSize)
				{
					Vector3Int chunkPos = new Vector3Int(x, y, z);

					if (chunks.ContainsKey(chunkPos))
					{
						Debug.LogWarning("Chunk already exists at " + chunkPos + "!");
						continue;
					}

					if (!dod)
						continue;

					Chunk chunk = Instantiate(chunkPrefab, chunkPos, Quaternion.identity, transform);
					chunk.chunkSize = chunkSize;
					chunk.Init();

					chunks.Add(chunk.position, chunk);

					LightSource light = Instantiate(prefabLight, new Vector3(
						chunkPos.x + Random.value * chunkSize,
						chunkPos.y + Random.value * chunkSize,
						chunkPos.z + Random.value * chunkSize),
					Quaternion.identity, lightRoot);

					light.colorTemp = Random.Range(-10, 10);
				}
			}
		}
	}

	public static void RegisterModifier(Modifier modifier)
	{
		Instance.modifiers.Add(modifier);
	}

	public static void RemoveModifier(Modifier modifier)
	{
		Instance.modifiers.Remove(modifier);
	}

	private void ApplyModifiers()
	{
		for (int i = 0; i < modifiers.Count; i++)
		{
			modifiers[i].Init();

			foreach (KeyValuePair<Vector3Int, Chunk> entry in chunks)
			{
				entry.Value.ApplyModifier(modifiers[i], i == 0, i == modifiers.Count - 1);
			}
		}

		foreach (KeyValuePair<Vector3Int, Chunk> entry in chunks)
		{
			entry.Value.UpdateOpacityVisuals();
			entry.Value.CacheNearAir();
		}
	}

	private void Update()
	{
		if (firstLightPass)
			return;

		GenChunks();

		CalculateLighting();
	}

	public static void RegisterLight(LightSource light)
	{
		Instance.lightSources.Add(light);
	}

	public static void RemoveLight(LightSource light)
	{
		Instance.lightSources.Remove(light);
	}

	private void GenChunks()
	{
		chunkGenTimer.Increment(Time.deltaTime);

		bool doChunkGen = chunkGenTimer.Expired();

		if (doChunkGen)
			chunkGenTimer.Reset();

		if (!doChunkGen)
			return;

		CreateChunksNearPlayer(nearPlayerGenRange, false);
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

		//chunksToLightUpdate.Clear();

		// Apply lights
		for (int i = 0; i < lightSources.Count; i++)
		{
			lightSources[i].UpdatePosition();

			if (!lightSources[i].dirty)
				continue;

			// Update affected chunks and clean up old ones
			if (lightSources[i].dirty)
			{
				chunksToLightCleanup = lightSources[i].FindAffectedChunks();

				foreach (Chunk chunk in chunksToLightCleanup)
				{
					if (!lightSources[i].affectedChunks.Contains(chunk))
						chunk.CleanupLight();
				}
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

		//foreach (KeyValuePair<Vector3Int, Chunk> entry in chunks)
		//{
		//	entry.Value.UpdateLightVisuals();
		//}

		foreach (Chunk chunk in chunksToLightUpdate)
		{
			chunk.UpdateLightVisuals();
		}
	}

	public static Chunk GetChunkFor(int x, int y, int z)
	{
		float chunkSize = Instance.chunkSize;

		Instance.chunks.TryGetValue(new Vector3Int(
			Mathf.FloorToInt(x / chunkSize) * (int)chunkSize,
			Mathf.FloorToInt(y / chunkSize) * (int)chunkSize,
			Mathf.FloorToInt(z / chunkSize) * (int)chunkSize
		),
		out Chunk chunk);

		return chunk;
	}

	public static Block GetBlockFor(int x, int y, int z)
	{
		Chunk chunk = GetChunkFor(x, y, z);

		if (chunk == null)
			return Block.empty;

		return chunk.GetBlock(
			x - chunk.position.x,
			y - chunk.position.y,
			z - chunk.position.z
		);
	}

	public static Block GetBlockFor(Vector3Int pos)
	{
		return GetBlockFor(pos.x, pos.y, pos.z);
	}

	public static int GetChunkSize()
	{
		return Instance.chunkSize;
	}

	public static int GetUpdateSize()
	{
		return Instance.lightUpdateSize;
	}
}
