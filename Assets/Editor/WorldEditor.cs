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
	protected static bool showDefaultSettings = false;

	public override bool RequiresConstantRepaint()
	{
		return true;
	}

	private void OnEnable()
	{
		showDefaultSettings = !Application.isPlaying;
	}

	public override void OnInspectorGUI()
	{
		//showDefaultSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showDefaultSettings, "World Settings");
		showDefaultSettings = EditorGUILayout.Toggle("Show World Settings", showDefaultSettings);

		if (showDefaultSettings)
			base.OnInspectorGUI();

		//EditorGUILayout.EndFoldoutHeaderGroup();

		if (!Application.isPlaying)
			return;

		World script = (World)target;

		////////////////////
		EditorGUILayout.Space();
		////////////////////

		EditorGUILayout.LabelField("World Size", EditorStyles.boldLabel);

		EditorGUILayout.LabelField("Chunk count: ", World.GetChunks().Count.ToString());

		////////////////////
		EditorGUILayout.Space();
		////////////////////

		EditorGUILayout.LabelField("Generators used: ", World.WorldBuilder.GeneratorsUsed().ToString());

		////////////////////
		EditorGUILayout.Space();
		////////////////////

		EditorGUILayout.LabelField("Chunk Generators", EditorStyles.boldLabel);

		var generators = World.WorldBuilder.GetChunkGenerators();

		foreach (KeyValuePair<Chunk.ProcStage, ChunkGenerator> entry in generators)
		{
			EditorGUILayout.LabelField(entry.Key.ToString(), entry.Value.GetSize() + " active chunks (" + entry.Value.GetEdgeChunks() + " edge chunks)");
		}
	}
}
