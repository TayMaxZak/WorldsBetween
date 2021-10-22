using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh : MonoBehaviour
{
	private Chunk chunk;

	public MeshFilter dummyMesh;
	private List<MeshFilter> meshes;

	private float vertexOffset = -0.5f;

	public void Init(Chunk chunk)
	{
		this.chunk = chunk;

		// Add meshes in fixed order
		meshes = new List<MeshFilter>();
		foreach (Transform t in transform)
		{
			MeshFilter mesh = t.GetComponent<MeshFilter>();
			if (mesh)
				meshes.Add(mesh);
		}

		// Duplicate original meshes to avoid permanent changes
		foreach (MeshFilter filter in meshes)
			filter.sharedMesh = filter.mesh;
	}

	public void SetVertexColors(Block[,,] blocks)
	{
		Vector3[] vertices;
		Color[] colors;

		Vector3 meshPos;
		Vector3Int blockPos = new Vector3Int();

		Block block;

		foreach (MeshFilter filter in meshes)
		{
			vertices = filter.sharedMesh.vertices;
			colors = new Color[vertices.Length];

			for (int i = 0; i < vertices.Length; i++)
			{
				float jitter = 0.0f;

				// Find block to sample for brightness
				meshPos = filter.transform.localPosition + filter.sharedMesh.vertices[i];
				blockPos.x = Mathf.RoundToInt(meshPos.x + vertexOffset + RandomJitter(jitter));
				blockPos.y = Mathf.RoundToInt(meshPos.y + vertexOffset + RandomJitter(jitter));
				blockPos.z = Mathf.RoundToInt(meshPos.z + vertexOffset + RandomJitter(jitter));

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
						// Assign vertex color for block
						colors[i] = Color.blue;

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
			Mesh mesh = filter.sharedMesh;
			mesh.colors = colors;
		}
	}

	private static float RandomJitter(float mult)
	{
		return (Random.value - 0.5f) * mult;
	}
}
