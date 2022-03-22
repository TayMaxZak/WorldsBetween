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

	// Transform
	public Vector3Int position;
	protected int chunkSize = 16;
	protected int scaleFactor = 1;

	// State
	public BuildStage buildStage = BuildStage.Init;
	public bool isProcessing = false;
	//public bool atEdge = false;
	public bool didInit = false;

	// References
	public ChunkMesh chunkMesh = new ChunkMesh();
	public ChunkGameObject go;

	public virtual void Init(int chunkSize, int scaleFactor)
	{
		this.chunkSize = chunkSize;
		this.scaleFactor = scaleFactor;

		CreateCollections();

		didInit = true;
	}

	public void SetPos(Vector3Int pos)
	{
		position = pos;
	}

	public Block GetBlock(int x, int y, int z)
	{
		return blocks[x * chunkSize * chunkSize + y * chunkSize + z];
	}

	public Block SetBlock(int x, int y, int z, Block b)
	{
		return (blocks[x * chunkSize * chunkSize + y * chunkSize + z] = b);
	}

	public virtual void CreateCollections()
	{
		blocks = new Block[chunkSize * chunkSize * chunkSize];

		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
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
		bool sky = coord.y >= 0;

		SetBlock(x, y, z, sky ? BlockList.EMPTY : BlockList.FILLED);
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

		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
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

		WorldLightAtlas.Instance.SetAirCount(position + Vector3Int.one * chunkSize / 2, airCount);
	}

	protected void FlagAdjacentsAsMaybeNearAir(int x, int y, int z)
	{
		Block block;

		// X-axis
		if (x < chunkSize - 1)
			GetBlock(x + 1, y, z).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x + 1, position.y + y, position.z + z)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);

		if (x > 0)
			GetBlock(x - 1, y, z).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x - 1, position.y + y, position.z + z)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);

		// Y-axis
		if (y < chunkSize - 1)
			GetBlock(x, y + 1, z).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x, position.y + y + 1, position.z + z)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);

		if (y > 0)
			GetBlock(x, y - 1, z).SetNeedsMesh(true);
		else if ((block = World.GetBlock(position.x + x, position.y + y - 1, position.z + z)) != BlockList.EMPTY)
			block.SetNeedsMesh(true);

		// Z-axis
		if (z < chunkSize - 1)
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
