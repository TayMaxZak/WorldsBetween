using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh
{
	private static Color borderColor = new Color(0.01f, 0.01f, 0.5f, 0.5f);

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

	//// TODO: Repurpose for textures
	//public void SetVertexColors(BlockSurface surface)
	//{
	//	Vector3 localVertPos;
	//	Vector3Int worldVertPos = new Vector3Int();

	//	// Loop through all vertices needed
	//	int loopCounter = 0;
	//	for (int i = surface.startIndex; i < surface.endIndex; i++)
	//	{
	//		loopCounter++;

	//		// Threading
	//		if (sharedVertices == null)
	//			return;

	//		// Find actual block to sample for brightness
	//		localVertPos = sharedVertices[i];
	//		worldVertPos.x = Mathf.RoundToInt(localVertPos.x + chunk.position.x);
	//		worldVertPos.y = Mathf.RoundToInt(localVertPos.y + chunk.position.y);
	//		worldVertPos.z = Mathf.RoundToInt(localVertPos.z + chunk.position.z);

	//		// Assign lighting data: new brightness, last brightness, new hue, last hue
	//		vertexColors[i] = new Color(0, 0, 0, 0);
	//	}
	//}

	//public Color[] GetVertexColors()
	//{
	//	return vertexColors;
	//}

	//public void ApplyVertexColors(Color[] newColors)
	//{
	//	vertexColors = newColors;

	//	// Can happen if thread finishes after games ends
	//	if (filter == null)
	//		return;

	//	// Apply vertex colors
	//	Mesh mesh = filter.sharedMesh;
	//	mesh.colors = vertexColors;
	//}

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

	public MeshData MakeMesh()
	{
		Block block;

		List<Vector3> vertices = new List<Vector3>();

		List<int> triangles = new List<int>();
		List<int> vegTriangles = new List<int>();

		List<Vector3> normals = new List<Vector3>();

		List<Vector2> uv = new List<Vector2>();

		List<int> airDirections = new List<int>();

		int chunkSize = chunk.chunkSizeWorld;
		for (int x = 0; x < chunkSize; x += chunk.scaleFactor)
		{
			for (int y = 0; y < chunkSize; y += chunk.scaleFactor)
			{
				for (int z = 0; z < chunkSize; z += chunk.scaleFactor)
				{
					Vector3Int faceOffset = new Vector3Int();
					Vector3 vert;
					Vector3 norm;

					block = chunk.GetBlock(x, y, z);

					// No model for this block
					if (!block.IsFilled())
					{
						// Air should not count as near air?
						block.SetNeedsMesh(false);
						continue;
					}
					// Solid
					else
					{
						// Not near air
						if (!block.GetNeedsMesh())
							continue;
					}

					// Useful to predict which directions will require a surface
					airDirections.Clear();

					for (int d = 0; d < directions.Length; d++)
					{
						faceOffset = new Vector3Int(
							chunk.position.x + x + directions[d].x * chunk.scaleFactor,
							chunk.position.y + y + directions[d].y * chunk.scaleFactor,
							chunk.position.z + z + directions[d].z * chunk.scaleFactor
						);

						if (chunk.chunkType == Chunk.ChunkType.Close && !World.Contains(faceOffset + Vector3.one / 2))
							continue;

						Block adj = World.GetBlock(faceOffset.x, faceOffset.y, faceOffset.z);

						if (!adj.IsOpaque() || (adj.GetMeshSmoothing() != block.GetMeshSmoothing()))
						{
							airDirections.Add(d);
						}
					}

					// No air in any direction
					if (airDirections.Count == 0)
						continue;

					BlockModel model = ModelsList.GetModelFor(block.GetBlockType() - 1);

					if (model.blockModelType == BlockModel.BlockModelType.SixFaces)
					{
						for (int d = 0; d < directions.Length; d++)
						{
							faceOffset.x = chunk.position.x + x + directions[d].x * chunk.scaleFactor;
							faceOffset.y = chunk.position.y + y + directions[d].y * chunk.scaleFactor;
							faceOffset.z = chunk.position.z + z + directions[d].z * chunk.scaleFactor;

							// Only render easily viewable faces for non-near chunks
							if (chunk.chunkType != Chunk.ChunkType.Close)
							{
								Vector3 checkDir = new Vector3(Mathf.RoundToInt(faceOffset.x), Mathf.RoundToInt(faceOffset.y), Mathf.RoundToInt(faceOffset.z)).normalized;

								if (Vector3.Dot(directions[d], checkDir) > 0.5f)
									continue;
							}

							MeshData blockMeshData = model.faces[d].meshData;

							// Should a surface be made in this direction
							if (!airDirections.Contains(d))
								continue;

							// Show debug rays in some places
							bool drawDebugRay = false/*SeedlessRandom.NextFloat() > 0.9999f*/;

							// What index are we starting this face from
							int indexOffset = vertices.Count;

							// Add vertices
							for (int v = 0; v < blockMeshData.vertices.Length; v++)
							{
								Vector3 middle = new Vector3(0.5f * chunk.scaleFactor + x, 0.5f * chunk.scaleFactor + y, 0.5f * chunk.scaleFactor + z);

								vert = Quaternion.Euler(rotations[d]) * (blockMeshData.vertices[v] + Vector3.forward * 0.5f);
								vert *= chunk.scaleFactor;

								Vector3 vertPos = middle + vert;

								// Calculate normals
								norm = Vector3.zero;

								// Based on adjacent blocks (never fails, angular result)
								for (int i = -1; i <= 1; i += 2)
								{
									for (int j = -1; j <= 1; j += 2)
									{
										for (int k = -1; k <= 1; k += 2)
										{
											if (!World.GetBlock(Mathf.FloorToInt(chunk.position.x + vertPos.x + i * 0.5f * chunk.scaleFactor), Mathf.FloorToInt(chunk.position.y + vertPos.y + j * 0.5f * chunk.scaleFactor), Mathf.FloorToInt(chunk.position.z + vertPos.z + k * 0.5f)).IsOpaque())
											{
												norm += new Vector3Int(i, j, k);

												if (drawDebugRay)
													Debug.DrawRay(vertPos + chunk.position, new Vector3(i, j, k) * 0.5f, Color.blue, 200);
											}
											else if (drawDebugRay)
												Debug.DrawRay(vertPos + chunk.position, new Vector3(i, j, k) * 0.5f, Color.red, 200);
										}
									}
								}

								// Stuck between corners, use hard normal always
								if (norm == Vector3.zero)
								{
									normals.Add(block.GetNormalRefractive() * (Vector3)directions[d]);
								}
								// Normal case
								else
								{
									// Smooth vs hard determined by block material
									float hardness = block.GetNormalHardness();

									normals.Add(block.GetNormalRefractive() * Vector3.Lerp(norm.normalized, directions[d], hardness).normalized);
								}


								float dot = block.GetMeshSmoothing();
								Vector3 displacedVert = vertPos + dot * (norm / 2 - norm.normalized * 2) / 2;
								vertices.Add(displacedVert);
							}

							// Add triangles
							for (int i = 0; i < (blockMeshData.triangles[0]).Length; i++)
							{
								triangles.Add((blockMeshData.triangles[0])[i] + indexOffset);
							}

							int uvCount = 4;

							int uvX = (block.GetBlockType() - 1) % uvCount;
							int uvY = (uvCount - 1) - (block.GetBlockType() - 1) / uvCount;

							float uvScale = 1f / uvCount;

							// Add UVs
							for (int i = 0; i < blockMeshData.uv.Length; i++)
							{
								uv.Add((blockMeshData.uv[i] + new Vector2(uvX, uvY)) * uvScale);
							}
						}
					} // Six Faces
					else if (model.blockModelType == BlockModel.BlockModelType.SingleModel)
					{
						MeshData blockMeshData = model.singleModel.meshData;

						// What index are we starting this face from
						int indexOffset = vertices.Count;

						// Add vertices
						float randomYAngle = SeedlessRandom.NextFloatInRange(0, 360);
						for (int v = 0; v < blockMeshData.vertices.Length; v++)
						{
							Vector3 middle = new Vector3(x, y, z);

							vert = blockMeshData.vertices[v];
							vert -= Vector3.one / 2;

							vert = Quaternion.Euler(new Vector3(0, randomYAngle, 0)) * vert; // Spin by random degrees
							vert *= 1.4f; // TODO: Find fix for floating instead of just scaling up model

							vert += Vector3.one / 2;
							vert *= chunk.scaleFactor;

							Vector3 vertPos = middle + vert;
							vertices.Add(vertPos);

							norm = blockMeshData.normals[v];
							normals.Add(block.GetNormalRefractive() * norm);
						}

						// Add triangles
						for (int i = 0; i < (blockMeshData.triangles[0]).Length; i++)
						{
							vegTriangles.Add((blockMeshData.triangles[0])[i] + indexOffset);
						}

						int uvCount = 4;

						int uvX = (block.GetBlockType() - 1) % uvCount;
						int uvY = (uvCount - 1) - (block.GetBlockType() - 1) / uvCount;

						float uvScale = 1f / uvCount;

						// Add UVs
						for (int i = 0; i < blockMeshData.uv.Length; i++)
						{
							uv.Add((blockMeshData.uv[i] + new Vector2(uvX, uvY)) * uvScale);
						}
					} // Single Model
				}
			}
		}

		return new MeshData(vertices.ToArray(), normals.ToArray(), uv.ToArray(), new Dictionary<int, int[]> { { 0, triangles.ToArray() }, { 1, vegTriangles.ToArray() } });
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

		//ApplyVertexColors(vertexColors);
	}
}
