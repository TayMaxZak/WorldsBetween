using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh : MonoBehaviour
{
	private Chunk chunk;

	public MeshFilter dummyMesh;
	private List<MeshFilter> meshes;

	private float vertexOffset = -0.5f;
	private List<Color> allColors = new List<Color>();

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

	public void SetVertexColors(Block[,,] blocks, float partialTime)
	{
		Vector3 dummy1;
		Vector3Int dummy2 = new Vector3Int();
		Block dummy3;

		foreach (MeshFilter filter in meshes)
		{
			// Reset colors
			allColors.Clear();

			for (int i = 0; i < filter.sharedMesh.vertexCount; i++)
			{
				float jitter = 0.0f;

				// Find block to sample for brightness
				dummy1 = filter.transform.localPosition + filter.sharedMesh.vertices[i];
				dummy2.x = Mathf.RoundToInt(dummy1.x + vertexOffset + RandomJitter(jitter));
				dummy2.y = Mathf.RoundToInt(dummy1.y + vertexOffset + RandomJitter(jitter));
				dummy2.z = Mathf.RoundToInt(dummy1.z + vertexOffset + RandomJitter(jitter));

				// Block is in this chunk?
				if (chunk.ContainsPos(dummy2.x, dummy2.y, dummy2.z))
				{
					dummy3 = blocks[dummy2.x, dummy2.y, dummy2.z];
				}
				// Block is outside this chunk?
				else
				{
					dummy3 = World.GetBlockFor(dummy2 + chunk.position);

					// Block is outside this world
					if (dummy3 == null)
					{
						// Assign vertex color for block
						allColors.Add(Color.blue);

						continue;
					}
				}

				// Convert brightness value to float, interpolating between light updates
				float a = dummy3.lastBrightness / 256f;
				float b = dummy3.brightness / 256f;

				float bright = Mathf.Lerp(a, b, partialTime);

				// Assign vertex color for block
				allColors.Add(new Color(bright, bright, bright)); // TODO: Cache colors?
			}

			// Apply vertex colors
			filter.sharedMesh.colors = allColors.ToArray();
		}
	}

	private static float RandomJitter(float mult)
	{
		return (Random.value - 0.5f) * mult;
	}
}
