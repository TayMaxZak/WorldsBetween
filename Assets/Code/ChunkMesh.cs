using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh
{
	private static Color borderColor = new Color(0.01f, 0.01f, 0.5f, 0.5f);
	private static Color resetColor = new Color(0.02f, 0.02f, 0.5f, 0.5f);

	private Chunk chunk;

	private MeshFilter filter;

	public Mesh blockMesh;

	// Save for later
	private Vector3[] sharedVertices;
	private Color[] colors;

	private static readonly Vector3Int[] directions = new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0),
													new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)};

	private static readonly Vector3Int[] rotations = new Vector3Int[] { new Vector3Int(0, 90, 0), new Vector3Int(0, -90, 0), new Vector3Int(-90, 0, 0),
													new Vector3Int(90, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(0, 180, 0)};

	public void Init(Chunk chunk, MeshFilter filter, Mesh blockMesh)
	{
		this.chunk = chunk;

		this.filter = filter;

		// Duplicate original mesh to avoid permanent changes
		filter.sharedMesh = filter.mesh;

		this.blockMesh = blockMesh;
	}

	public Mesh GetSharedMesh()
	{
		return filter.sharedMesh;
	}

	public struct LightingData
	{
		public float avgBrightness;
		public float avgColorTemp;

		public LightingData(float avgBrightness, float avgColorTemp)
		{
			this.avgBrightness = avgBrightness;
			this.avgColorTemp = avgColorTemp;
		}
	}

	public void SetVertexColors(Block block)
	{
		Vector3 meshPos;
		Vector3Int vertexPos = new Vector3Int();

		// Loop through all vertices needed
		int loopCounter = 0;
		for (int i = block.startIndex; i < block.endIndex; i++)
		{
			loopCounter++;

			// Find actual block to sample for brightness
			meshPos = sharedVertices[i];
			vertexPos.x = Mathf.RoundToInt(meshPos.x + chunk.position.x);
			vertexPos.y = Mathf.RoundToInt(meshPos.y + chunk.position.y);
			vertexPos.z = Mathf.RoundToInt(meshPos.z + chunk.position.z);

			// Block that's closest to this actual vertex
			LightingData ld = GetLightingDataAt(vertexPos);

			// Convert brightness value to float
			float lastBright = ld.avgBrightness / 255f;
			//if (adj.postUpdate > 0)
			//	lastBright = adj.brightness / 255f;

			float newBright = ld.avgBrightness / 255f;

			// Convert hue value to float
			float lastColorTemp = ld.avgColorTemp / 255f;
			//if (adj.postUpdate > 0)
			//	lastHue = adj.colorTemp / 255f;

			float newColorTemp = ld.avgColorTemp / 255f;

			// Assign lighting data: new brightness, last brightness, new hue, last hue
			colors[i] = new Color(lastBright, newBright, lastColorTemp, newColorTemp);

			//// Placeholder
			//colors[i] = new Color(RandomJitter(0.0f) + 0.2f, RandomJitter(0.0f) + 0.2f, RandomJitter(0.25f) + 0.5f, RandomJitter(0.25f) + 0.5f);

			//bool bright = World.GetBlockFor(chunk.position + blockPos).opacity <= 127;
			//colors[i] = new Color(bright ? 0.8f : 0.2f, bright ? 0.8f : 0.2f, bright ? 0.2f : 0.8f, bright ? 0.2f : 0.8f);
		}

		//if (filter.sharedMesh.colors.Length == 0)
		//	ApplyVertexColors();
	}

	private LightingData GetLightingDataAt(Vector3Int pos)
	{
		Block adj;

		float count = 0;
		float avgBrightness = 0;
		float avgColorTemp = 0;

		float offset = 0.5f;

		Vector3Int debugPosV = new Vector3Int(pos.x, pos.y, pos.z);
		Vector3 debugPosB = new Vector3(pos.x + offset, pos.y + offset, pos.z + offset);

		pos.x = (int)(pos.x + offset);
		pos.y = (int)(pos.y + offset);
		pos.z = (int)(pos.z + offset);

		for (int x = -1; x < 1; x++)
		{
			for (int y = -1; y < 1; y++)
			{
				for (int z = -1; z < 1; z++)
				{
					if (chunk.ContainsPos(pos.x + x, pos.y + y, pos.z + z))
						adj = chunk.GetBlock(pos.x + x - chunk.position.x, pos.y + y - chunk.position.y, pos.z + z - chunk.position.z);
					else
						adj = World.GetBlockFor(pos.x + x, pos.y + y, pos.z + z);

					if (adj.nearAir == 0)
						continue;

					count++;
					avgBrightness += adj.brightness;
					avgColorTemp += adj.colorTemp;
				}
			}
		}

		// Try again?
		if (count == 0)
		{
			pos.x = (int)(pos.x - 1);
			pos.y = (int)(pos.y - 1);
			pos.z = (int)(pos.z - 1);

			for (int x = -1; x < 1; x++)
			{
				for (int y = -1; y < 1; y++)
				{
					for (int z = -1; z < 1; z++)
					{
						if (chunk.ContainsPos(pos.x + x, pos.y + y, pos.z + z))
							adj = chunk.GetBlock(pos.x + x - chunk.position.x, pos.y + y - chunk.position.y, pos.z + z - chunk.position.z);
						else
							adj = World.GetBlockFor(pos.x + x, pos.y + y, pos.z + z);

						if (adj.nearAir == 0)
							continue;

						count++;
						avgBrightness += adj.brightness;
						avgColorTemp += adj.colorTemp;
					}
				}
			}
		}

		// Prevent division by zero
		if (count == 0)
			count++;

		return new LightingData(avgBrightness / count, avgColorTemp / count);
	}

	public void ApplyVertexColors()
	{
		// Can happen if thread finishes after games ends
		if (filter == null)
			return;

		// Apply vertex colors
		Mesh mesh = filter.sharedMesh;
		mesh.colors = colors;
	}

	public struct MeshData
	{
		public Vector3[] vertices;
		public Vector3[] normals;
		public int[] triangles;
		public Vector2[] uv;

		public MeshData(Mesh mesh)
		{
			vertices = mesh.vertices;
			normals = mesh.normals;
			triangles = mesh.triangles;
			uv = mesh.uv;
		}

		public MeshData(Vector3[] vertices, Vector3[] normals, int[] triangles, Vector2[] uv)
		{
			this.vertices = vertices;
			this.normals = normals;
			this.triangles = triangles;
			this.uv = uv;
		}
	}

	public MeshData GenerateMesh(MeshData blockMeshData, Block[,,] blocks)
	{
		Block block;

		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		List<Vector3> normals = new List<Vector3>();
		List<Vector2> uv = new List<Vector2>();

		Vector3Int faceOffset = new Vector3Int();

		Vector3 vert;

		int chunkSize = World.GetChunkSize();
		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					block = blocks[x, y, z];

					// Empty block
					// TODO: Change
					if (block.opacity <= 127 || block.nearAir == 0)
						continue;

					// Remember which vertex index this block starts at
					block.startIndex = vertices.Count;

					for (int d = 0; d < directions.Length; d++)
					{
						faceOffset.x = chunk.position.x + x + directions[d].x;
						faceOffset.y = chunk.position.y + y + directions[d].y;
						faceOffset.z = chunk.position.z + z + directions[d].z;

						if (World.GetBlockFor(faceOffset.x, faceOffset.y, faceOffset.z).opacity > 127)
							continue;

						int indexOffset = vertices.Count;

						// Add vertices
						for (int i = 0; i < blockMeshData.vertices.Length; i++)
						{
							vert = Quaternion.Euler(rotations[d]) * (blockMeshData.vertices[i] + Vector3.forward * 0.5f);

							vertices.Add(new Vector3(vert.x + 0.5f + x, vert.y + 0.5f + y, vert.z + 0.5f + z));
						}

						block.endIndex = vertices.Count;

						// Add normals
						for (int i = 0; i < blockMeshData.normals.Length; i++)
						{
							normals.Add(Quaternion.Euler(rotations[d]) * blockMeshData.normals[i]);
						}

						// Add triangles
						for (int i = 0; i < blockMeshData.triangles.Length; i++)
						{
							triangles.Add(blockMeshData.triangles[i] + indexOffset);
						}

						// Add UVs
						for (int i = 0; i < blockMeshData.uv.Length; i++)
						{
							uv.Add(blockMeshData.uv[i]);
						}
					}
				}
			}
		}

		return new MeshData(vertices.ToArray(), normals.ToArray(), triangles.ToArray(), uv.ToArray());
	}

	public void FinishMesh(Mesh newMesh)
	{
		// Can happen if thread finishes after games ends
		if (filter == null)
			return;

		filter.mesh = newMesh;

		sharedVertices = filter.sharedMesh.vertices;

		colors = new Color[sharedVertices.Length];
		for (int i = 0; i < colors.Length; i++)
			colors[i] = borderColor;

		ApplyVertexColors();
	}

	public void ResetColors()
	{
		if (colors == null)
			return;

		for (int i = 0; i < colors.Length; i++)
			colors[i] = resetColor;

		ApplyVertexColors();
	}
}
