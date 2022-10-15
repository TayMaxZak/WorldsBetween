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
		GenerateTerrain,
		GenerateFeatures,
		GenerateDecorators,
		MakeMesh,
		Done
	}

	// Data
	public ChunkType chunkType;
	protected Block[] blocks; // Discrete data on the block grid 
	protected BlockAttributes[] attributes; // Blended data, with a "pixel" at the corner of each block. Visually blended via vertex colors
	
	protected Color[] lighting; // Color data, with a pixel at the center of each block
	protected List<BlockLight> lights; // Light sources on the block grid
	protected List<BlockSound> sounds; // Light sources on the block grid

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

	public Chunk(Vector3Int position)
	{
		this.position = position;
	}

	public virtual void Init(int chunkSize)
	{
		chunkSizeBlocks = chunkSize;

		CreateCollections();

		didInit = true;
	}

	public virtual void CreateCollections()
	{
		blocks = new Block[chunkSizeBlocks * chunkSizeBlocks * chunkSizeBlocks];
		attributes = new BlockAttributes[chunkSizeBlocks * chunkSizeBlocks * chunkSizeBlocks];
		lighting = new Color[chunkSizeBlocks * chunkSizeBlocks * chunkSizeBlocks];
		lights = new List<BlockLight>();
		sounds = new List<BlockSound>();

		for (int x = 0; x < chunkSizeBlocks; x++)
		{
			for (int y = 0; y < chunkSizeBlocks; y++)
			{
				for (int z = 0; z < chunkSizeBlocks; z++)
				{
					SetBlock(x, y, z, BlockList.EMPTY);
					SetLighting(x, y, z, WorldLightAtlas.emptyColor);
					SetAttributes(x, y, z, BlockAttributes.empty);
				}
			}
		}
	}

	#region Generate
	public void AsyncGenerate(Modifier.ModifierStage stageToDo)
	{
		BkgThreadGenerate(this, System.EventArgs.Empty, stageToDo);
	}

	protected void BkgThreadGenerate(object sender, System.EventArgs e, Modifier.ModifierStage stageToDo)
	{
		isProcessing = true;

		BackgroundWorker bw = new BackgroundWorker();

		// What to do in the background thread
		bw.DoWork += new DoWorkEventHandler(
		delegate (object o, DoWorkEventArgs args)
		{
			Generate(stageToDo);
		});

		// What to do when worker completes its task
		bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
		delegate (object o, RunWorkerCompletedEventArgs args)
		{
			isProcessing = false;

			CacheDataFromBlocks(false);

			if (buildStage == BuildStage.GenerateTerrain)
				buildStage = BuildStage.GenerateFeatures;
			else if (buildStage == BuildStage.GenerateFeatures)
				buildStage = BuildStage.GenerateDecorators;
			else if (buildStage == BuildStage.GenerateDecorators)
				buildStage = BuildStage.MakeMesh;
			World.WorldBuilder.QueueNextStage(this);

			OnFinishProcStage();
		});

		bw.RunWorkerAsync();
	}

	protected virtual void DefaultBlock(int x, int y, int z)
	{
		Vector3Int coord = new Vector3Int(position.x + x, position.y + y, position.z + z);

		Block solid = BlockList.ROCK;
		Block top = coord.y <= World.GetWaterHeight() ? BlockList.LIGHT : BlockList.CARPET;

		//PersistentData data = PersistentData.GetInstanceForRead();
		//if (data && data.GetDepth() == 10)
		//	solid = BlockList.ARTIFICAL;

		if (coord.y >= World.GetWorldHeight(coord))
			SetBlock(x, y, z, BlockList.EMPTY);
		//else if (coord.y + 2 >= World.GetWorldHeight(coord + Vector3Int.up * 2))
		//	SetBlock(x, y, z, coord.y + 1 >= World.GetWorldHeight(coord + Vector3Int.up) ? top : BlockList.MUD);
		else
			SetBlock(x, y, z, coord.y + 1 >= World.GetWorldHeight(coord + Vector3Int.up) ? top : solid);
	}

	protected virtual void Generate(Modifier.ModifierStage stageToDo)
	{
		if (stageToDo == Modifier.ModifierStage.Terrain)
		{
			for (int x = 0; x < chunkSizeBlocks; x++)
			{
				for (int y = 0; y < chunkSizeBlocks; y++)
				{
					for (int z = 0; z < chunkSizeBlocks; z++)
					{
						DefaultBlock(x, y, z);
					}
				}
			}
		}

		List<Modifier> modifiers = World.GetModifiers();

		for (int i = 0; i < modifiers.Count; i++)
		{
			// Limit which modifiers are being applied by stage
			if (modifiers[i].stage == stageToDo)
				modifiers[i].ApplyModifier(this);
		}
	}

	public virtual void CacheDataFromBlocks(bool thisChunkOnly)
	{
		int airCount = 0;

		for (int x = 0; x < chunkSizeBlocks; x++)
		{
			for (int y = 0; y < chunkSizeBlocks; y++)
			{
				for (int z = 0; z < chunkSizeBlocks; z++)
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

		//WorldLightAtlas.Instance.SetAirCount(position + Vector3Int.one * chunkSizeWorld / 2, airCount);
	}

	// TODO: Fix for thisChunkOnly
	protected void FlagAdjacentsAsMaybeNearAir(int x, int y, int z)
	{
		Block block;

		// X-axis
		if (x < chunkSizeBlocks - 1)
			GetBlock(x + 1, y, z).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x + 1, position.y + y, position.z + z)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);

		if (x > 0)
			GetBlock(x - 1, y, z).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x - 1, position.y + y, position.z + z)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);

		// Y-axis
		if (y < chunkSizeBlocks - 1)
			GetBlock(x, y + 1, z).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x, position.y + y + 1, position.z + z)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);

		if (y > 0)
			GetBlock(x, y - 1, z).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x, position.y + y - 1, position.z + z)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);

		// Z-axis
		if (z < chunkSizeBlocks - 1)
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
			//Generate(Modifier.ModifierStage.Decorator);
			CacheDataFromBlocks(true);

			args.Result = MakeMesh();
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
				colors32 = data.colors32,
				subMeshCount = 2
			};
			newMesh.SetTriangles(data.triangles[0], 0);
			newMesh.SetTriangles(data.triangles[1], 1);

			// Apply new mesh
			chunkMesh.FinishMesh(newMesh);

			World.ApplySoundsAt(position, sounds);

			buildStage = BuildStage.Done;

			World.WorldBuilder.QueueNextStage(this);

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

	public Block GetBlock(int x, int y, int z)
	{
		return blocks[x * chunkSizeBlocks * chunkSizeBlocks + y * chunkSizeBlocks + z];
	}

	public Block SetBlock(int x, int y, int z, Block b)
	{
		return (blocks[x * chunkSizeBlocks * chunkSizeBlocks + y * chunkSizeBlocks + z] = b);
	}

	public Color GetLighting(int x, int y, int z)
	{
		return lighting[x * chunkSizeBlocks * chunkSizeBlocks + y * chunkSizeBlocks + z];
	}

	public Color SetLighting(int x, int y, int z, Color c)
	{
		return (lighting[x * chunkSizeBlocks * chunkSizeBlocks + y * chunkSizeBlocks + z] = c);
	}

	// TODO: Cache value in some way? Calculate it in advance???
	public Color GetAvgLighting()
	{
		Color average = WorldLightAtlas.emptyColor;
		float n = chunkSizeBlocks * chunkSizeBlocks * chunkSizeBlocks;

		for (int x = 0; x < chunkSizeBlocks; x++)
		{
			for (int y = 0; y < chunkSizeBlocks; y++)
			{
				for (int z = 0; z < chunkSizeBlocks; z++)
				{
					average += GetLighting(x, y, z);
				}
			}
		}

		return average / n;
	}

	public List<BlockLight> GetBlockLights()
	{
		return lights;
	}

	public void AddBlockLight(BlockLight light)
	{
		lights.Add(light);
	}

	public void RemoveBlockLightAt(Vector3Int pos)
	{
		BlockLight toRemove = null;
		foreach (BlockLight light in lights)
		{
			if (light.blockPos == pos)
				toRemove = light;
		}
		if (toRemove != null)
			lights.Remove(toRemove);
	}

	public void AddBlockSound(BlockSound sound)
	{
		sounds.Add(sound);
	}

	public void RemoveBlockSoundAt(Vector3Int pos)
	{
		BlockSound toRemove = null;
		foreach (BlockSound sound in sounds)
		{
			if (Vector3.SqrMagnitude(sound.pos - pos) < 0.5f)
				toRemove = sound;
		}
		if (toRemove != null)
			sounds.Remove(toRemove);
	}

	public BlockAttributes GetAttributes(int x, int y, int z)
	{
		return attributes[x * chunkSizeBlocks * chunkSizeBlocks + y * chunkSizeBlocks + z];
	}

	public BlockAttributes SetAttributes(int x, int y, int z, BlockAttributes a)
	{
		return (attributes[x * chunkSizeBlocks * chunkSizeBlocks + y * chunkSizeBlocks + z] = a);
	}
}
