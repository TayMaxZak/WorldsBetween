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
		MakeSurface,
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

	private HashSet<BlockSurface> surfaces;

	private Color[] lightCache;

	public ChunkMesh chunkMesh = new ChunkMesh();

	public ChunkGameObject go;


	// Represents where each light reaches. true = light, false = shadow
	private Dictionary<LightSource, ChunkBitArray> lightToMaskMap = new Dictionary<LightSource, ChunkBitArray>();

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

		surfaces = new HashSet<BlockSurface>();

		corners = new ChunkBitArray(chunkSize, false);

		lightCache = new Color[chunkSize * chunkSize * chunkSize];

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

			procStage = ProcStage.MakeSurface;
			World.Generator.QueueNextStage(this);
		});

		bw.RunWorkerAsync();
	}

	private void Generate()
	{
		List<NoiseModifier> modifiers = World.GetModifiers();

		for (int i = 0; i < modifiers.Count; i++)
			ApplyModifier(modifiers[i], i == 0, i == modifiers.Count - 1);
	}

	private void ApplyModifier(NoiseModifier modifier, bool firstPass, bool lastPass)
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

					if (lastPass && Mathf.Abs(y + position.y - 30) < 4)
						blocks[x, y, z].opacity = (byte)Mathf.Clamp(blocks[x, y, z].opacity - 8, 0, 255);
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
		return chunkMesh.MakeSurfaceAndMesh(blocks, surfaces, corners);
	}
	#endregion

	#region Light Calc
	public void AsyncCalcLight()
	{
		World.AddSunlight(this);
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
			CalcLightAllBlocks(true);
		});

		// What to do when worker completes its task
		bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
		delegate (object o, RunWorkerCompletedEventArgs args)
		{
			isProcessing = false;

			BakeLight(ProcStage.Done);

			//AsyncAmbientLight();
		});

		bw.RunWorkerAsync();
	}

	private void CalcLightAllBlocks(bool preAmbient)
	{
		for (int x = 0; x < chunkSize; x++)
		{
			for (int y = 0; y < chunkSize; y++)
			{
				for (int z = 0; z < chunkSize; z++)
				{
					Vector3Int localPos = new Vector3Int(x, y, z);
					LightingSample ls = SampleLightAt(preAmbient, localPos + position);

					lightCache[x * chunkSize * chunkSize + y * chunkSize + z] = new Color(ls.brightness, Mathf.Max(0, ls.colorTemp), Mathf.Max(0, -ls.colorTemp));

					if (preAmbient && !corners.Get(x, y, z))
						ambientLight.Contribute(ls.brightness, ls.colorTemp);
				}
			}
		}

		foreach (KeyValuePair<LightSource, ChunkBitArray> entry in lightToMaskMap)
			entry.Value.needsCalc = false;
	}

	public LightingSample SampleLightAt(bool preAmbient, Vector3Int worldPos)
	{
		LinkedList<LightSource> lights = World.GetLightsFor(this);
		if (lights == null)
			return new LightingSample(0.5f, -1f);

		float brightness = 0;
		float colorTemp = 0;

		Vector3Int localPos = worldPos - position;

		foreach (LightSource light in lights)
		{
			float dist = light.GetDistanceTo(worldPos);

			// However bright should this position be relative to the light, added and blended into existing lights
			float bright = light.GetBrightnessAt(this, worldPos, dist);

			// Apply shadows
			lightToMaskMap.TryGetValue(light, out ChunkBitArray maskBits);

			if (maskBits == null)
			{
				lightToMaskMap.Add(light, maskBits = new ChunkBitArray(World.GetChunkSize(), false));

				World.Lighter.CalcMaskFor(corners, maskBits);
			}

			// Get and apply shadows
			float maskMult = maskBits.Get(localPos.x, localPos.y, localPos.z) ? 1 : 0;

			bright *= maskMult;

			float oldBrightness = brightness;
			brightness += bright;

			colorTemp += light.colorTemp * maskMult;
		}

		if (!preAmbient)
		{
			Chunk startChunk = World.GetChunkFor(worldPos);

			if (startChunk != null)
			{
				LightingSample sample = startChunk.ambientLight.Retrieve(worldPos);

				if (sample.brightness > 0)
				{
					brightness += sample.brightness;
				}
			}
		}

		return new LightingSample(brightness, colorTemp);
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
			CalcLightAllBlocks(false);
		});

		// What to do when worker completes its task
		bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
		delegate (object o, RunWorkerCompletedEventArgs args)
		{
			isProcessing = false;

			BakeLight(ProcStage.Done);
		});

		bw.RunWorkerAsync();
	}
	#endregion

	#region Light Visuals
	private void BakeLight(ProcStage nextStage)
	{
		UpdateLightVisuals(lightStage < LightStage.AmbientLight);

		procStage = nextStage;
		World.Generator.QueueNextStage(this);
	}

	private void UpdateLightVisuals(bool preAmbient)
	{
		for (int x = 0; x < chunkSize; x++)
		{
			for (int y = 0; y < chunkSize; y++)
			{
				for (int z = 0; z < chunkSize; z++)
				{
					Vector3Int localPos = new Vector3Int(x, y, z);
					WorldLightAtlas.Instance.WriteToLightmap(WorldLightAtlas.LightMapSpace.WorldSpace, localPos + position, lightCache[x * chunkSize * chunkSize + y * chunkSize + z]);
				}
			}
		}
	}
	#endregion

	public void QueueLightUpdate()
	{
		if (isProcessing || procStage < ProcStage.MakeSurface)
			return;

		lightStage = LightStage.DirectLight;
	}

	public void NeedsLightDataRecalc(LightSource light)
	{
		if (isProcessing || procStage < ProcStage.MakeSurface)
			return;

		lightStage = LightStage.DirectLight;

		lightToMaskMap.TryGetValue(light, out ChunkBitArray bits);
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

	public HashSet<BlockSurface> GetSurfaces()
	{
		return surfaces;
	}

	public ChunkBitArray GetCorners()
	{
		return corners;
	}

	public AmbientLightNode GetAmbientLightNode()
	{
		return ambientLight;
	}
}
