using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(Chunk))]
class LabelHandle : Editor
{
	private static GUIStyle style = new GUIStyle();

	void OnSceneGUI()
	{
		Chunk chunk = (Chunk)target;
		if (chunk == null)
		{
			return;
		}

		style.normal.textColor = Color.blue;
		Handles.Label(chunk.transform.position + Vector3.one * World.GetChunkSize() / 2 + Vector3.up * 2,
			chunk.transform.position.ToString() + 
			"\ngenStage: " + chunk.genStage.ToString() + 
			"\natEdge: " + chunk.atEdge.ToString(),
		style);
	}
}