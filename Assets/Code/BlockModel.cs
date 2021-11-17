using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Block Model")]
[System.Serializable]
public class BlockModel : ScriptableObject
{
	public SurfaceModel[] faces = new SurfaceModel[6] {
		new SurfaceModel("Right"),
		new SurfaceModel("Left"),
		new SurfaceModel("Top"),
		new SurfaceModel("Bottom"),
		new SurfaceModel("Front"),
		new SurfaceModel("Back")
	};

	[System.Serializable]
	public class SurfaceModel
	{
		public string label;
		public Mesh faceMesh;
		public bool replaceAdjModel; // e.g., the bottom model of this block replaces the top model of the block under it

		public SurfaceModel(string label)
		{
			this.label = label;
		}
	}
}
