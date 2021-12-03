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
	private Vector3[][] vertices;
	[SerializeField]
	private Vector3[][] normals;
	[SerializeField]
	private Color32[][] colors;

	private void Awake()
	{
		filters = GetComponentsInChildren<MeshFilter>();

		meshes = new Mesh[filters.Length];
		for (int i = 0; i < meshes.Length; i++)
			meshes[i] = filters[i].mesh;

		vertices = new Vector3[meshes.Length][];
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = meshes[i].vertices;
		}

		normals = new Vector3[meshes.Length][];
		for (int i = 0; i < normals.Length; i++)
		{
			normals[i] = meshes[i].normals;
		}

		colors = new Color32[meshes.Length][];
		for (int i = 0; i < colors.Length; i++)
		{
			colors[i] = new Color32[vertices[i].Length];
		}
	}

	private void Update()
	{
		for (int i = 0; i < colors.Length; i++)
		{
			for (int k = 0; k < colors[i].Length; k++)
			{
				colors[i][k] = GetColor(vertices[i][k], normals[i][k]);
			}
			meshes[i].colors32 = colors[i];
		}
	}

	public Color32 GetColor(Vector3 vert, Vector3 norm)
	{
		Vector3Int blockPos = new Vector3Int();

		// Find actual block to sample for brightness
		blockPos.x = Mathf.RoundToInt(vert.x);
		blockPos.y = Mathf.RoundToInt(vert.y);
		blockPos.z = Mathf.RoundToInt(vert.z);

		// Surfaces closest to this actual vertex
		LightingSample ls = World.GetChunkFor(blockPos).CalcLightAt(blockPos, norm);

		float lastBright = ls.brightness;

		float newBright = ls.brightness;

		float lastColorTemp = (ls.colorTemp + 1) / 2f;

		float newColorTemp = (ls.colorTemp + 1) / 2f;

		// Assign lighting data: new brightness, last brightness, new hue, last hue
		return new Color(lastBright, newBright, lastColorTemp, newColorTemp);
	}
}
