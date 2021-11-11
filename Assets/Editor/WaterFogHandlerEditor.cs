using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class WaterFogHandlerEditor : MonoBehaviour
{
	private void Update()
	{
		Camera camera;

		if (Application.isPlaying)
		{
			foreach (SceneView sc in SceneView.sceneViews)
			{
				camera = sc.camera;


			}
		}
	}
}
