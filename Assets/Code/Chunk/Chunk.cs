﻿using System.Collections;
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

	public Vector3Int position; // Coordinates of chunk


	public bool atEdge = false;

	public bool isProcessing = false;

	private int chunkSize = 8;

	private Block[,,] blocks;

	private ChunkBitArray corners;

	public ChunkMesh chunkMesh = new ChunkMesh();

	public ChunkGameObject go;

	public bool didInit = false;


	public void Init(int chunkSize)
	{
		this.chunkSize = chunkSize;

		CreateCollections();

		didInit = true;
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
					blocks[x, y, z] = new Block(x, y, z, 0);

					DefaultBlock(x, y, z);
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

			CacheDataFromBlocks();

			procStage = ProcStage.MakeMesh;
			World.WorldBuilder.QueueNextStage(this);

			Debug.DrawRay(position, Vector3.ClampMagnitude(World.GetRelativeOrigin() - position, 16), Color.green, 1);

			OnFinishProcStage();
		});

		bw.RunWorkerAsync();
	}

	public void SetBlocksToDefault()
	{
		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					DefaultBlock(x, y, z);
				}
			}
		}
	}

	private void DefaultBlock(int x, int y, int z)
	{
		Vector3Int coord = new Vector3Int(position.x + x, position.y + y, position.z + z);
		bool sky = coord.y >= -1;
		blocks[x, y, z].opacity = (byte)(sky ? 0 : 255);

		blocks[x, y, z].maybeNearAir = 0;
	}

	private void Generate()
	{
		List<Modifier> modifiers = World.GetModifiers();

		for (int i = 0; i < modifiers.Count; i++)
			ApplyModifier(modifiers[i]);
	}

	private void ApplyModifier(Modifier modifier)
	{
		modifier.ApplyModifier(this);
	}

	public void CacheDataFromBlocks()
	{
		int airCount = 0;

		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					// Only care if this block is an air block
					if (!blocks[x, y, z].IsAir())
					{
						FlagAllCornersTrue(x + position.x, y + position.y, z + position.z);

						continue;
					}

					airCount++;

					// Handle adjacent blocks for this block
					FlagAdjacentsAsMaybeNearAir(x, y, z);
				}
			}
		}

		WorldLightAtlas.Instance.SetAirCount(position + Vector3Int.one * chunkSize / 2, airCount);
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

	private void FlagAllCornersTrue(int x, int y, int z)
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

			procStage = ProcStage.Done;
			World.WorldBuilder.QueueNextStage(this);

			Debug.DrawRay(position, Vector3.ClampMagnitude(World.GetRelativeOrigin() - position, 16), Color.cyan, 1);

			OnFinishProcStage();
		});

		bw.RunWorkerAsync();
	}

	private ChunkMesh.MeshData MakeMesh()
	{
		return chunkMesh.MakeSurfaceAndMesh(blocks);
	}
	#endregion

	// Notify that this chunk has completed a stage of processing
	public void OnFinishProcStage()
	{
		World.WorldBuilder.ChunkFinishedProcStage();
	}

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
