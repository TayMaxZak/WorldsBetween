using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;

public class Chunk
{
	public enum GenStage
	{
		Empty,
		Allocated,
		Generated,
		Meshed,
		Lit,
		Ready
	}
	public GenStage genStage = GenStage.Empty;


	public Vector3Int position; // Coordinates of chunk


	public bool atEdge = false;

	public bool isProcessing = false;

	private int chunkSize = 8;

	private Block[,,] blocks;

	public ChunkMesh chunkMesh = new ChunkMesh();


	// Represents where each light reaches. true = light, false = shadow
	protected Dictionary<LightSource, ChunkBitArray> shadowBits = new Dictionary<LightSource, ChunkBitArray>();

	public void Init(int chunkSize)
	{
		this.chunkSize = chunkSize;

		// Create blocks
		CreateBlocks();
	}

	public void SetPos(Vector3Int pos)
	{
		position = pos;
	}

	public void CreateBlocks()
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

			CacheNearAir();

			genStage = GenStage.Generated;
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
				}
			}
		}
	}

	public void CacheNearAir()
	{
		Block block;

		int cutoff = 127;

		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					// Remember if this block is bordering air
					if (blocks[x, y, z].opacity > cutoff)
					{
						continue;
					}

					blocks[x, y, z].nearAir = 255;

					// Assign adjacent blocks in this chunk
					if (x > 0)
						blocks[x - 1, y, z].nearAir = 255;
					else if ((block = World.GetBlockFor(position.x + x - 1, position.y + y, position.z + z)) != Block.empty)
						block.nearAir = 255;

					if (x < chunkSize - 1)
						blocks[x + 1, y, z].nearAir = 255;
					else if ((block = World.GetBlockFor(position.x + x + 1, position.y + y, position.z + z)) != Block.empty)
						block.nearAir = 255;

					if (y > 0)
						blocks[x, y - 1, z].nearAir = 255;
					else if ((block = World.GetBlockFor(position.x + x, position.y + y - 1, position.z + z)) != Block.empty)
						block.nearAir = 255;

					if (y < chunkSize - 1)
						blocks[x, y + 1, z].nearAir = 255;
					else if ((block = World.GetBlockFor(position.x + x, position.y + y + 1, position.z + z)) != Block.empty)
						block.nearAir = 255;

					if (z > 0)
						blocks[x, y, z - 1].nearAir = 255;
					else if ((block = World.GetBlockFor(position.x + x, position.y + y, position.z + z - 1)) != Block.empty)
						block.nearAir = 255;

					if (z < chunkSize - 1)
						blocks[x, y, z + 1].nearAir = 255;
					else if ((block = World.GetBlockFor(position.x + x, position.y + y, position.z + z + 1)) != Block.empty)
						block.nearAir = 255;
				}
			}
		}
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

		ChunkMesh.MeshData blockMesh = new ChunkMesh.MeshData(chunkMesh.blockMesh);

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

			genStage = GenStage.Meshed;
			World.Generator.QueueNextStage(this);
		});

		bw.RunWorkerAsync();
	}

	private ChunkMesh.MeshData MakeMesh(ChunkMesh.MeshData blockMesh)
	{
		return chunkMesh.GenerateMesh(blockMesh, blocks);
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

			genStage = GenStage.Lit;
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
		foreach (Block block in blocks)
		{
			if (block.nearAir == 0)
				continue;

			// Use floats to preserve precision
			float newBrightness = 0;
			float newColorTemp = 0;

			bool changed = false;
			foreach (LightSource light in lights)
			{
				counter++;

				// First pass. Reset lighting
				if (light == lights.First.Value)
				{
					changed = true;

					block.lastBrightness = block.brightness;
					block.brightness = 0;
					block.lastColorTemp = block.colorTemp;
					block.colorTemp = 127;

					newBrightness = (block.brightness) / 255f;
					newColorTemp = -1 + (2 * block.colorTemp) / 255f;
				}

				Vector3Int worldPos = new Vector3Int(position.x + block.localX, position.y + block.localY, position.z + block.localZ);

				float dist = light.GetDistanceTo(worldPos);

				// However bright should this position be relative to the light, added and blended into existing lights
				float bright = light.GetBrightnessAt(this, dist, worldPos.y < World.GetWaterHeight());

				if (bright <= 1 / 255f)
					continue;

				float atten = light.GetShadowBrightnessAt(this, dist, worldPos.y < World.GetWaterHeight());

				// Apply shadows
				shadowBits.TryGetValue(light, out ChunkBitArray bits);

				// Calculate shadows
				if (bits == null)
				{
					shadowBits.Add(light, bits = new ChunkBitArray(World.GetChunkSize(), true));
				}

				// First time calculating for this block
				if (bits.needsCalc)
					bits.Set(!light.IsShadowed(worldPos), block.localX, block.localY, block.localZ);

				// Get shadows
				float mult = bits.Get(block.localX, block.localY, block.localZ) ? 1 : 0;
				mult = Mathf.Clamp01(mult + atten);

				// Apply shadows & final falloff. Shadows are less intense near the source
				bright *= mult;

				newBrightness = 1 - (1 - newBrightness) * (1 - bright);

				// Like opacity for a Color layer
				float colorTempOpac = light.GetColorOpacityAt(this, dist, worldPos.y < World.GetWaterHeight());
				colorTempOpac *= mult;
				newColorTemp += colorTempOpac * light.colorTemp;
			}

			if (changed)
			{
				block.brightness = (byte)(newBrightness * 255f);
				float a = Mathf.Clamp(newColorTemp, -1, 1);
				float b = a + 1;
				float c = b / 2;
				block.colorTemp = (byte)(255f * c);
			}
		}

		foreach (KeyValuePair<LightSource, ChunkBitArray> entry in shadowBits)
			entry.Value.needsCalc = false;


		return counter;
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
		foreach (Block block in blocks)
		{
			// Only update necessary blocks
			if (block.nearAir > 0)
				chunkMesh.SetVertexColors(block);
		}

		return chunkMesh.GetVertexColors();
	}
	#endregion

	public void QueueLightUpdate()
	{
		if (isProcessing || genStage < GenStage.Meshed)
			return;

		// TODO: Clear dictionaries if unused?

		//chunkMesh.ResetColors();

		genStage = GenStage.Meshed;
		World.Generator.QueueNextStage(this, false);
	}

	public void NeedsLightDataRecalc(LightSource light)
	{
		if (isProcessing || genStage < GenStage.Meshed)
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
}
