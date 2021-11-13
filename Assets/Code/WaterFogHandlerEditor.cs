using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class WaterFogHandlerEditor : MonoBehaviour
{
	[SerializeField]
	private WaterFogHandler testHandler;

	[SerializeField]
	private World testWorld;

	private void OnRenderObject()
	{
		if (!Application.isPlaying)
		{
			bool inWater = false;

			// If ANY scene is underwater, set underwater settings
			foreach (SceneView sc in SceneView.sceneViews)
			{
				if (WaterFogHandler.InWater(sc.camera, World.GetWaterHeight(testWorld)))
				{
					inWater = true;
					break;
				}
			}

			WaterFogHandler.SetSettings(testHandler, inWater);
		}
	}
}
