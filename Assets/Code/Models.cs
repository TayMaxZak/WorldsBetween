using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Models : MonoBehaviour
{
	private static Models Instance;

	[SerializeField]
	private Mesh blockMesh;

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
	}

	public static Mesh GetBlockMesh()
	{
		return Instance.blockMesh;
	}
}
