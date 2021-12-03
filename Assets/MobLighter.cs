using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobLighter : MonoBehaviour
{
	private static Color resetColor = new Color(0.4f, 0.5f, 0.2f, 0.2f);

	[SerializeField]
	private MeshFilter[] filters;
	[SerializeField]
	private Mesh[] meshes;
	[SerializeField]
	private Color32[][] colors;

	private void Awake()
	{
		filters = GetComponentsInChildren<MeshFilter>();

		meshes = new Mesh[filters.Length];
		for (int i = 0; i < meshes.Length; i++)
			meshes[i] = filters[i].mesh;

		colors = new Color32[meshes.Length][];
		for (int i = 0; i < colors.Length; i++)
		{
			colors[i] = new Color32[meshes[i].vertices.Length];
		}
	}

	private void Update()
	{
		for (int i = 0; i < colors.Length; i++)
		{
			for (int k = 0; k < colors[i].Length; k++)
			{
				colors[i][k] = SeedlessRandom.NextFloat() > 0.5f ? Color.white : resetColor;
			}
			meshes[i].colors32 = colors[i];
		}
	}
}
