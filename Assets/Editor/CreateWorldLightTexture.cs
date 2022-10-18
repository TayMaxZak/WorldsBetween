using UnityEditor;
using UnityEngine;

public class CreateWorldLightTexture : ScriptableWizard
{
	private static NoiseModifier xNoise;
	private static NoiseModifier yNoise;
	private static NoiseModifier zNoise;

	public enum LightMapStyle
	{
		Flat,
		Point
	}
	public LightMapStyle style = LightMapStyle.Flat;

	[Range(4, 512)]
	public int size = 128;

	[Range(0, 5)]
	public float maxRed = 1;
	[Range(0, 5)]
	public float maxGreen = 1;
	[Range(0, 5)]
	public float maxBlue = 1;

	[Header("")]
	[Range(0, 1)]
	public float randomDither = 0;

	[MenuItem("WorldLighting/Create Lightmap")]
	static void CreateWizard()
	{
		DisplayWizard<CreateWorldLightTexture>("Create Lightmaps", "Create");
	}

	void OnWizardCreate()
	{
		CreateTexture3D(size, style, maxRed, maxGreen, maxBlue, randomDither);
	}

	static void CreateTexture3D(int size, LightMapStyle style, float maxRed, float maxGreen, float maxBlue, float randomDither)
	{
		// Configure the texture
		TextureFormat format = TextureFormat.RGBAHalf;
		TextureWrapMode wrapMode = TextureWrapMode.Clamp;
		FilterMode filterMode = FilterMode.Bilinear;

		// Create the texture and apply the configuration
		Texture3D texture = new Texture3D(size, size, size, format, false);
		texture.wrapMode = wrapMode;
		texture.filterMode = filterMode;

		// Create a 3-dimensional array to store color data
		Color[] colors = new Color[size * size * size];

		// Populate the array so that the x, y, and z values of the texture will map to red, blue, and green colors
		float inverseResolution = 1.0f / (size - 1.0f);

		Vector3 middle = Vector3.one * size / 2;

		for (int z = 0; z < size; z++)
		{
			int zOffset = z * size * size;
			for (int y = 0; y < size; y++)
			{
				int yOffset = y * size;
				for (int x = 0; x < size; x++)
				{
					float r = 0, g = 0, b = 0;

					switch (style)
					{
						case LightMapStyle.Flat:
							{
								r = maxRed;
								g = maxGreen;
								b = maxBlue;
							}
							break;
						case LightMapStyle.Point:
							{
								Vector3 here = new Vector3(x, y, z);
								float dist = Vector3.SqrMagnitude(middle - here) * (2.0f / size) * (2.0f / size);
								dist = 1 - Mathf.Clamp01(dist);

								r = dist * maxRed;
								g = dist * maxGreen;
								b = dist * maxBlue;
							}
							break;
						default:
							break;
					}

					colors[x + yOffset + zOffset] = new Color(Mathf.Max(0, r + Dither(randomDither) * r), Mathf.Max(0, g + Dither(randomDither) * g), Mathf.Max(0, b + Dither(randomDither) * b), 0);
				}
			}
		}

		// Copy the color values to the texture
		texture.SetPixels(colors);

		// Apply the changes to the texture and upload the updated texture to the GPU
		texture.Apply();

		// Save the texture to your Unity Project
		string nameToUse = "LightMap";
		int existing = AssetDatabase.FindAssets(nameToUse).Length;
		if (existing == 0)
			AssetDatabase.CreateAsset(texture, "Assets/CustomAssets/" + nameToUse + ".asset");
		else
			AssetDatabase.CreateAsset(texture, "Assets/CustomAssets/" + nameToUse + " " + existing + ".asset");
	}

	private static float Dither(float mult)
	{
		return SeedlessRandom.NextFloatInRange(-1, 1) * mult;
	}
}