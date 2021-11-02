using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

[CustomEditor(typeof(World))]
[CanEditMultipleObjects]
public class WorldEditor : Editor
{
	public override bool RequiresConstantRepaint()
	{
		return true;
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		World script = (World)target;

		////////////////////
		EditorGUILayout.Space();
		////////////////////

		EditorGUILayout.LabelField("World Size", EditorStyles.boldLabel);

		EditorGUILayout.LabelField("Chunk count: ", World.WorldEditorInfo.GetChunkCount(script).ToString());

		////////////////////
		EditorGUILayout.Space();
		////////////////////

		EditorGUILayout.LabelField("Generators used: ", World.WorldEditorInfo.GetGeneratorsUsed(script).ToString());

		////////////////////
		EditorGUILayout.Space();
		////////////////////

		EditorGUILayout.LabelField("Chunk Generators", EditorStyles.boldLabel);

		var generators = World.WorldEditorInfo.GetChunkGenerators(script);

		foreach (KeyValuePair<Chunk.GenStage, ChunkGenerator> entry in generators)
		{
			EditorGUILayout.LabelField(entry.Key.ToString(), entry.Value.GetSize() + " active chunks (" + entry.Value.GetEdgeChunks() + " edge chunks)");
		}
	}
}
