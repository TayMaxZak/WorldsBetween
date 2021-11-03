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

	public bool processing = false;

	private int chunkSize = 8;

	private Block[,,] blocks;

	public ChunkMesh chunkMesh = new ChunkMesh();

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
		processing = true;

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
			processing = false;

			CacheNearAir();

			genStage = GenStage.Generated;
			World.QueueNextStage(this);
		});

		bw.RunWorkerAsync();
	}

	private void Generate()
	{
		List<Modifier> modifiers = World.GetModifiers();

		for (int i = 0; i < modifiers.Count; i++)
			ApplyModifier(modifiers[i], i == 0, i == modifiers.Count - 1);
	}

	private void ApplyModifier(Modifier modifier, bool firstPass, bool lastPass)
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
		processing = true;

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
			processing = false;

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
			World.QueueNextStage(this);
		});

		bw.RunWorkerAsync();
	}

	private ChunkMesh.MeshData MakeMesh(ChunkMesh.MeshData blockMesh)
	{
		return chunkMesh.GenerateMesh(blockMesh, blocks);
	}
	#endregion

	#region Light
	public void AsyncCalcLight()
	{
		BkgThreadCalcLight(this, System.EventArgs.Empty);
	}

	private void BkgThreadCalcLight(object sender, System.EventArgs e)
	{
		processing = true;

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
			processing = false;

			genStage = GenStage.Lit;
			World.QueueNextStage(this);
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

				Vector3Int pos = new Vector3Int(position.x + block.localX, position.y + block.localY, position.z + block.localZ);

				// However bright should this position be relative to the light, added and blended into existing lights
				float bright = light.GetBrightnessAt(pos, pos.y < World.GetWaterHeight());
				newBrightness = 1 - (1 - newBrightness) * (1 - bright);

				// Like opacity for a color layer
				float colorTempOpac = bright;
				float colorTemp = light.GetColorTemperatureAt(colorTempOpac, pos.y < World.GetWaterHeight());
				newColorTemp += colorTempOpac * colorTemp;
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

		return counter;
	}

	public void UpdateLightVisuals()
	{
		int counter = 0;

		foreach (Block block in blocks)
		{
			counter++;

			chunkMesh.SetVertexColors(block);
		}

		if (counter > 0)
			chunkMesh.ApplyVertexColors();
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
}
