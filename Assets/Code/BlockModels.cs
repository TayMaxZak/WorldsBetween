using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockModels : MonoBehaviour
{
	private static BlockModels Instance;

	[SerializeField]
	private Mesh[] models;

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

	public static Mesh GetModelFor(int blockType)
	{
		return Instance.models[blockType];
	}
}
