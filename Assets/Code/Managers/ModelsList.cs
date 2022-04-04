using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelsList : MonoBehaviour
{
	private static ModelsList Instance;

	[SerializeField]
	private BlockModel[] models;

	private void Awake()
	{
		// Ensure singleton
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}
		else
			Instance = this;

		// Get mesh data
		foreach (BlockModel bm in models)
		{
			if (bm.blockModelType == BlockModel.BlockModelType.SixFaces)
				foreach (BlockModel.SurfaceModel sm in bm.faces)
					sm.meshData = new ChunkMesh.MeshData(sm.faceMesh);
			else if (bm.blockModelType == BlockModel.BlockModelType.SingleModel)
				bm.singleModel.meshData = new ChunkMesh.MeshData(bm.singleModel.mesh);
		}
	}

	public static BlockModel GetModelFor(int blockType)
	{
		return Instance.models[blockType];
	}
}
