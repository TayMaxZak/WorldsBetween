using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;

[System.Serializable]
public class Chunk
{
	public enum ChunkType
	{
		Close,
		Far,
		Bkg
	}

	public enum BuildStage
	{
		Init,
		Generate,
		MakeMesh,
		Done
	}

	// Data
	public ChunkType chunkType;
	protected Block[] blocks;
	protected List<LightSource> lights;

	// Transform
	public Vector3Int position;
	protected int chunkSizeBlocks = 16;
	public int scaleFactor = 1;
	public int chunkSizeWorld = 16;

	// State
	public BuildStage buildStage = BuildStage.Init;
	public bool isProcessing = false;
	public bool didInit = false;

	// References
	public ChunkMesh chunkMesh = new ChunkMesh();
	public ChunkGameObject go;

	public virtual void Init(int chunkSize)
	{
		chunkSizeBlocks = chunkSize;
		chunkSizeWorld = chunkSize * scaleFactor;

		CreateCollections();

		didInit = true;
	}

	public void SetPos(Vector3Int pos)
	{
		position = pos;
	}

	public Block GetBlock(int x, int y, int z)
	{
		x /= scaleFactor; y /= scaleFactor; z /= scaleFactor;

		return blocks[x * chunkSizeBlocks * chunkSizeBlocks + y * chunkSizeBlocks + z];
	}

	public Block SetBlock(int x, int y, int z, Block b)
	{
		x /= scaleFactor; y /= scaleFactor; z /= scaleFactor;

		return (blocks[x * chunkSizeBlocks * chunkSizeBlocks + y * chunkSizeBlocks + z] = b);
	}

	public virtual void CreateCollections()
	{
		lights = new List<LightSource>();

		blocks = new Block[chunkSizeBlocks * chunkSizeBlocks * chunkSizeBlocks];

		for (int x = 0; x < chunkSizeWorld; x += scaleFactor)
		{
			for (int y = 0; y < chunkSizeWorld; y += scaleFactor)
			{
				for (int z = 0; z < chunkSizeWorld; z += scaleFactor)
				{
					SetBlock(x, y, z, BlockList.EMPTY);

					DefaultBlock(x, y, z);
				}
			}
		}
	}


	#region Generate
	public void AsyncGenerate()
	{
		BkgThreadGenerate(this, System.EventArgs.Empty);
	}

	protected void BkgThreadGenerate(object sender, System.EventArgs e)
	{
		isProcessing = true;

		BackgroundWorker bw = new BackgroundWorker();

		// What to do in the background thread
		bw.DoWork += new DoWorkEventHandler(
		delegate (object o, DoWorkEventArgs args)
		{
			Generate();
		});

		// What to do when worker completes its task
		bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
		delegate (object o, RunWorkerCompletedEventArgs args)
		{
			isProcessing = false;

			CacheDataFromBlocks();

			buildStage = BuildStage.MakeMesh;
			World.WorldBuilder.QueueNextStage(this);

			OnFinishProcStage();
		});

		bw.RunWorkerAsync();
	}

	protected virtual void DefaultBlock(int x, int y, int z)
	{
		Vector3Int coord = new Vector3Int(position.x + x, position.y + y, position.z + z);
		bool sky = coord.y >= World.GetWorldHeight();

		SetBlock(x, y, z, sky ? BlockList.EMPTY : BlockList.NATURAL);
	}

	protected virtual void Generate()
	{
		List<Modifier> modifiers = World.GetModifiers();

		for (int i = 0; i < modifiers.Count; i++)
			modifiers[i].ApplyModifier(this);
	}

	public virtual void CacheDataFromBlocks()
	{
		int airCount = 0;

		for (byte x = 0; x < chunkSizeWorld; x++)
		{
			for (byte y = 0; y < chunkSizeWorld; y++)
			{
				for (byte z = 0; z < chunkSizeWorld; z++)
				{
					// Only care if this block is a transparent block
					if (GetBlock(x, y, z).IsOpaque())
						continue;

					airCount++;

					// Handle adjacent blocks for this block
					FlagAdjacentsAsMaybeNearAir(x, y, z);
				}
			}
		}

		WorldLightAtlas.Instance.SetAirCount(position + Vector3Int.one * chunkSizeWorld / 2, airCount);
	}

	protected void FlagAdjacentsAsMaybeNearAir(int x, int y, int z)
	{
		Block block;

		// X-axis
		if (x < chunkSizeWorld - 1)
			GetBlock(x + 1, y, z).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x + 1, position.y + y, position.z + z)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);

		if (x > 0)
			GetBlock(x - 1, y, z).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x - 1, position.y + y, position.z + z)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);

		// Y-axis
		if (y < chunkSizeWorld - 1)
			GetBlock(x, y + 1, z).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x, position.y + y + 1, position.z + z)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);

		if (y > 0)
			GetBlock(x, y - 1, z).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x, position.y + y - 1, position.z + z)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);

		// Z-axis
		if (z < chunkSizeWorld - 1)
			GetBlock(x, y, z + 1).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x, position.y + y, position.z + z + 1)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);

		if (z > 0)
			GetBlock(x, y, z - 1).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x, position.y + y, position.z + z - 1)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);
	}
	#endregion

	#region Mesh
	public void AsyncMakeMesh()
	{
		BkgThreadMakeMesh(this, System.EventArgs.Empty);
	}

	protected virtual void BkgThreadMakeMesh(object sender, System.EventArgs e)
	{
		isProcessing = true;

		BackgroundWorker bw = new BackgroundWorker();

		// What to do in the background thread
		bw.DoWork += new DoWorkEventHandler(
		delegate (object o, DoWorkEventArgs args)
		{
			args.Result = MakeMesh();

			if (chunkType == ChunkType.Close)
			{
				for (int i = 0; i < SeedlessRandom.NextIntInRange(40, 40); i++)
				{
					Vector3Int pos = new Vector3Int(SeedlessRandom.NextIntInRange(0, chunkSizeWorld), SeedlessRandom.NextIntInRange(0, chunkSizeWorld), SeedlessRandom.NextIntInRange(0, chunkSizeWorld)) + position;

					// Not inside an opaque block, and located on top of a rigid block
					if (!World.GetBlock(pos).IsOpaque() && (World.GetBlock(pos + Vector3Int.down).IsRigid() || World.GetBlock(pos + Vector3Int.up).IsRigid()))
					{
						// Check existing positions
						bool overlapping = false;
						foreach (LightSource l in lights)
						{
							if (l.pos == pos)
								overlapping = true;
						}
						if (overlapping)
							break;

						bool cond = (pos.y > World.GetWaterHeight() && World.GetBlock(pos + Vector3Int.down).IsRigid());

						// Create light
						if (!cond || SeedlessRandom.NextFloat() > 0.25f)
						{
							lights.Add(new LightSource()
							{
								pos = pos,
								// Randomize color
								lightColor = cond ?
								(Color.Lerp(
									(SeedlessRandom.NextFloat() < 0.1f ? Color.white : LightSource.colorOrange),
									(SeedlessRandom.NextFloat() < 0.8f ? LightSource.colorGold : LightSource.colorRed),
									SeedlessRandom.NextFloatInRange(0, 1f) * SeedlessRandom.NextFloatInRange(0.25f, 0.75f)
								)) :
								(5 * Color.Lerp(
									(SeedlessRandom.NextFloat() < 0.05f ? LightSource.colorGreen : LightSource.colorCyan),
									(SeedlessRandom.NextFloat() < 0.05f ? LightSource.colorOrange : LightSource.colorBlue),
									SeedlessRandom.NextFloatInRange(0, 1f)
								)),
								intensity = cond ? 1f : 0.3f,
								noise = cond ? 0.67f : 1f
							});
						}
					}
				}
			}
		});

		// What to do when worker completes its task
		bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
		delegate (object o, RunWorkerCompletedEventArgs args)
		{
			isProcessing = false;

			// Load mesh data from thread
			ChunkMesh.MeshData data = (ChunkMesh.MeshData)args.Result;

			Mesh newMesh = new Mesh
			{
				vertices = data.vertices,
				normals = data.normals,
				uv = data.uv,
				subMeshCount = 2
			};
			newMesh.SetTriangles(data.triangles[0], 0);
			newMesh.SetTriangles(data.triangles[1], 1);

			// Apply new mesh
			chunkMesh.FinishMesh(newMesh);

			buildStage = BuildStage.Done;

			World.WorldBuilder.QueueNextStage(this);

			World.SetLightsAt(position, lights);

			OnFinishProcStage();
		});

		bw.RunWorkerAsync();
	}

	private ChunkMesh.MeshData MakeMesh()
	{
		return chunkMesh.MakeMesh();
	}
	#endregion

	// Notify that this chunk has completed a stage of processing
	public virtual void OnFinishProcStage()
	{
		World.WorldBuilder.ChunkFinishedProcStage();
	}
}
