using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

[CustomEditor(typeof(Chunk))]
[CanEditMultipleObjects]
public class ChunkEditor : Editor
{
	public override void OnInspectorGUI()
	{
		if (!EditorApplication.isPlaying)
		{
			DrawDefaultInspector();

			if (GUILayout.Button("Create Dummy Mesh"))
			{
				foreach (UnityEngine.Object target in targets)
				{
					Chunk myScript = (Chunk)target;

					myScript.CreateDummyMesh();
				}
			}
		}
		else
			base.OnInspectorGUI();
	}
}
