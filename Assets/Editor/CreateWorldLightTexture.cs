using UnityEditor;
using UnityEngine;

public class CreateWorldLightTexture : MonoBehaviour
{
	private static NoiseModifier brightnessNoise;
	private static NoiseModifier temperatureNoise;

	[MenuItem("WorldLighting/Create Test 3D Texture")]
	static void CreateTexture3D()
	{
		brightnessNoise = new NoiseModifier()
		{
			scale = Vector3.one * 0.06f,
			offset = 2444.0424f
		};
		brightnessNoise.Init();

		temperatureNoise = new NoiseModifier()
		{
			scale = Vector3.one * 0.025f,
			offset = 2444.0424f
		};
		temperatureNoise.Init();

		// Configure the texture
		int size = 128;
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
					float brightness = Mathf.Abs(brightnessNoise.StrengthAt(x, y, z));
					float temperature = Mathf.Abs(temperatureNoise.StrengthAt(x, y, z));

					colors[x + yOffset + zOffset] = new Color(brightness, temperature, 0);
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