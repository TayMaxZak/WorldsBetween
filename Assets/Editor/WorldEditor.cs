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

		EditorGUILayout.LabelField("Chunk count: ", World.GetRealChunkCount().ToString());

		////////////////////
		EditorGUILayout.Space();
		////////////////////

		EditorGUILayout.LabelField("Generators used: ", World.WorldBuilder.GeneratorsUsed().ToString());

		////////////////////
		EditorGUILayout.Space();
		////////////////////

		EditorGUILayout.LabelField("World Builder", EditorStyles.boldLabel);

		var generators = World.WorldBuilder.GetChunkGenerators();

		foreach (KeyValuePair<Chunk.BuildStage, ChunkGenerator> entry in generators)
		{
			EditorGUILayout.LabelField(entry.Key.ToString(), entry.Value.GetSize() + " active chunks (" + entry.Value.GetEdgeChunks() + " edge chunks)");
		}

		EditorGUILayout.LabelField("Light Engine", EditorStyles.boldLabel);

		EditorGUILayout.LabelField("Missing rays: ", "" + (World.LightEngine.RaysMax() - World.LightEngine.ChunksCur()));

		EditorGUILayout.LabelField("Progress:", "" + (int)(100 * World.LightEngine.GetGenProgress()));
	}
}
