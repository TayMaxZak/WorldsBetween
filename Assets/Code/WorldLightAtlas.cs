using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[System.Serializable]
[ExecuteInEditMode]
public class WorldLightAtlas : MonoBehaviour
{
	public Texture3D defaultLightmap;
	public Texture3D liveLightmap;

	private int size = 32;

	public Color randomColor = Color.white;

	private void OnEnable()
	{
		if (!Application.isPlaying)
		{
			ApplyTexture(defaultLightmap);
		}
		else
		{
			CreateLightmap();
			ApplyTexture(liveLightmap);
		}
	}

	private void OnDestroy()
	{
		if (liveLightmap)
		{
			Debug.Log("Destroyed");
		}

		ApplyTexture(defaultLightmap);
	}

	private void ApplyTexture(Texture texture)
	{
		Shader.SetGlobalTexture("LightMap", texture);
		Shader.SetGlobalFloat("LightMapScale", texture.width);
	}

	private void CreateLightmap()
	{
		// Create the texture and apply the configuration
		liveLightmap = new Texture3D(size, size, size, TextureFormat.RGBAHalf, false);

		liveLightmap.wrapMode = TextureWrapMode.Clamp;
		liveLightmap.filterMode = FilterMode.Bilinear;

		Color[] colors = new Color[size * size * size];
		for (int z = 0; z < size; z++)
		{
			int zOffset = z * size * size;
			for (int y = 0; y < size; y++)
			{
				int yOffset = y * size;
				for (int x = 0; x < size; x++)
				{
					RandomColor();

					colors[x + yOffset + zOffset] = randomColor;
				}
			}
		}

		// Copy the color values to the texture
		liveLightmap.SetPixels(colors);

		// Apply the changes to the texture and upload the updated texture to the GPU
		liveLightmap.Apply();
	}

	private void WriteToLightmap(Vector3Int pos, Color value)
	{
		for (int j = 0; j < liveLightmap.width; j++)
		{
			liveLightmap.SetPixel(pos.x, pos.y, pos.z, value);
		}
	}

	private void Update()
	{
		if (!Application.isPlaying)
			return;

		if (SeedlessRandom.NextFloat() < 0.05f)
		{
			RandomColor();
		}

		int count = SeedlessRandom.NextFloat() < 0.2f ? SeedlessRandom.NextIntInRange(size, size * 2) : 0;
		for (int i = 0; i < count; i++)
			WriteToLightmap(
				new Vector3Int(SeedlessRandom.NextIntInRange(0, size),
				SeedlessRandom.NextIntInRange(0, size),
				SeedlessRandom.NextIntInRange(0, size)),
				randomColor);

		if (count > 0)
			liveLightmap.Apply();
	}

	private void RandomColor()
	{
		float mult = 1.5f;

		randomColor.r = mult * SeedlessRandom.NextFloat();
		randomColor.r *= randomColor.r;
		randomColor.r *= SeedlessRandom.NextFloat();

		randomColor.g = SeedlessRandom.NextFloat() * SeedlessRandom.NextFloat() * SeedlessRandom.NextFloat();

		randomColor.b = 1;

		randomColor.a = 0;
	}
}
