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
		int size = 16;
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
					float i = xNoise.StrengthAt(x, y, z);
					float j = yNoise.StrengthAt(x, y, z);
					float k = zNoise.StrengthAt(x, y, z);

					//colors[x + yOffset + zOffset] = new Color(1.33f * Mathf.Pow(i, 5), 1.33f * Mathf.Pow(j, 5), 1.33f * Mathf.Pow(k, 5));
					colors[x + yOffset + zOffset] = new Color(0.5f, 0.5f, 0.5f);
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