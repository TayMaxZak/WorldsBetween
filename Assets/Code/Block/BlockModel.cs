using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Block Model")]
[System.Serializable]
public class BlockModel : ScriptableObject
{
	public enum BlockModelType
	{
		SixFaces,
		SingleModel
	}
	public BlockModelType blockModelType = BlockModelType.SixFaces;

	public SurfaceModel[] faces = new SurfaceModel[6] {
		new SurfaceModel("Right"),
		new SurfaceModel("Left"),
		new SurfaceModel("Top"),
		new SurfaceModel("Bottom"),
		new SurfaceModel("Front"),
		new SurfaceModel("Back")
	};

	public SingleModel singleModel;

	[System.Serializable]
	public class SurfaceModel
	{
		public string label;
		public Mesh faceMesh;
		public bool replaceAdjModel; // e.g., the bottom model of this block replaces the top model of the block under it

		[HideInInspector]
		public ChunkMesh.MeshData meshData;

		public SurfaceModel(string label)
		{
			this.label = label;
		}
	}

	[System.Serializable]
	public class SingleModel
	{
		public string label;
		public Mesh mesh;

		[HideInInspector]
		public ChunkMesh.MeshData meshData;

		public SingleModel(string label)
		{
			this.label = label;
		}
	}
}
