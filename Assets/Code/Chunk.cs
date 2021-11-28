﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;

[System.Serializable]
public class Chunk
{
	public enum GenStage
	{
		Allocate,
		Generate,
		MakeSurface,
		CalcLight,
		AmbientLight,
		ApplyVertexColors,
		Ready
	}
	public GenStage genStage = GenStage.Allocate;


	public Vector3Int position; // Coordinates of chunk


	public bool atEdge = false;

	public bool isProcessing = false;

	private int chunkSize = 8;

	private Block[,,] blocks;

	private LinkedList<BlockSurface>[,,] surfaces;

	public ChunkMesh chunkMesh = new ChunkMesh();


	// Represents where each light reaches. true = light, false = shadow
	private Dictionary<LightSource, ChunkBitArray> shadowBits = new Dictionary<LightSource, ChunkBitArray>();

	[SerializeField]
	private AmbientLightNode ambientLight;

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

		surfaces = new LinkedList<BlockSurface>[chunkSize, chunkSize, chunkSize];

		ambientLight = new AmbientLightNode(new Vector3Int(position.x + chunkSize / 2, position.y + chunkSize / 2, position.z + chunkSize / 2), chunkSize);
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

			CacheMaybeFlags();

			genStage = GenStage.MakeSurface;
			World.Generator.QueueNextStage(this);
		});

		bw.RunWorkerAsync();
	}

	private void Generate()
	{
		List<Noise> modifiers = World.GetModifiers();

		for (int i = 0; i < modifiers.Count; i++)
			ApplyModifier(modifiers[i], i == 0, i == modifiers.Count - 1);
	}

	private void ApplyModifier(Noise modifier, bool firstPass, bool lastPass)
	{
		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					float newOpacity = (firstPass ? 1 : blocks[x, y, z].opacity / 255f);

					newOpacity -= modifier.StrengthAt(x + position.x, y + position.y, z + position.z);

					blocks[x, y, z].opacity = (byte)(Mathf.Clamp01(newOpacity) * 255);

					if (lastPass && y + position.y > 6)
						blocks[x, y, z].opacity = (byte)Mathf.Clamp(blocks[x, y, z].opacity - 16, 0, 255);
				}
			}
		}
	}

	public void CacheMaybeFlags()
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
						continue;
					}

					// TODO: ???????????????
					//blocks[x, y, z].maybeNearAir = 255;

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

		ChunkMesh.MeshData blockMesh = new ChunkMesh.MeshData(BlockModels.GetModelFor(0).faces[0].faceMesh);

		// What to do in the background thread
		bw.DoWork += new DoWorkEventHandler(
		delegate (object o, DoWorkEventArgs args)
		{
			args.Result = MakeMesh(blockMesh);
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
				triangles = data.triangles,
				uv = data.uv
			};

			// Apply new mesh
			chunkMesh.FinishMesh(newMesh);

			genStage = GenStage.CalcLight;
			World.Generator.QueueNextStage(this);
		});

		bw.RunWorkerAsync();
	}

	private ChunkMesh.MeshData MakeMesh(ChunkMesh.MeshData blockMesh)
	{
		return chunkMesh.MakeSurfaceAndMesh(blockMesh, blocks, surfaces);
	}
	#endregion

	#region Light Calc
	public void AsyncCalcLight()
	{
		BkgThreadCalcLight(this, System.EventArgs.Empty);
	}

	private void BkgThreadCalcLight(object sender, System.EventArgs e)
	{
		isProcessing = true;

		BackgroundWorker bw = new BackgroundWorker();

		// What to do in the background thread
		bw.DoWork += new DoWorkEventHandler(
		delegate (object o, DoWorkEventArgs args)
		{
			CalcLight();
		});

		// What to do when worker completes its task
		bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
		delegate (object o, RunWorkerCompletedEventArgs args)
		{
			isProcessing = false;

			genStage = GenStage.AmbientLight;
			World.Generator.QueueNextStage(this);
		});

		bw.RunWorkerAsync();
	}

	private int CalcLight()
	{
		LinkedList<LightSource> lights = World.GetLightsFor(this);
		if (lights == null)
			return 0;

		int counter = 0;

		// Set brightness
		foreach (LinkedList<BlockSurface> ls in surfaces)
		{
			if (ls == null)
				continue;

			foreach (BlockSurface surface in ls)
			{
				Vector3Int worldPos = surface.GetAdjBlockWorldCoord();

				foreach (LightSource light in lights)
				{
					counter++;

					// First pass. Reset lighting
					if (light == lights.First.Value)
					{
						surface.lastBrightness = surface.brightness;
						surface.brightness = 0;
						surface.lastColorTemp = surface.colorTemp;
						surface.colorTemp = 0.0f;
					}

					float dist = light.GetDistanceTo(worldPos);

					// However bright should this position be relative to the light, added and blended into existing lights
					float bright = light.GetBrightnessAt(this, surface, dist, worldPos.y < World.GetWaterHeight());

					//if (bright <= 0.01f)
					//	continue;

					// Apply shadows
					shadowBits.TryGetValue(light, out ChunkBitArray bits);

					// Calculate shadows
					if (bits == null)
					{
						shadowBits.Add(light, bits = new ChunkBitArray(World.GetChunkSize(), true));
					}

					// First time calculating for this block
					if (bits.needsCalc)
						bits.Set(!light.IsShadowed(worldPos), surface.block.localX, surface.block.localY, surface.block.localZ);

					// Get and apply shadows
					float shadowedMult = bits.Get(surface.block.localX, surface.block.localY, surface.block.localZ) ? 1 : 0;

					bright *= shadowedMult;

					surface.brightness = 1 - (1 - surface.brightness) * (1 - bright);

					// Like opacity for a Color layer
					float colorTempOpac = light.GetColorOpacityAt(this, surface, dist, worldPos.y < World.GetWaterHeight());

					colorTempOpac *= shadowedMult;

					surface.colorTemp += colorTempOpac * light.colorTemp;
				}

				ambientLight.Contribute(surface.normal, surface.brightness, surface.colorTemp);
			}
		}

		foreach (KeyValuePair<LightSource, ChunkBitArray> entry in shadowBits)
			entry.Value.needsCalc = false;

		return counter;
	}
	#endregion

	#region Ambient Light
	public void AsyncAmbientLight()
	{
		BkgThreadAmbientLight(this, System.EventArgs.Empty);
	}

	private void BkgThreadAmbientLight(object sender, System.EventArgs e)
	{
		isProcessing = true;

		BackgroundWorker bw = new BackgroundWorker();

		// What to do in the background thread
		bw.DoWork += new DoWorkEventHandler(
		delegate (object o, DoWorkEventArgs args)
		{
			AmbientLight();
		});

		// What to do when worker completes its task
		bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
		delegate (object o, RunWorkerCompletedEventArgs args)
		{
			isProcessing = false;

			genStage = GenStage.ApplyVertexColors;
			World.Generator.QueueNextStage(this);
		});

		bw.RunWorkerAsync();
	}

	private void AmbientLight()
	{
		Chunk startChunk;
		Vector3Int coord;

		// Ambient light retrieval
		foreach (LinkedList<BlockSurface> ls in surfaces)
		{
			if (ls == null)
				continue;

			foreach (BlockSurface surface in ls)
			{
				coord = surface.GetAdjBlockWorldCoord();

				startChunk = World.GetChunkFor(coord);

				if (startChunk == null)
					continue;
				LightingSample sample = startChunk.ambientLight.Retrieve(coord, surface.normal);

				surface.brightness = 1 - (1 - surface.brightness) * (1 - sample.brightness);
				surface.colorTemp += sample.colorTemp;
			}
		}
	}
	#endregion

	#region Light Visuals
	public void AsyncLightVisuals()
	{
		BkgThreadLightVisuals(this, System.EventArgs.Empty);
	}

	private void BkgThreadLightVisuals(object sender, System.EventArgs e)
	{
		isProcessing = true;

		BackgroundWorker bw = new BackgroundWorker();

		// What to do in the background thread
		bw.DoWork += new DoWorkEventHandler(
		delegate (object o, DoWorkEventArgs args)
		{
			args.Result = UpdateLightVisuals();
		});

		// What to do when worker completes its task
		bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
		delegate (object o, RunWorkerCompletedEventArgs args)
		{
			isProcessing = false;

			chunkMesh.ApplyVertexColors((Color[])args.Result);

			genStage = GenStage.Ready;
			World.Generator.QueueNextStage(this);
		});

		bw.RunWorkerAsync();
	}

	private Color[] UpdateLightVisuals()
	{
		// TODO: First surface of every chunk has broken vertex colors
		foreach (LinkedList<BlockSurface> ls in surfaces)
		{
			if (ls == null)
				continue;

			foreach (BlockSurface surf in ls)
			{
				chunkMesh.SetVertexColors(surf);
			}
		}

		return chunkMesh.GetVertexColors();
	}
	#endregion

	public void QueueLightUpdate()
	{
		if (isProcessing || genStage < GenStage.CalcLight)
			return;

		// TODO: Clear dictionaries if unused?

		//chunkMesh.ResetColors();

		genStage = GenStage.CalcLight;
		World.Generator.QueueNextStage(this, false);
	}

	public void NeedsLightDataRecalc(LightSource light)
	{
		if (isProcessing || genStage < GenStage.CalcLight)
			return;

		shadowBits.TryGetValue(light, out ChunkBitArray bits);
		if (bits != null)
			bits.needsCalc = false;
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

	public LinkedList<BlockSurface> GetSurfaces(int x, int y, int z)
	{
		return surfaces[x, y, z];
	}

	public AmbientLightNode GetAmbientLightNode()
	{
		return ambientLight;
	}
}
