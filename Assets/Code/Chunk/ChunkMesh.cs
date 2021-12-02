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

	public void SetVertexColors(BlockSurface surface, bool fakeBrightness)
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

			// Surfaces closest to this actual vertex
			LightingSample ls = SampleLightingAt(worldVertPos, surface);

			float sampleBlend = 1f;

			float lastBright = Mathf.Lerp(surface.brightness, ls.brightness, sampleBlend);

			if (fakeBrightness)
				lastBright = 1 - (1 - lastBright) * (1 - lastBright);

			float newBright = Mathf.Lerp(surface.brightness, ls.brightness, sampleBlend);

			if (fakeBrightness)
				newBright = 1 - (1 - newBright) * (1 - newBright);

			float lastColorTemp = (Mathf.Lerp(surface.colorTemp, ls.colorTemp, sampleBlend) + 1) / 2f;

			float newColorTemp = (Mathf.Lerp(surface.colorTemp, ls.colorTemp, sampleBlend) + 1) / 2f;

			// Assign lighting data: new brightness, last brightness, new hue, last hue
			vertexColors[i] = new Color(lastBright, newBright, lastColorTemp, newColorTemp);
		}
	}

	private LightingSample SampleLightingAt(Vector3Int worldVertPos, BlockSurface inputSurface)
	{
		LinkedList<BlockSurface> adjSurfaces;

		float count = 0;
		float avgBrightness = 0;
		float avgColorTemp = 0;

		Vector3Int adjBlockPos = new Vector3Int();

		for (int x = -1; x <= 1; x += 2)
		{
			for (int y = -1; y <= 1; y += 2)
			{
				for (int z = -1; z <= 1; z += 2)
				{
					adjBlockPos.x = Mathf.FloorToInt(worldVertPos.x + x * 0.5f);
					adjBlockPos.y = Mathf.FloorToInt(worldVertPos.y + y * 0.5f);
					adjBlockPos.z = Mathf.FloorToInt(worldVertPos.z + z * 0.5f);

					if (chunk.ContainsPos(adjBlockPos.x - chunk.position.x, adjBlockPos.y - chunk.position.y, adjBlockPos.z - chunk.position.z))
						adjSurfaces = chunk.GetSurfaces(adjBlockPos.x - chunk.position.x, adjBlockPos.y - chunk.position.y, adjBlockPos.z - chunk.position.z);
					else
						adjSurfaces = World.GetSurfacesFor(adjBlockPos.x, adjBlockPos.y, adjBlockPos.z);

					if (adjSurfaces == null)
						continue;

					float tolerance = 0.001f;

					foreach (BlockSurface adjSurface in adjSurfaces)
					{
						// Dot product of each surface normal
						float normalDot = Vector3.Dot(inputSurface.normal, adjSurface.normal);
						// Dot product of input surface normal and offset direction between both surfaces
						float polarityDot = Vector3.Dot(inputSurface.normal, (adjSurface.GetWorldPosition() - worldVertPos));
						// Dot product of adjacent surface's relative offset and input surface's offset relative to the adjacent surface's block
						float distanceDot = Vector3.Dot(adjSurface.relativeOffset, worldVertPos - adjSurface.GetBlockWorldPosition());

						// Test if same normal
						bool edge = normalDot < 1 - tolerance;
						bool flat = !edge;

						bool crease = edge && polarityDot > tolerance;

						bool shadow = false, bounce = false;

						// Test shadow conditions
						if (adjSurface.brightness < inputSurface.brightness - tolerance)
						{
							shadow = crease;
						}
						// Test bounce conditions
						else if (adjSurface.brightness > inputSurface.brightness + tolerance)
						{
							bounce = !crease;

							//if (distanceDot < tolerance)
							//	bounce = false;
						}

						// Flat surface
						if (!shadow && !bounce)
						{
							if (!flat || distanceDot < -tolerance)
								continue;
						}

						count++;
						avgBrightness += adjSurface.brightness;
						avgColorTemp += adjSurface.colorTemp;
					}
				}
			}
		}

		return new LightingSample(avgBrightness / count, avgColorTemp / count);
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

	public MeshData MakeSurfaceAndMesh(Block[,,] blocks, LinkedList<BlockSurface>[,,] surfaces)
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
						MeshData blockMeshData = ModelsList.GetModelFor(0).faces[d].meshData;

						faceOffset.x = chunk.position.x + x + directions[d].x;
						faceOffset.y = chunk.position.y + y + directions[d].y;
						faceOffset.z = chunk.position.z + z + directions[d].z;

						// Should a surface be made in this direction
						if (!World.GetBlockFor(faceOffset.x, faceOffset.y, faceOffset.z).IsAir())
							continue;
						surfacesAdded++;

						//Vector3 randomNormal = new Vector3(SeedlessRandom.NextFloatInRange(-1, 1), SeedlessRandom.NextFloatInRange(-1, 1), SeedlessRandom.NextFloatInRange(-1, 1));
						BlockSurface surface = new BlockSurface(chunk, block, directions[d], new Vector3(directions[d].x * 0.5f, directions[d].y * 0.5f, directions[d].z * 0.5f));

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

						// Remember this surface
						if (surfaces[x, y, z] == null)
							surfaces[x, y, z] = new LinkedList<BlockSurface>();
						surfaces[x, y, z].AddLast(surface);

						if (surfaces[x, y, z].Count > 6)
							Debug.LogError("Too many surfaces on one block");

						// Add normals
						for (int i = 0; i < blockMeshData.normals.Length; i++)
						{
							normals.Add(Quaternion.Euler(rotations[d]) * blockMeshData.normals[i]);
						}

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

		return new MeshData(vertices.ToArray(), normals.ToArray(), uv.ToArray(), new Dictionary<int, int[]> { { 0, triangles.ToArray() } });
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
