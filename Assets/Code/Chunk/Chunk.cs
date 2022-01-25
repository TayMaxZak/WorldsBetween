using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;

[System.Serializable]
public class Chunk
{
	public enum ProcStage
	{
		Allocate,
		Generate,
		MakeMesh,
		Done
	}
	public ProcStage procStage = ProcStage.Allocate;

	public enum LightStage
	{
		Await,
		DirectLight,
		AmbientLight,
		Done
	}
	public LightStage lightStage = LightStage.Await;


	public Vector3Int position; // Coordinates of chunk


	public bool atEdge = false;

	public bool isProcessing = false;

	private int chunkSize = 8;

	private Block[,,] blocks;

	private ChunkBitArray corners;

	public ChunkMesh chunkMesh = new ChunkMesh();

	public ChunkGameObject go;


	// Represents where each light reaches. true = light, false = shadow
	//private Dictionary<LightSource, ChunkBitArray> lightToMaskMap = new Dictionary<LightSource, ChunkBitArray>();

	public void Init(int chunkSize)
	{
		this.chunkSize = chunkSize;

		CreateCollections();
	}

	public void SetPos(Vector3Int pos)
	{
		position = pos;
	}

	public void CreateCollections()
	{
		blocks = new Block[chunkSize, chunkSize, chunkSize];

		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					blocks[x, y, z] = new Block(x, y, z, 255);
				}
			}
		}

		corners = new ChunkBitArray(chunkSize, false);
	}

	#region Generate
	public void AsyncGenerate()
	{
		BkgThreadGenerate(this, System.EventArgs.Empty);
	}

	private void BkgThreadGenerate(object sender, System.EventArgs e)
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

			CacheAdjacentFlags();

			procStage = ProcStage.MakeMesh;
			World.Generator.QueueNextStage(this);
		});

		bw.RunWorkerAsync();
	}

	private void Generate()
	{
		List<NoiseModifier> nModifiers = World.GetNoiseModifiers();

		for (int i = 0; i < nModifiers.Count; i++)
			ApplyModifier(nModifiers[i]);

		List<RoomModifier> rModifiers = World.GetRoomModifiers();

		for (int i = 0; i < rModifiers.Count; i++)
			ApplyModifier(rModifiers[i]);
	}

	private void ApplyModifier(NoiseModifier modifier)
	{
		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					float strength = modifier.StrengthAt(x + position.x, y + position.y, z + position.z);

					bool passes = strength > modifier.boundary;

					if (!passes)
						continue;

					blocks[x, y, z].opacity = (byte)(modifier.addOrSub ? 255 : 0);
				}
			}
		}
	}

	private void ApplyModifier(RoomModifier modifier)
	{
		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					float strength = modifier.StrengthAt(x + position.x, y + position.y, z + position.z);

					bool passes = strength > 0.5f;

					if (!passes)
						continue;

					blocks[x, y, z].opacity = (byte)(modifier.addOrSub ? 255 : 0);
				}
			}
		}
	}

	public void CacheAdjacentFlags()
	{
		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					// Only care if this block is an air block
					if (!blocks[x, y, z].IsAir())
					{
						FlagAllCorners(x + position.x, y + position.y, z + position.z);

						continue;
					}

					// Handle adjacent blocks for this block
					FlagAdjacentsAsMaybeNearAir(x, y, z);
				}
			}
		}
	}

	private void FlagAdjacentsAsMaybeNearAir(int x, int y, int z)
	{
		Block block;

		// X-axis
		if (x < chunkSize - 1)
			blocks[x + 1, y, z].maybeNearAir = 255;
		else if ((block = World.GetBlockFor(position.x + x + 1, position.y + y, position.z + z)) != Block.empty)
			block.maybeNearAir = 255;

		if (x > 0)
			blocks[x - 1, y, z].maybeNearAir = 255;
		else if ((block = World.GetBlockFor(position.x + x - 1, position.y + y, position.z + z)) != Block.empty)
			block.maybeNearAir = 255;

		// Y-axis
		if (y < chunkSize - 1)
			blocks[x, y + 1, z].maybeNearAir = 255;
		else if ((block = World.GetBlockFor(position.x + x, position.y + y + 1, position.z + z)) != Block.empty)
			block.maybeNearAir = 255;

		if (y > 0)
			blocks[x, y - 1, z].maybeNearAir = 255;
		else if ((block = World.GetBlockFor(position.x + x, position.y + y - 1, position.z + z)) != Block.empty)
			block.maybeNearAir = 255;

		// Z-axis
		if (z < chunkSize - 1)
			blocks[x, y, z + 1].maybeNearAir = 255;
		else if ((block = World.GetBlockFor(position.x + x, position.y + y, position.z + z + 1)) != Block.empty)
			block.maybeNearAir = 255;

		if (z > 0)
			blocks[x, y, z - 1].maybeNearAir = 255;
		else if ((block = World.GetBlockFor(position.x + x, position.y + y, position.z + z - 1)) != Block.empty)
			block.maybeNearAir = 255;
	}

	private void FlagAllCorners(int x, int y, int z)
	{
		// Apply
		World.SetCorner(true, x, y, z);

		World.SetCorner(true, x + 1, y, z);
		World.SetCorner(true, x, y + 1, z);
		World.SetCorner(true, x, y, z + 1);

		World.SetCorner(true, x + 1, y + 1, z);
		World.SetCorner(true, x, y + 1, z + 1);
		World.SetCorner(true, x + 1, y, z + 1);

		World.SetCorner(true, x + 1, y + 1, z + 1);
	}
	#endregion

	#region Mesh
	public void AsyncMakeMesh()
	{
		BkgThreadMakeMesh(this, System.EventArgs.Empty);
	}

	private void BkgThreadMakeMesh(object sender, System.EventArgs e)
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

			//// TODO: Better way to do this?
			//float random = 45 + (SeedlessRandom.NextFloat() < 0.2f ? 45 : 0);
			//if (go && go.transform)
			//	go.transform.eulerAngles = new Vector3(SeedlessRandom.NextFloatInRange(-random, random), SeedlessRandom.NextFloatInRange(-random, random), SeedlessRandom.NextFloatInRange(-random, random));

			procStage = ProcStage.Done;
			lightStage = LightStage.DirectLight;
			World.Generator.QueueNextStage(this);

			//AsyncCalcLight();
		});

		bw.RunWorkerAsync();
	}

	private ChunkMesh.MeshData MakeMesh()
	{
		return chunkMesh.MakeSurfaceAndMesh(blocks);
	}
	#endregion

	// Utility
	public bool ContainsPos(int x, int y, int z)
	{
		return x >= 0 && chunkSize > x && y >= 0 && chunkSize > y && z >= 0 && chunkSize > z;
	}

	public Block GetBlock(int x, int y, int z)
	{
		return blocks[x, y, z];
	}

	public ChunkBitArray GetCorners()
	{
		return corners;
	}
}
