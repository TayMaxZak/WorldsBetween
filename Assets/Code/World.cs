using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

public class World : MonoBehaviour
{
	private static World Instance;

	[Header("References")]
	[SerializeField]
	private Transform player;

	[SerializeField]
	private Chunk chunkPrefab;
	private Dictionary<Vector3Int, Chunk> chunks;

	private List<Modifier> modifiers;

	[SerializeField]
	private Transform lightRoot;
	[SerializeField]
	private LightSource prefabLight;
	private List<LightSource> lightSources = new List<LightSource>();

	[Header("World Settings")]
	[SerializeField]
	private bool randomizeSeed = false;
	[SerializeField]
	private int seed = 0;
	[SerializeField]
	private int chunkSize = 8;

	[Header("Performance")]
	[SerializeField]
	private Timer chunkGenTimer = null;
	[SerializeField]
	private Timer lightUpdateTimer = null;
	[SerializeField]
	private int lightUpdateSize = 32;

	private bool initialWorldGen = true;
	private bool initialChunks = true;

	[Header("Generation")]
	[SerializeField]
	private int nearPlayerGenRange = 4;

	// Extras
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
		CreateChunksNearPlayer(2);

		ApplyModifiers();

		CalculateLighting();

		initialWorldGen = false;
	}

	private void CreateChunksNearPlayer(int range)
	{
		range *= chunkSize;

		Vector3Int startPos = new Vector3Int(
			Mathf.FloorToInt(player.position.x / chunkSize) * chunkSize,
			Mathf.FloorToInt(player.position.y / chunkSize) * chunkSize,
			Mathf.FloorToInt(player.position.z / chunkSize) * chunkSize
		);

		for (int x = startPos.x - range; x <= startPos.x + range; x += chunkSize)
		{
			for (int y = startPos.y - range - (initialChunks ? 20 * chunkSize : 0); y <= startPos.y + range; y += chunkSize)
			{
				for (int z = startPos.z - range; z <= startPos.z + range; z += chunkSize)
				{
					Vector3Int chunkPos = new Vector3Int(x, y, z);

					// Chunk already exists at this location
					if (chunks.ContainsKey(chunkPos))
						continue;

					// Create and register chunk
					Chunk chunk = Instantiate(chunkPrefab, chunkPos, Quaternion.identity, transform);

					chunks.Add(chunk.position, chunk);

					//// Add a random light to this chunk
					//LightSource light = Instantiate(prefabLight, new Vector3(
					//	chunkPos.x + Random.value * chunkSize,
					//	chunkPos.y + Random.value * chunkSize,
					//	chunkPos.z + Random.value * chunkSize),
					//Quaternion.identity, lightRoot);

					//light.colorTemp = Random.Range(-10, 10);
				}
			}
		}

		if (initialChunks)
			initialChunks = false;
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
				if (entry.Value.genStage != Chunk.GenStage.Allocated)
					continue;

				entry.Value.ApplyModifier(modifiers[i], i == 0, i == modifiers.Count - 1);

				if (i == modifiers.Count - 1)
					entry.Value.genStage = Chunk.GenStage.Generated;
			}
		}
	}

	private void Update()
	{
		if (initialWorldGen)
			return;

		UpdateChunkCreation();

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

	private void UpdateChunkCreation()
	{
		chunkGenTimer.Increment(Time.deltaTime);

		if (chunkGenTimer.Expired())
			chunkGenTimer.Reset();
		else
			return;

		CreateChunksNearPlayer(nearPlayerGenRange);

		ApplyModifiers();
	}

	public static void QueueNextStage(Chunk chunk, Chunk.GenStage stage)
	{

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

			chunksToLightCleanup = lightSources[i].FindAffectedChunks();

			//foreach (Chunk chunk in chunksToLightCleanup)
			//{
			//	if (!lightSources[i].affectedChunks.Contains(chunk))
			//		chunk.CleanupLight();
			//}

			// Should this light activate?
			bool notReady = false;

			foreach (Chunk chunk in lightSources[i].affectedChunks)
			{
				if (chunk.genStage != Chunk.GenStage.Meshed && chunk.genStage != Chunk.GenStage.Lit)
				{
					notReady = true;
					break;
				}
			}

			if (notReady)
				continue;

			// Go through each affected chunk
			foreach (Chunk chunk in lightSources[i].affectedChunks)
			{
				if (chunk.genStage != Chunk.GenStage.Meshed && chunk.genStage != Chunk.GenStage.Lit)
					continue;

				// First pass on this chunk. Reset "canvas" and apply light from scratch
				bool firstPass = !chunksToLightUpdate.Contains(chunk);

				if (firstPass)
					chunksToLightUpdate.Add(chunk);

				// Mark chunk as dirty
				if (chunk.lightsToHandle > 0)
				{
					if (firstPass)
						chunk.MarkAsDirtyForLight();

					// This chunk can consider this light handled
					chunk.lightsToHandle--;

					// Last pass on this chunk? If so, begin applying vertex colors
					bool lastPass = chunk.lightsToHandle == 0;

					chunk.AddLight(lightSources[i], firstPass, lastPass);

					if (lastPass)
						chunk.genStage = Chunk.GenStage.Lit;
				}
			}

			if (lightSources[i].dirty && !notReady)
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

	public static int GetLightUpdateSize()
	{
		return Instance.lightUpdateSize;
	}
}
