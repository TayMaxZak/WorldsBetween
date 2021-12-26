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

	private static readonly Vector3Int[] grassRotations = new Vector3Int[] { new Vector3Int(0, 45, 0), new Vector3Int(0, -45, 0) };

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

	// TODO: Repurpose for textures
	public void SetVertexColors(BlockSurface surface)
	{
		Vector3 localVertPos;
		Vector3Int worldVertPos = new Vector3Int();

		// Loop through all vertices needed
		int loopCounter = 0;
		for (int i = surface.startIndex; i < surface.endIndex; i++)
		{
			loopCounter++;

			// Threading
			if (sharedVertices == null)
				return;

			// Find actual block to sample for brightness
			localVertPos = sharedVertices[i];
			worldVertPos.x = Mathf.RoundToInt(localVertPos.x + chunk.position.x);
			worldVertPos.y = Mathf.RoundToInt(localVertPos.y + chunk.position.y);
			worldVertPos.z = Mathf.RoundToInt(localVertPos.z + chunk.position.z);

			// Assign lighting data: new brightness, last brightness, new hue, last hue
			vertexColors[i] = new Color(0, 0, 0, 0);
		}
	}

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
		public Vector2[] uv;

		public Dictionary<int, int[]> triangles;

		public MeshData(Mesh mesh)
		{
			vertices = mesh.vertices;
			normals = mesh.normals;
			uv = mesh.uv;

			triangles = new Dictionary<int, int[]>();
			triangles[0] = mesh.triangles;
		}

		public MeshData(Vector3[] vertices, Vector3[] normals, Vector2[] uv, Dictionary<int, int[]> triangles)
		{
			this.vertices = vertices;
			this.normals = normals;
			this.uv = uv;

			this.triangles = triangles;
		}
	}

	public MeshData MakeSurfaceAndMesh(Block[,,] blocks, LinkedList<BlockSurface>[,,] surfaces, ChunkBitArray corners)
	{
		Block block;

		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		List<int> vegTriangles = new List<int>();
		List<Vector3> normals = new List<Vector3>();
		List<Vector2> uv = new List<Vector2>();

		Vector3Int faceOffset = new Vector3Int();

		Vector3 vert;

		Vector3 norm;

		List<int> airDirections = new List<int>();

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
					// Solid
					else
					{
						FlagAllCorners(corners, x, y, z);

						// Not near air
						if (block.maybeNearAir == 0)
							continue;
					}

					// Useful to predict which directions will require a surface
					airDirections.Clear();

					for (int d = 0; d < directions.Length; d++)
					{
						faceOffset.x = chunk.position.x + x + directions[d].x;
						faceOffset.y = chunk.position.y + y + directions[d].y;
						faceOffset.z = chunk.position.z + z + directions[d].z;

						if (!World.GetBlockFor(faceOffset.x, faceOffset.y, faceOffset.z).IsAir())
							continue;
						airDirections.Add(d);
					}

					// No air in any direction
					if (airDirections.Count == 0)
						continue;

					int surfacesAdded = 0;
					for (int d = 0; d < directions.Length; d++)
					{
						MeshData blockMeshData = ModelsList.GetModelFor(0).faces[d].meshData;

						faceOffset.x = chunk.position.x + x + directions[d].x;
						faceOffset.y = chunk.position.y + y + directions[d].y;
						faceOffset.z = chunk.position.z + z + directions[d].z;

						// Should a surface be made in this direction
						if (!airDirections.Contains(d))
							continue;
						surfacesAdded++;

						//Vector3 randomNormal = new Vector3(SeedlessRandom.NextFloatInRange(-1, 1), SeedlessRandom.NextFloatInRange(-1, 1), SeedlessRandom.NextFloatInRange(-1, 1));
						BlockSurface surface = new BlockSurface(chunk, block, directions[d], new Vector3(directions[d].x * 0.5f, directions[d].y * 0.5f, directions[d].z * 0.5f));

						// Remember this surface
						if (surfaces[x, y, z] == null)
							surfaces[x, y, z] = new LinkedList<BlockSurface>();
						surfaces[x, y, z].AddLast(surface);

						if (surfaces[x, y, z].Count > 6)
							Debug.LogError("Too many surfaces on one block");

						int indexOffset = vertices.Count;
						// Remember which vertex index this surface starts at
						surface.startIndex = indexOffset;

						// Add vertices
						for (int i = 0; i < blockMeshData.vertices.Length; i++)
						{
							vert = Quaternion.Euler(rotations[d]) * (blockMeshData.vertices[i] + Vector3.forward * 0.5f);

							vertices.Add(new Vector3(vert.x + 0.5f + x, vert.y + 0.5f + y, vert.z + 0.5f + z));

							// Add normals
							norm = Vector3.zero;
							int smoothingDirs = 0;
							for (int j = 0; j < airDirections.Count; j++)
							{
								if (Vector3.Dot(vert.normalized, directions[airDirections[j]]) < -0.01)
									continue;

								smoothingDirs++;
								norm += directions[airDirections[j]];
							}

							normals.Add(Vector3.Lerp(directions[d], norm, 0.67f).normalized);
						}

						// Remember which vertex index this surface ends at
						surface.endIndex = vertices.Count;

						// Add triangles
						for (int i = 0; i < (blockMeshData.triangles[0]).Length; i++)
						{
							triangles.Add((blockMeshData.triangles[0])[i] + indexOffset);
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

		return new MeshData(vertices.ToArray(), normals.ToArray(), uv.ToArray(), new Dictionary<int, int[]> { { 0, triangles.ToArray() }, { 1, vegTriangles.ToArray() } });
	}

	// TODO: Handle chunk borders
	private void FlagAllCorners(ChunkBitArray corners, int x, int y, int z)
	{
		//Block block;

		int chunkSize = World.GetChunkSize();

		bool doX = false, doY = false, doZ = false;

		// X-axis
		if (x < chunkSize - 1)
			doX = true;

		// Y-axis
		if (y < chunkSize - 1)
			doY = true;

		// Z-axis
		if (z < chunkSize - 1)
			doZ = true;

		// Apply
		corners.Set(true, x, y, z);

		if (doX)
			corners.Set(true, x + 1, y, z);
		if (doY)
			corners.Set(true, x, y + 1, z);
		if (doZ)
			corners.Set(true, x, y, z + 1);

		if (doX && doY)
			corners.Set(true, x + 1, y + 1, z);
		if (doY && doZ)
			corners.Set(true, x, y + 1, z + 1);
		if (doZ && doX)
			corners.Set(true, x + 1, y, z + 1);

		if (doX && doY && doZ)
			corners.Set(true, x + 1, y + 1, z + 1);
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
