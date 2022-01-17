using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class World : MonoBehaviour
{
	private static World Instance;

	[Header("References")]
	[SerializeField]
	private Vector3Int relativeOrigin;

	[SerializeField]
	private GameObject waterSystem;

	[SerializeField]
	private Sun sunObject;

	private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

	[SerializeField]
	private List<NoiseModifier> modifiers = new List<NoiseModifier>();

	private Dictionary<Vector3Int, LinkedList<LightSource>> lightSources = new Dictionary<Vector3Int, LinkedList<LightSource>>();

	[SerializeField]
	private WorldGenerator generator;
	public static WorldGenerator Generator;

	[Header("General Settings")]
	[SerializeField]
	private bool randomizeSeed = false;
	[SerializeField]
	private int seed = 0;
	[SerializeField]
	private int chunkSize = 8;

	public bool isInfinite = true;

	[Header("World Settings")]
	[SerializeField]
	private int waterHeight = 0;

	private void Awake()
	{
		// Ensure singleton
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}
		else
			Instance = this;

		Generator = generator;
		Generator.Init();

		WorldInit();
	}

	private void WorldInit()
	{
		// Pick a seed, then use it to initialize RNG
		if (randomizeSeed)
			seed = SeedlessRandom.NextIntInRange(int.MinValue, int.MaxValue);
		Random.InitState(seed);

		foreach (NoiseModifier mod in modifiers)
			mod.Init();

		// Scene setup
		WaterFollow(relativeOrigin);

		if (sunObject)
		{
			sunObject.Init();
			RegisterLight(sunObject.lightSource);
		}
	}

	private void Start()
	{
		// First batch of chunks
		Generator.StartGen();
	}

	private void Update()
	{
		Generator.ContinueGenerating();
	}

	[ContextMenu("Restart Gen")]
	public void RestartGen()
	{
		lightSources.Clear();
		chunks.Clear();

		randomizeSeed = true;
		WorldInit();

		Generator.StartGen();
	}

	[ContextMenu("Cancel Gen")]
	public void CancelGen()
	{
		Generator.StopGen();
	}

	public static void RegisterModifier(NoiseModifier modifier)
	{
		Instance.modifiers.Add(modifier);
	}

	public static void RemoveModifier(NoiseModifier modifier)
	{
		Instance.modifiers.Remove(modifier);
	}

	public static List<NoiseModifier> GetModifiers()
	{
		return Instance.modifiers;
	}

	public static void WaterFollow(Vector3 pos)
	{
		Instance.waterSystem.transform.position = new Vector3(pos.x, Instance.waterHeight, pos.z);
	}

	public static void RegisterLight(LightSource light)
	{
		if (Instance.sunObject && light != Instance.sunObject.lightSource)
			UpdateLight(Instance.sunObject.lightSource, false);

		light.FindAffectedChunkCoords();

		foreach (Vector3Int coord in light.GetAffectedChunkCoords())
		{
			Instance.lightSources.TryGetValue(coord, out LinkedList<LightSource> ls);

			// First light added to this chunk
			if (ls == null)
				Instance.lightSources.Add(coord, ls = new LinkedList<LightSource>());

			if (!ls.Contains(light))
				ls.AddLast(light);
		}
	}

	public static void RemoveLight(LightSource light)
	{
		foreach (Vector3Int coord in light.GetAffectedChunkCoords())
		{
			Instance.lightSources.TryGetValue(coord, out LinkedList<LightSource> ls);

			if (ls != null)
				ls.Remove(light);
		}
	}

	public static void UpdateLight(LightSource light, bool recalcLight)
	{
		if (Instance.sunObject && light != Instance.sunObject.lightSource)
			UpdateLight(Instance.sunObject.lightSource, false);

		List<Vector3Int> oldChunks = light.FindAffectedChunkCoords();

		// Some lights do not track old chunks
		if (oldChunks != null)
		{
			foreach (Vector3Int coord in oldChunks)
			{
				Instance.lightSources.TryGetValue(coord, out LinkedList<LightSource> ls);

				if (ls != null)
					ls.Remove(light);

				if (recalcLight)
				{
					Chunk chunk = GetChunkFor(coord);
					if (chunk != null)
					{
						chunk.QueueLightUpdate();
						if (light != Instance.sunObject.lightSource)
							chunk.NeedsLightDataRecalc(light);
					}
				}
			}
		}

		foreach (Vector3Int coord in light.GetAffectedChunkCoords())
		{
			Instance.lightSources.TryGetValue(coord, out LinkedList<LightSource> ls);

			// First light added to this chunk
			if (ls == null)
				Instance.lightSources.Add(coord, ls = new LinkedList<LightSource>());

			if (!ls.Contains(light))
				ls.AddLast(light);

			if (recalcLight)
			{
				Chunk chunk = GetChunkFor(coord);
				if (chunk != null)
				{
					chunk.QueueLightUpdate();
					if (light != Instance.sunObject.lightSource)
						chunk.NeedsLightDataRecalc(light);
				}
			}
		}
	}

	public static void AddSunlight(Chunk newChunk)
	{
		if (!Instance.sunObject)
			return;

		LightSource sun = Instance.sunObject.lightSource;
		Vector3Int coord = newChunk.position;

		Instance.lightSources.TryGetValue(coord, out LinkedList<LightSource> ls);

		if (ls == null)
			Instance.lightSources.Add(coord, ls = new LinkedList<LightSource>());

		if (!ls.Contains(sun))
			ls.AddLast(sun);
	}

	public static LinkedList<LightSource> GetLightsFor(Chunk chunk)
	{
		Instance.lightSources.TryGetValue(chunk.position, out LinkedList<LightSource> ls);

		return ls;
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

	public static Chunk GetChunkFor(Vector3Int pos)
	{
		return GetChunkFor(pos.x, pos.y, pos.z);
	}

	public static Block GetBlockFor(int x, int y, int z)
	{
		Chunk chunk = GetChunkFor(x, y, z);

		if (chunk == null || chunk.procStage == Chunk.ProcStage.Allocate)
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

	public static Vector3Int GetRelativeOrigin()
	{
		return Instance.relativeOrigin;
	}

	public static Dictionary<Vector3Int, Chunk> GetChunks()
	{
		return Instance.chunks;
	}

	public static Dictionary<Vector3Int, LinkedList<LightSource>>.KeyCollection GetLitChunkCoords()
	{
		return Instance.lightSources.Keys;
	}

	public static bool IsInfinite()
	{
		return Instance.isInfinite;
	}

	public static int GetWaterHeight()
	{
		return Instance.waterHeight;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Utils.colorDarkGrayBlue;

		Gizmos.DrawWireCube(transform.position + Vector3.one * chunkSize / 2, Vector3.one * (1 + generator.GetGenRange() * 2) * chunkSize);
	}
}
