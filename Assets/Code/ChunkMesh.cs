using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh : MonoBehaviour
{
	private static Color borderColor = new Color(0, 0, 0.5f, 0.5f);

	private Chunk chunk;

	private MeshFilter filter;

	public Mesh blockMesh;

	// Save for later
	Vector3[] sharedVertices;
	Color[] colors;

	public void Init(Chunk chunk)
	{
		this.chunk = chunk;

		filter = GetComponentInChildren<MeshFilter>();

		// Duplicate original mesh to avoid permanent changes
		filter.sharedMesh = filter.mesh;
	}

	public void SetVertexColors(Block block)
	{
		float offset = 0.5f;

		Block adj;

		Vector3 meshPos;
		Vector3Int blockPos = new Vector3Int();

		// Loop through all vertices needed
		int loopCounter = 0;
		for (int i = block.startIndex; i < block.endIndex; i++)
		{
			loopCounter++;

			// Find actual block to sample for brightness
			meshPos = sharedVertices[i];
			blockPos.x = (int)(meshPos.x + offset);
			blockPos.y = (int)(meshPos.y + offset);
			blockPos.z = (int)(meshPos.z + offset);

			// Block that's closest to this actual vertex
			adj = World.GetBlockFor(chunk.position + blockPos);
			if (adj == null)
				adj = block;

			// Convert brightness value to float
			float lastBright = adj.brightness / 255f;
			float newBright = adj.brightness / 255f;

			// Convert hue value to float
			float lastHue = adj.colorTemp / 255f;
			float newHue = adj.colorTemp / 255f;

			// Assign lighting data: new brightness, last brightness, new hue, last hue
			colors[i] = new Color(lastBright, newBright, lastHue, newHue);

			//// Placeholder
			//colors[i] = new Color(RandomJitter(0.0f) + 0.2f, RandomJitter(0.0f) + 0.2f, RandomJitter(0.25f) + 0.5f, RandomJitter(0.25f) + 0.5f);
		}

		//if (filter.sharedMesh.colors.Length == 0)
		//	ApplyVertexColors();
	}

	public void ApplyVertexColors()
	{
		// Apply vertex colors
		Mesh mesh = filter.sharedMesh;
		mesh.colors = colors;
	}

	public void GenerateMesh(Block[,,] blocks)
	{
		Block block;

		Mesh newMesh = new Mesh();

		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		List<Vector3> normals = new List<Vector3>();
		List<Vector2> uv = new List<Vector2>();

		Vector3[] blockVert;
		Vector3[] blockNormals;
		int[] blockTri;
		Vector2[] blockUv;

		Vector3 blockMeshOffset;

		int chunkSize = chunk.GetChunkSize();
		for (byte x = 0; x < chunkSize; x++)
		{
			for (byte y = 0; y < chunkSize; y++)
			{
				for (byte z = 0; z < chunkSize; z++)
				{
					block = blocks[x, y, z];

					// Empty block
					if (block.opacity / 255f < 0.5f)
						continue;

					// Remember which vertex index this block starts at
					int indexOffset = vertices.Count;

					block.startIndex = indexOffset;

					// Local position offset for this block
					blockMeshOffset.x = x;
					blockMeshOffset.y = y;
					blockMeshOffset.z = z;

					// Add vertices
					blockVert = blockMesh.vertices;

					for (int i = 0; i < blockVert.Length; i++)
					{
						vertices.Add(blockVert[i] + Random.onUnitSphere * (0.5f - block.opacity / 255f) + blockMeshOffset);
					}

					block.endIndex = vertices.Count;

					// Add normals
					blockNormals = blockMesh.normals;

					for (int i = 0; i < blockNormals.Length; i++)
					{
						normals.Add(blockNormals[i]);
					}

					// Add triangles
					blockTri = blockMesh.triangles;

					for (int i = 0; i < blockTri.Length; i++)
					{
						triangles.Add(blockTri[i] + indexOffset);
					}

					// Add UVs
					blockUv = blockMesh.uv;

					for (int i = 0; i < blockUv.Length; i++)
					{
						uv.Add(blockUv[i]);
					}
				}
			}
		}

		newMesh.vertices = vertices.ToArray();
		newMesh.triangles = triangles.ToArray();
		newMesh.normals = normals.ToArray();
		newMesh.uv = uv.ToArray();

		filter.mesh = newMesh;

		sharedVertices = filter.sharedMesh.vertices;

		Color baseColor = new Color(0, 0, 0.5f, 0.5f);
		colors = new Color[sharedVertices.Length];
		for (int i = 0; i < colors.Length; i++)
			colors[i] = baseColor;

		ApplyVertexColors();
	}

	private static float RandomJitter(float mult)
	{
		return (Random.value - 0.5f) * mult;
	}
}
