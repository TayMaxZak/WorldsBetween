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

		style.normal.textColor = script.data.atEdge ? Color.red : ((script.data.genStage == Chunk.GenStage.Empty || script.data.genStage == Chunk.GenStage.Ready) ? Color.white : Utils.colorBlue);
		Handles.Label(script.transform.position + Vector3.one * World.GetChunkSize() / 2 + Vector3.up * 2,
			script.transform.position.ToString() + 
			"\ngenStage: " + script.data.genStage.ToString() + 
			"\natEdge: " + script.data.atEdge.ToString() +
			"\nisProcessing: " + script.data.isProcessing.ToString(),
		style);
	}
}