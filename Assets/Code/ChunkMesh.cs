using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh : MonoBehaviour
{
	private static Color borderColor = new Color(0, 0, 0.5f, 0.5f);

	private Chunk chunk;

	public MeshFilter chunkMeshPrefab;
	public MeshFilter filter;

	public List<Mesh> blockMeshes;

	public void Init(Chunk chunk)
	{
		this.chunk = chunk;

		filter = GetComponentInChildren<MeshFilter>();

		// Duplicate original mesh to avoid permanent changes
		filter.sharedMesh = filter.mesh;
	}

	public void SetVertexColors(Block[,,] blocks)
	{
		Vector3[] vertices;
		Color[] colors;

		Vector3 meshPos;
		Vector3Int blockPos = new Vector3Int();

		Block block;	
		
		// Go through chunk mesh
		vertices = filter.sharedMesh.vertices;
		colors = new Color[vertices.Length];

		for (int i = 0; i < vertices.Length; i++)
		{
			float jitter = 0.0f;

			// Find block to sample for brightness
			meshPos = filter.transform.localPosition + filter.sharedMesh.vertices[i];
			blockPos.x = Mathf.RoundToInt(meshPos.x + RandomJitter(jitter));
			blockPos.y = Mathf.RoundToInt(meshPos.y + RandomJitter(jitter));
			blockPos.z = Mathf.RoundToInt(meshPos.z + RandomJitter(jitter));

			// Block is in this chunk?
			if (chunk.ContainsPos(blockPos.x, blockPos.y, blockPos.z))
			{
				block = blocks[blockPos.x, blockPos.y, blockPos.z];
			}
			// Block is outside this chunk?
			else
			{
				block = World.GetBlockFor(blockPos + chunk.position + Vector3Int.one);

				// Block is outside this world
				if (block == null)
				{
					// Assign vertex color for block
					colors[i] = borderColor;

					continue;
				}
			}

			// Convert brightness value to float
			float lastBright = block.lastBrightness / 255f;
			float newBright = block.brightness / 255f;

			// Convert hue value to float
			float lastHue = block.lastColorTemp / 255f;
			float newHue = block.colorTemp / 255f;

			// Assign lighting data: new brightness, last brightness, new hue, last hue
			// Saturation is affected by high brightness; very bright = not saturated
			colors[i] = new Color(lastBright, newBright, lastHue, newHue);
		}

		// Apply vertex colors
		// TODO: See what happens if it doesn't set the colors. Blending gone wrong?
		Mesh mesh = filter.sharedMesh;
		mesh.colors = colors;
	}

	public void SetOpacity(Block[,,] blocks)
	{
		float vertexOffset = 0;

		Vector3 meshPos;
		Vector3Int blockPos = new Vector3Int();

		Block block;

		Mesh newMesh = new Mesh();

		// Find block to sample for opacity
		meshPos = filter.transform.localPosition;
		blockPos.x = Mathf.RoundToInt(meshPos.x + vertexOffset);
		blockPos.y = Mathf.RoundToInt(meshPos.y + vertexOffset);
		blockPos.z = Mathf.RoundToInt(meshPos.z + vertexOffset);

		// Block is in this chunk?
		if (chunk.ContainsPos(blockPos.x, blockPos.y, blockPos.z))
		{
			block = blocks[blockPos.x, blockPos.y, blockPos.z];
		}
		// Block is outside this chunk?
		else
		{
			block = World.GetBlockFor(blockPos + chunk.position);

			// Block is outside this world
			if (block == null)
			{
				filter.mesh = newMesh;

				return;
			}
		}

		if (block.opacity / 255f < 0.5f)
			filter.mesh = newMesh;
		else
			filter.gameObject.SetActive(true);
	}

	private static float RandomJitter(float mult)
	{
		return (Random.value - 0.5f) * mult;
	}
}
