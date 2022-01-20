using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[System.Serializable]
[ExecuteInEditMode]
public class WorldLightAtlas : MonoBehaviour
{
	public enum LightMapSpace
	{
		TexSpace,
		WorldSpace
	}

	public static WorldLightAtlas Instance;

	public Texture3D defaultLightmap;
	public Texture3D defaultLightmap2;

	public Texture3D directLightmap;
	public Texture3D ambientLightmap;

	private int size = 256;

	public Color randomColor = Color.white;

	private static int changeCount = 0;

	private void OnEnable()
	{
		if (!Application.isPlaying)
		{
			ApplyTexture(defaultLightmap, defaultLightmap2);
		}
		else
		{
			size = World.GetChunkSize() * (1 + World.Generator.GetGenRange() * 2);

			CreateDirectLightmap();
			CreateAmbientLightmap();
			ApplyTexture(directLightmap, ambientLightmap);

			Instance = this;
		}
	}

	private void OnDestroy()
	{
		if (directLightmap)
		{
			Debug.Log("Destroyed");
		}

		ApplyTexture(defaultLightmap, defaultLightmap2);
	}

	private void ApplyTexture(Texture texture, Texture texture2)
	{
		Shader.SetGlobalTexture("LightMap", texture);
		Shader.SetGlobalTexture("LightMap2", texture2);
		Shader.SetGlobalFloat("LightMapScale", texture.width);
		Shader.SetGlobalFloat("LightMapScale2", texture2.width);
	}

	private void CreateDirectLightmap()
	{
		// Create the texture and apply the configuration
		directLightmap = new Texture3D(size, size, size, TextureFormat.RGBAHalf, false);

		directLightmap.wrapMode = TextureWrapMode.Repeat;
		directLightmap.filterMode = FilterMode.Bilinear;

		Color[] colors = new Color[size * size * size];
		for (int z = 0; z < size; z++)
		{
			int zOffset = z * size * size;
			for (int y = 0; y < size; y++)
			{
				int yOffset = y * size;
				for (int x = 0; x < size; x++)
				{
					colors[x + yOffset + zOffset] = Color.black;
				}
			}
		}

		// Copy the color values to the texture
		directLightmap.SetPixels(colors);

		// Apply the changes to the texture and upload the updated texture to the GPU
		directLightmap.Apply();
	}

	private void CreateAmbientLightmap()
	{
		// One pixel per chunk
		int size = this.size / 16;

		// Create the texture and apply the configuration
		ambientLightmap = new Texture3D(size, size, size, TextureFormat.RGBAHalf, false);

		ambientLightmap.wrapMode = TextureWrapMode.Repeat;
		ambientLightmap.filterMode = FilterMode.Bilinear;

		Color[] colors = new Color[size * size * size];
		for (int z = 0; z < size; z++)
		{
			int zOffset = z * size * size;
			for (int y = 0; y < size; y++)
			{
				int yOffset = y * size;
				for (int x = 0; x < size; x++)
				{
					colors[x + yOffset + zOffset] = Color.black;
				}
			}
		}

		// Copy the color values to the texture
		ambientLightmap.SetPixels(colors);

		// Apply the changes to the texture and upload the updated texture to the GPU
		ambientLightmap.Apply();
	}

	public void WriteToLightmap(LightMapSpace texSpace, Vector3Int pos, Color value)
	{
		if (directLightmap == null)
			return;

		if (texSpace == LightMapSpace.WorldSpace)
			pos = WorldToTex(pos);

		if (pos.x < 0 || pos.y < 0 || pos.z < 0 || pos.x >= size || pos.y >= size || pos.z >= size)
			return;

		Color oldValue = directLightmap.GetPixel(pos.x, pos.y, pos.z);
		directLightmap.SetPixel(pos.x, pos.y, pos.z, value);

		int chunkSize = World.GetChunkSize();
		float changeFraction = 1f / (chunkSize);

		Color oldAmbValue = ambientLightmap.GetPixel(pos.x, pos.y, pos.z);
		Color newAmbValue = oldAmbValue + (value - oldValue) * changeFraction;
		ambientLightmap.SetPixel(pos.x / chunkSize, pos.y / chunkSize, pos.z / chunkSize, newAmbValue);

		changeCount++;
	}

	private void Update()
	{
		if (!Application.isPlaying)
			return;

		if (changeCount > 0)
		{
			directLightmap.Apply();
			ambientLightmap.Apply();
		}
		changeCount = 0;
	}

	private Vector3Int TexToWorld(Vector3Int tex)
	{
		return 2 * tex - Vector3Int.one * size;
	}

	private Vector3Int WorldToTex(Vector3Int wrld)
	{
		return wrld + (Vector3Int.one * size) / 2;
	}

	private void RandomizeColor()
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
