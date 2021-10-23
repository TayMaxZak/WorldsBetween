using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh : MonoBehaviour
{
	private static Color borderColor = new Color(0, 0, 0.5f, 0.5f);

	private Chunk chunk;

	private MeshFilter filter;

	public Mesh blockMesh;

	// Remember
	Vector3[] vertices;

	public void Init(Chunk chunk)
	{
		this.chunk = chunk;

		filter = GetComponentInChildren<MeshFilter>();

		// Duplicate original mesh to avoid permanent changes
		filter.sharedMesh = filter.mesh;
	}

	//public void SetVertexColors(Block[,,] blocks, bool doAll)
	//{
	//	Vector3 meshPos;
	//	Vector3Int blockPos = new Vector3Int();

	//	Block block;

	//	// Remember mesh data
	//	vertices = filter.sharedMesh.vertices;

	//	Color[] colors = filter.sharedMesh.colors;
	//	if (colors.Length == 0)
	//		colors = new Color[vertices.Length];

	//	// Loop through all vertices needed
	//	int loopCounter = 0;
	//	for (int o = 0; o < (doAll ? vertices.Length : Mathf.Min(4, vertices.Length)); o++)
	//	{
	//		loopCounter++;

	//		int i = doAll ? o : Random.Range(0, vertices.Length);

	//		// Randomly offset vertices for fun
	//		vertices[i] += Random.insideUnitSphere * 0.01f;

	//		// Find block to sample for brightness
	//		meshPos = filter.transform.localPosition + filter.sharedMesh.vertices[i];
	//		blockPos.x = Mathf.RoundToInt(meshPos.x);
	//		blockPos.y = Mathf.RoundToInt(meshPos.y);
	//		blockPos.z = Mathf.RoundToInt(meshPos.z);

	//		// Block is in this chunk?
	//		if (chunk.ContainsPos(blockPos.x, blockPos.y, blockPos.z))
	//		{
	//			block = blocks[blockPos.x, blockPos.y, blockPos.z];
	//		}
	//		// Block is outside this chunk?
	//		else
	//		{
	//			block = World.GetBlockFor(blockPos + chunk.position + Vector3Int.one);

	//			// Block is outside this world
	//			if (block == null)
	//			{
	//				// Assign vertex color for block
	//				colors[i] = borderColor;

	//				continue;
	//			}
	//		}

	//		// Convert brightness value to float
	//		float lastBright = block.lastBrightness / 255f;
	//		float newBright = block.brightness / 255f;

	//		// Convert hue value to float
	//		float lastHue = block.lastColorTemp / 255f;
	//		float newHue = block.colorTemp / 255f;

	//		// Assign lighting data: new brightness, last brightness, new hue, last hue
	//		colors[i] = new Color(lastBright, newBright, lastHue, newHue);

	//		//// Placeholder
	//		//colors[i] = new Color(RandomJitter(0.0f) + 0.2f, RandomJitter(0.0f) + 0.2f, RandomJitter(0.25f) +  0.5f, RandomJitter(0.25f) + 0.5f);
	//	}

	//	// Apply vertex colors
	//	// TODO: See what happens if it doesn't set the colors. Blending gone wrong?
	//	Mesh mesh = filter.sharedMesh;
	//	mesh.colors = colors;
	//	mesh.vertices = vertices;

	//	//Debug.Log(name + ": " + loopCounter);
	//}

	public void SetVertexColors(Block block)
	{
		Vector3 meshPos;
		Vector3Int blockPos = new Vector3Int();

		// Remember mesh data
		vertices = filter.sharedMesh.vertices;

		Color[] colors = filter.sharedMesh.colors;
		if (colors.Length == 0)
			colors = new Color[vertices.Length];

		// Loop through all vertices needed
		int loopCounter = 0;
		for (int i = block.startIndex; i < block.endIndex; i++)
		{
			loopCounter++;

			// Find block to sample for brightness
			meshPos = filter.transform.localPosition + filter.sharedMesh.vertices[i];
			blockPos.x = Mathf.RoundToInt(meshPos.x);
			blockPos.y = Mathf.RoundToInt(meshPos.y);
			blockPos.z = Mathf.RoundToInt(meshPos.z);

			// Block matches?
			if (!chunk.ContainsPos(blockPos.x, blockPos.y, blockPos.z)/* || chunk.GetBlock(blockPos.x, blockPos.y, blockPos.z) != block*/)
				continue;

			// Convert brightness value to float
			float lastBright = block.brightness / 255f; // TODO: Fix
			float newBright = block.brightness / 255f;

			// Convert hue value to float
			float lastHue = block.colorTemp / 255f; // TODO: Fix
			float newHue = block.colorTemp / 255f;

			// Assign lighting data: new brightness, last brightness, new hue, last hue
			colors[i] = new Color(lastBright, newBright, lastHue, newHue);
		}

		// Apply vertex colors
		// TODO: See what happens if it doesn't set the colors. Blending gone wrong?
		Mesh mesh = filter.sharedMesh;
		mesh.colors = colors;
		mesh.vertices = vertices;

		//Debug.Log(name + ": " + loopCounter);
	}

	public void GenerateMesh(Block[,,] blocks)
	{
		Block block;

		Mesh newMesh = new Mesh();

		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		List<Vector3> normals = new List<Vector3>();

		Vector3[] blockVert;
		Vector3[] blockNormals;
		int[] blockTri;

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
						vertices.Add(blockVert[i] + blockMeshOffset);
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
				}
			}
		}

		newMesh.vertices = vertices.ToArray();
		newMesh.triangles = triangles.ToArray();
		newMesh.normals = normals.ToArray();

		filter.mesh = newMesh;
	}

	private static float RandomJitter(float mult)
	{
		return (Random.value - 0.5f) * mult;
	}
}
