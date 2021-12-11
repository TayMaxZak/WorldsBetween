using UnityEditor;
using UnityEngine;

public class CreateWorldLightTexture : MonoBehaviour
{
	private static NoiseModifier xNoise;
	private static NoiseModifier yNoise;
	private static NoiseModifier zNoise;

	[MenuItem("WorldLighting/Create Test 3D Texture")]
	static void CreateTexture3D()
	{
		xNoise = new NoiseModifier()
		{
			scale = Vector3.one * 0.06f,
			offset = 2444.0424f,
			strength = 1
		};
		xNoise.Init();

		yNoise = new NoiseModifier()
		{
			scale = Vector3.one * 0.06f,
			offset = 2144.0424f,
			strength = 1
		};
		yNoise.Init();

		zNoise = new NoiseModifier()
		{
			scale = Vector3.one * 0.06f,
			offset = 28744.0424f,
			strength = 1
		};
		zNoise.Init();

		// Configure the texture
		int size = 32;
		TextureFormat format = TextureFormat.RGBAHalf;
		TextureWrapMode wrapMode = TextureWrapMode.Clamp;

		// Create the texture and apply the configuration
		Texture3D texture = new Texture3D(size, size, size, format, false);
		texture.wrapMode = wrapMode;

		// Create a 3-dimensional array to store color data
		Color[] colors = new Color[size * size * size];

		// Populate the array so that the x, y, and z values of the texture will map to red, blue, and green colors
		float inverseResolution = 1.0f / (size - 1.0f);
		for (int z = 0; z < size; z++)
		{
			int zOffset = z * size * size;
			for (int y = 0; y < size; y++)
			{
				int yOffset = y * size;
				for (int x = 0; x < size; x++)
				{
					//float i = xNoise.StrengthAt(x, y, z);
					//float j = yNoise.StrengthAt(x, y, z);
					//float k = zNoise.StrengthAt(x, y, z);

					//float mult = 15;
					//colors[x + yOffset + zOffset] = new Color(mult * Mathf.Pow(i, 12), mult * Mathf.Pow(j, 12), mult * Mathf.Pow(k, 12));
					//colors[x + yOffset + zOffset] = new Color(0.5f, 0.5f, 0.5f);

					float inv = y * inverseResolution;
					colors[x + yOffset + zOffset] = new Color(0, 2, 1);
				}
			}
		}

		// Copy the color values to the texture
		texture.SetPixels(colors);

		// Apply the changes to the texture and upload the updated texture to the GPU
		texture.Apply();

		// Save the texture to your Unity Project
		int existing = AssetDatabase.FindAssets("TestLightMap").Length;
		if (existing == 0)
			AssetDatabase.CreateAsset(texture, "Assets/CustomAssets/WorldLightAtlas/TestLightMap.asset");
		else
			AssetDatabase.CreateAsset(texture, "Assets/CustomAssets/WorldLightAtlas/TestLightMap " + existing + ".asset");
	}
}