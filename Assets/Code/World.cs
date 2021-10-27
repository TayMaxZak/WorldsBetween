using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
	[Header("World Settings")]
	[SerializeField]
	private int seed = 0;
	[SerializeField]
	private int worldExtent = 4;

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

	// Modifiers
	private List<Modifier> modifiers;

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

		Random.InitState(seed);

		chunks = new Dictionary<Vector3Int, Chunk>();

		modifiers = new List<Modifier>();
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
		int size = 8;

		for (int x = -worldExtent; x <= worldExtent; x++)
		{
			for (int y = -worldExtent; y <= worldExtent; y++)
			{
				for (int z = -worldExtent; z <= worldExtent; z++)
				{
					Chunk chunk = Instantiate(chunkPrefab, new Vector3(x * size, y * size, z * size), Quaternion.identity, transform);

					chunk.UpdatePos();
					chunks.Add(chunk.position, chunk);

					Instantiate(prefabLight, new Vector3(
						x * size + Random.value * size,
						y * size + Random.value * size,
						z * size + Random.value * size),
					Quaternion.identity, lightRoot);
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

	private void Generate()
	{
		foreach (KeyValuePair<Vector3Int, Chunk> entry in chunks)
		{
			for (int i = 0; i < modifiers.Count; i++)
			{
				modifiers[i].Init();

				entry.Value.ApplyModifier(modifiers[i], i == 0, i == modifiers.Count - 1);
			}

			entry.Value.UpdateOpacityVisuals();
		}

		foreach (KeyValuePair<Vector3Int, Chunk> entry in chunks)
		{
			entry.Value.CacheNearAir();
		}
	}

	private void Update()
	{
		if (firstLightPass)
			return;

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
		float chunkSize = 8;

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

	public static int GetUpdateSize()
	{
		return Instance.lightUpdateSize;
	}
}
