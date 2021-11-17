﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockModels : MonoBehaviour
{
	private static BlockModels Instance;

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
	}

	public static BlockModel GetModelFor(int blockType)
	{
		return Instance.models[blockType];
	}
}
