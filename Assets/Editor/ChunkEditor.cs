using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(Chunk))]
[CanEditMultipleObjects]
class LabelHandle : Editor
{
	private static GUIStyle style = new GUIStyle();

	void OnSceneGUI()
	{
		Chunk chunk = (Chunk)target;
		if (chunk == null || !Application.isPlaying)
		{
			return;
		}

		style.normal.textColor = chunk.atEdge ? Color.red : ((chunk.genStage == Chunk.GenStage.Empty || chunk.genStage == Chunk.GenStage.Ready) ? Color.white : Utils.colorBlue);
		Handles.Label(chunk.transform.position + Vector3.one * World.GetChunkSize() / 2 + Vector3.up * 2,
			chunk.transform.position.ToString() + 
			"\ngenStage: " + chunk.genStage.ToString() + 
			"\natEdge: " + chunk.atEdge.ToString(),
		style);
	}
}