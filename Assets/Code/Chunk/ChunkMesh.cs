﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh
{
	private static readonly Color32 defaultColor = new Color32(0, 0, 0, 0);

	private Chunk chunk;

	private MeshFilter meshVisual;

	private MeshCollider meshPhysics;

	// Save for later
	private Vector3[] sharedVertices;
	private Color32[] vertexColors;

	private static readonly Vector3Int[] directions = new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0),
													new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)};

	private static readonly Vector3Int[] rotations = new Vector3Int[] { new Vector3Int(0, 90, 0), new Vector3Int(0, -90, 0), new Vector3Int(-90, 0, 0),
													new Vector3Int(90, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(0, 180, 0)};

	public void Init(Chunk chunk, MeshFilter meshVisual, MeshCollider meshPhysics)
	{
		this.chunk = chunk;

		this.meshVisual = meshVisual;

		this.meshPhysics = meshPhysics;

		// Duplicate original mesh to avoid permanent changes
		meshVisual.sharedMesh = meshVisual.mesh;
	}

	public Mesh GetSharedMesh()
	{
		return meshVisual.sharedMesh;
	}

	public struct MeshData
	{
		public Vector3[] vertices;
		public Vector3[] normals;
		public Vector2[] uv;
		public Vector2[] uv2;
		public Color32[] colors32;

		public Dictionary<int, int[]> triangles;

		public MeshData(Mesh mesh)
		{
			vertices = mesh.vertices;
			normals = mesh.normals;
			uv = mesh.uv;
			uv2 = mesh.uv2;

			if (mesh.colors32.Length == 0)
				colors32 = new Color32[vertices.Length];
			else
				colors32 = mesh.colors32;

			triangles = new Dictionary<int, int[]>();
			triangles[0] = mesh.triangles;
		}

		public MeshData(Vector3[] vertices, Vector3[] normals, Vector2[] uv, Vector2[] uv2, Color32[] colors32, Dictionary<int, int[]> triangles)
		{
			this.vertices = vertices;
			this.normals = normals;
			this.uv = uv;
			this.uv2 = uv2;
			this.colors32 = colors32;

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

		List<Vector2> uv0 = new List<Vector2>();
		List<Vector2> uv1 = new List<Vector2>();

		List<Color32> colors32 = new List<Color32>();

		List<int> airDirections = new List<int>();


		int chunkSize = chunk.chunkSizeWorld;
		for (int x = 0; x < chunkSize; x++)
		{
			for (int y = 0; y < chunkSize; y++)
			{
				for (int z = 0; z < chunkSize; z++)
				{
					Vector3Int faceOffset = new Vector3Int();
					Vector3 vert;
					Vector3 norm;
					// Random pos jitter per block
					Vector3 posJitter = new Vector3(SeedlessRandom.NextFloatInRange(-0.33f, 0.33f), SeedlessRandom.NextFloatInRange(-0.25f, -0.3f), SeedlessRandom.NextFloatInRange(-0.33f, 0.33f));

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

						if (chunk.chunkType == Chunk.ChunkType.Close && !World.Contains(faceOffset))
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

							bool flipTopology = SeedlessRandom.NextFloat() < 0.5f;

							// Show debug rays in some places
							bool drawDebugRay = false/*SeedlessRandom.NextFloat() > 0.99f*/;

							// What index are we starting this face from
							int indexOffset = vertices.Count;

							// Add vertices
							for (int v = 0; v < blockMeshData.vertices.Length; v++)
							{
								Vector3 middle = new Vector3(0.5f * chunk.scaleFactor + x, 0.5f * chunk.scaleFactor + y, 0.5f * chunk.scaleFactor + z);

								Vector3 inVertex = blockMeshData.vertices[v];
								inVertex.x *= flipTopology ? -1 : 1;

								vert = Quaternion.Euler(rotations[d]) * (inVertex + Vector3.forward * 0.5f);
								vert *= chunk.scaleFactor;

								Vector3 vertPos = middle + vert;

								// Calculate normals based on adjacent blocks
								int empty = 0;
								norm = Vector3.zero;
								for (int i = -1; i <= 1; i += 2)
								{
									for (int j = -1; j <= 1; j += 2)
									{
										for (int k = -1; k <= 1; k += 2)
										{
											if (!World.GetBlock(
												Mathf.FloorToInt(chunk.position.x + vertPos.x + (i * 0.5f - 0.0f) * chunk.scaleFactor),
												Mathf.FloorToInt(chunk.position.y + vertPos.y + (j * 0.5f - 0.0f) * chunk.scaleFactor),
												Mathf.FloorToInt(chunk.position.z + vertPos.z + (k * 0.5f - 0.0f) * chunk.scaleFactor)
												).IsOpaque())
											{
												empty++;
												norm += new Vector3Int(i, j, k);

												if (drawDebugRay)
													Debug.DrawRay(vertPos + chunk.position, new Vector3(i, j, k) * 0.1f, Color.blue, 200);
											}
											else if (drawDebugRay)
												Debug.DrawRay(vertPos + chunk.position, new Vector3(i, j, k) * 0.1f, Color.red, 200);
										}
									}
								}

								// Stuck between corners, use hard normal always
								if (norm == Vector3.zero)
								{
									normals.Add(block.GetNormalRefractive() * (Vector3)directions[d]);

									vertices.Add(vertPos);
								}
								// Normal case
								else
								{
									// Smooth vs hard determined by block material
									float hardness = block.GetNormalHardness();

									normals.Add(block.GetNormalRefractive() * Vector3.Lerp(norm.normalized, directions[d], hardness).normalized);

									// Distances for normal offset
									float normalAmt = 0.4f; // 1 - sqrt(3) =  0.134f
									float normalDir = 0;

									if (empty < 4)
										normalDir = 1;
									else if (empty > 4)
										normalDir = -1;

									if (drawDebugRay)
										Debug.Log(empty);

									//float clampDistance = Mathf.Lerp(cornerDistance, faceDistance, dot);
									Vector3 clampedVert = middle + vert + normalAmt * normalDir * norm.normalized;

									Vector3 displacedVert = Vector3.Lerp(vertPos, clampedVert, block.GetMeshSmoothing());
									vertices.Add(displacedVert);
								}

								// Vertex colors
								Color32 vertexColor = defaultColor;
								vertexColor.r = (byte)(255 * World.GetAttributes(
									Mathf.RoundToInt(chunk.position.x + vertPos.x + SeedlessRandom.NextFloatInRange(-0.1f, 0.1f)),
									Mathf.RoundToInt(chunk.position.y + vertPos.y + SeedlessRandom.NextFloatInRange(-0.1f, 0.1f)),
									Mathf.RoundToInt(chunk.position.z + vertPos.z + SeedlessRandom.NextFloatInRange(-0.1f, 0.1f))
								).GetMoss());

								colors32.Add(vertexColor);
							}

							// Add triangles
							for (int i = 0; i < (blockMeshData.triangles[0]).Length; i++)
							{
								if (!flipTopology)
									triangles.Add((blockMeshData.triangles[0])[i] + indexOffset);
								else
									triangles.Add((blockMeshData.triangles[0])[(blockMeshData.triangles[0]).Length - i - 1] + indexOffset);
							}

							int uvCount = 4;

							int uvX = (block.GetBlockType() - 1) % uvCount;
							int uvY = (uvCount - 1) - (block.GetBlockType() - 1) / uvCount;

							float uvScale = 1f / uvCount;

							// Add UVs
							for (int i = 0; i < blockMeshData.uv.Length; i++)
							{
								Vector2 inUv = new Vector2(blockMeshData.uv[i].x, blockMeshData.uv[i].y);
								if (flipTopology)
									inUv.x = 1 - inUv.x; 

								uv0.Add((Vector2.one / 4f + inUv / 2f + new Vector2(uvX, uvY)) * uvScale);
							}
							for (int i = 0; i < blockMeshData.uv.Length; i++)
							{
								Vector2 inUv = new Vector2(blockMeshData.uv[i].x, blockMeshData.uv[i].y);
								if (flipTopology)
									inUv.x = 1 - inUv.x;

								uv1.Add(inUv);
							}
						}
					} // end Six Faces
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
							vert *= 1.414f;
							vert += posJitter;
							// Random displacement per vertex
							vert += new Vector3(SeedlessRandom.NextFloatInRange(-0.25f, 0.25f), SeedlessRandom.NextFloatInRange(-0.25f, 0.25f), SeedlessRandom.NextFloatInRange(-0.25f, 0.25f));

							vert += Vector3.one / 2;
							vert *= chunk.scaleFactor;

							Vector3 vertPos = middle + vert;
							vertices.Add(vertPos);

							norm = blockMeshData.normals[v];
							normals.Add(block.GetNormalRefractive() * norm);

							// Vertex colors
							Color32 vertexColor = defaultColor;
							vertexColor.r = (byte)(255 * World.GetAttributes(
								Mathf.RoundToInt(chunk.position.x + vertPos.x + SeedlessRandom.NextFloatInRange(-0.1f, 0.1f)),
								Mathf.RoundToInt(chunk.position.y + vertPos.y + SeedlessRandom.NextFloatInRange(-0.1f, 0.1f)),
								Mathf.RoundToInt(chunk.position.z + vertPos.z + SeedlessRandom.NextFloatInRange(-0.1f, 0.1f))
							).GetMoss());

							colors32.Add(vertexColor);
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
							uv0.Add((Vector2.one / 4f + blockMeshData.uv[i] / 2f + new Vector2(uvX, uvY)) * uvScale);
						}
						for (int i = 0; i < blockMeshData.uv.Length; i++)
						{
							uv1.Add(blockMeshData.uv[i]);
						}
					} // end Single Model
				}
			}
		}

		return new MeshData(vertices.ToArray(), normals.ToArray(), uv0.ToArray(), uv1.ToArray(), colors32.ToArray(), new Dictionary<int, int[]> { { 0, triangles.ToArray() }, { 1, vegTriangles.ToArray() } });
	}

	public void FinishMesh(Mesh newMesh)
	{
		// Can happen if thread finishes after games ends
		if (meshVisual == null)
			return;

		meshVisual.mesh = newMesh;
		meshPhysics.sharedMesh = newMesh;

		sharedVertices = meshVisual.sharedMesh.vertices;
	}
}
