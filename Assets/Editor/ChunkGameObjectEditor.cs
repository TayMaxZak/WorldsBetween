using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(ChunkGameObject))]
[CanEditMultipleObjects]
class ChunkGameObjectEditor : Editor
{
	private static GUIStyle style = new GUIStyle();

	void OnSceneGUI()
	{
		ChunkGameObject script = (ChunkGameObject)target;
		if (script == null || !Application.isPlaying)
		{
			return;
		}

		style.normal.textColor = (script.data.buildStage == Chunk.BuildStage.Init || script.data.buildStage == Chunk.BuildStage.Done) ? Color.white : Utils.colorBlue;
		Handles.Label(script.transform.position + Vector3.one * World.GetChunkSize() / 2 + Vector3.up * 2,
			script.transform.position.ToString() + 
			"\ngenStage: " + script.data.buildStage.ToString() + 
			"\nisProcessing: " + script.data.isProcessing.ToString(),
		style);
	}
}