using UnityEditor;
using UnityEngine;

public class SetWorldLightTexture : MonoBehaviour
{
	private static NoiseModifier xNoise;
	private static NoiseModifier yNoise;
	private static NoiseModifier zNoise;

	[MenuItem("WorldLighting/Apply Lightmap")]
	static void ApplyLightTexture()
	{
		if (Selection.activeObject is Texture3D)
		{
			if (Selection.assetGUIDs.Length > 0)
			{
				Texture3D texture = Selection.activeObject as Texture3D;

				Shader.SetGlobalTexture("LightMap", texture);
				Shader.SetGlobalFloat("LightMapScale", texture.width);
			}
		}
	}
}