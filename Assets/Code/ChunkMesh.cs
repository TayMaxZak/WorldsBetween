using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh : MonoBehaviour
{
	private Chunk chunk;

	private List<MeshFilter> meshes;

	private List<Color> allColors = new List<Color>();

	public void Init(Chunk chunk)
	{
		this.chunk = chunk;

		meshes = new List<MeshFilter>(GetComponentsInChildren<MeshFilter>());
	}

	public void SetVertexColors(Block[] blocks)
	{
		Vector3 dummy1;
		Vector3Int dummy2 = new Vector3Int();

		foreach (MeshFilter filter in meshes)
		{
			// Reset colors
			allColors.Clear();

			for (int i = 0; i < filter.sharedMesh.vertices.Length; i++)
			{
				dummy1 = filter.transform.position;
				dummy2.x = Mathf.FloorToInt(dummy1.x);
				dummy2.y = Mathf.FloorToInt(dummy1.y);
				dummy2.z = Mathf.FloorToInt(dummy1.z);

				float bright = Block.GetFloatBrightness(blocks[chunk.CoordToIndex(dummy2.x, dummy2.y, dummy2.z)].brightness);
				allColors.Add(new Color(bright, bright, bright));
			}

			filter.sharedMesh = filter.mesh;
			filter.sharedMesh.colors = allColors.ToArray();
		}
	}
}
