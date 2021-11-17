using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh
{
	private static Color borderColor = new Color(0.01f, 0.01f, 0.5f, 0.5f);
	private static Color resetColor = new Color(0.02f, 0.02f, 0.5f, 0.5f);

	private Chunk chunk;

	private MeshFilter filter;

	// Save for later
	private Vector3[] sharedVertices;
	private Color[] vertexColors;

	private static readonly Vector3Int[] directions = new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0),
													new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)};

	private static readonly Vector3Int[] rotations = new Vector3Int[] { new Vector3Int(0, 90, 0), new Vector3Int(0, -90, 0), new Vector3Int(-90, 0, 0),
													new Vector3Int(90, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(0, 180, 0)};

	public void Init(Chunk chunk, MeshFilter filter)
	{
		this.chunk = chunk;

		this.filter = filter;

		// Duplicate original mesh to avoid permanent changes
		filter.sharedMesh = filter.mesh;
	}

	public Mesh GetSharedMesh()
	{
		return filter.sharedMesh;
	}

	public struct LightingSample
	{
		public float avgBrightness;
		public float avgColorTemp;

		public LightingSample(float avgBrightness, float avgColorTemp)
		{
			this.avgBrightness = avgBrightness;
			this.avgColorTemp = avgColorTemp;
		}
	}

	public void SetVertexColors(BlockSurface surface)
	{
		Vector3 meshPos;
		Vector3Int vertexPos = new Vector3Int();
		Vector3Int blockPos = new Vector3Int(surface.block.localX, surface.block.localY, surface.block.localZ);

		//float roundingOffset = 0.5f;

		// Loop through all vertices needed
		int loopCounter = 0;
		for (int i = surface.startIndex; i < surface.endIndex; i++)
		{
			loopCounter++;

			// Find actual block to sample for brightness
			meshPos = sharedVertices[i];
			vertexPos.x = Mathf.RoundToInt(meshPos.x + chunk.position.x);
			vertexPos.y = Mathf.RoundToInt(meshPos.y + chunk.position.y);
			vertexPos.z = Mathf.RoundToInt(meshPos.z + chunk.position.z);

			// Surfaces closest to this actual vertex
			//LightingSample ld = SampleLightingAt(vertexPos, new Vector3(roundingOffset + blockPos.x - meshPos.x, roundingOffset + blockPos.y - meshPos.y, roundingOffset + blockPos.z - meshPos.z));

			float lastBright = surface.brightness;

			float newBright = surface.brightness;

			float lastColorTemp = surface.colorTemp;

			float newColorTemp = surface.colorTemp;

			// Assign lighting data: new brightness, last brightness, new hue, last hue
			vertexColors[i] = new Color(lastBright, newBright, lastColorTemp, newColorTemp);

			//// Placeholder
			//colors[i] = new Color(RandomJitter(0.0f) + 0.2f, RandomJitter(0.0f) + 0.2f, RandomJitter(0.25f) + 0.5f, RandomJitter(0.25f) + 0.5f);

			//bool bright = World.GetBlockFor(chunk.position + blockPos).opacity <= 127;
			//colors[i] = new Color(bright ? 0.8f : 0.2f, bright ? 0.8f : 0.2f, bright ? 0.2f : 0.8f, bright ? 0.2f : 0.8f);
		}
	}

	//private LightingSample SampleLightingAt(Vector3Int vertPos, Vector3 offsets)
	//{
	//	Block adj;

	//	float count = 0;
	//	float avgBrightness = 0;
	//	float avgColorTemp = 0;

	//	Vector3Int adjPos = new Vector3Int();

	//	for (int x = -1; x <= 1; x += 2)
	//	{
	//		for (int y = -1; y <= 1; y += 2)
	//		{
	//			for (int z = -1; z <= 1; z += 2)
	//			{
	//				adjPos.x = Mathf.FloorToInt(vertPos.x + x * 0.5f);
	//				adjPos.y = Mathf.FloorToInt(vertPos.y + y * 0.5f);
	//				adjPos.z = Mathf.FloorToInt(vertPos.z + z * 0.5f);

	//				if (chunk.ContainsPos(adjPos.x - chunk.position.x, adjPos.y - chunk.position.y, adjPos.z - chunk.position.z))
	//					adj = chunk.GetBlock(adjPos.x - chunk.position.x, adjPos.y - chunk.position.y, adjPos.z - chunk.position.z);
	//				else
	//					adj = World.GetBlockFor(adjPos.x, adjPos.y, adjPos.z);

	//				if (adj.maybeNearAir == 0)
	//					continue;

	//				count++;
	//				avgBrightness += adj.brightness;
	//				avgColorTemp += adj.colorTemp;
	//			}
	//		}
	//	}

	//	return new LightingSample(avgBrightness / count, avgColorTemp / count);
	//}

	public Color[] GetVertexColors()
	{
		return vertexColors;
	}

	public void ApplyVertexColors(Color[] newColors)
	{
		vertexColors = newColors;

		// Can happen if thread finishes after games ends
		if (filter == null)
			return;

		// Apply vertex colors
		Mesh mesh = filter.sharedMesh;
		mesh.colors = vertexColors;
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

	public MeshData MakeSurfaceAndMesh(MeshData blockMeshData, Block[,,] blocks, LinkedList<BlockSurface>[,,] surfaces)
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

					// No model for this block
					if (block.IsAir())
					{
						// Air should not count as near air?
						block.maybeNearAir = 0;
						continue;
					}
					// 
					else if (block.maybeNearAir == 0)
						continue;

					int surfacesAdded = 0;
					for (int d = 0; d < directions.Length; d++)
					{
						faceOffset.x = chunk.position.x + x + directions[d].x;
						faceOffset.y = chunk.position.y + y + directions[d].y;
						faceOffset.z = chunk.position.z + z + directions[d].z;

						// Should a surface be made in this direction
						if (!World.GetBlockFor(faceOffset.x, faceOffset.y, faceOffset.z).IsAir())
							continue;
						surfacesAdded++;

						BlockSurface surface = new BlockSurface(directions[d], new Vector3(directions[d].x * 0.5f + 0.5f, directions[d].y * 0.5f + 0.5f, directions[d].z * 0.5f + 0.5f));

						int indexOffset = vertices.Count;
						// Remember which vertex index this surface starts at
						surface.startIndex = indexOffset;

						// Add vertices
						for (int i = 0; i < blockMeshData.vertices.Length; i++)
						{
							vert = Quaternion.Euler(rotations[d]) * (blockMeshData.vertices[i] + Vector3.forward * 0.5f);

							vertices.Add(new Vector3(vert.x + 0.5f + x, vert.y + 0.5f + y, vert.z + 0.5f + z));
						}

						// Remember which vertex index this surface starts at
						surface.endIndex = vertices.Count;

						surface.block = block;

						// Remember this surface
						if (surfaces[x, y, z] == null)
							surfaces[x, y, z] = new LinkedList<BlockSurface>();
						surfaces[x, y, z].AddLast(surface);

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
					// No surfaces created, not actually near air
					if (surfacesAdded == 0)
						block.maybeNearAir = 0;
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

		vertexColors = new Color[sharedVertices.Length];
		for (int i = 0; i < vertexColors.Length; i++)
			vertexColors[i] = borderColor;

		ApplyVertexColors(vertexColors);
	}

	public void ResetColors()
	{
		if (vertexColors == null)
			return;

		for (int i = 0; i < vertexColors.Length; i++)
			vertexColors[i] = resetColor;

		ApplyVertexColors(vertexColors);
	}
}
