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
	public Texture3D liveLightmap;

	private int size = 256;

	public Color randomColor = Color.white;

	private static int changeCount = 0;

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

			Instance = this;
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

		liveLightmap.wrapMode = TextureWrapMode.Repeat;
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
					RandomizeColor();

					colors[x + yOffset + zOffset] = Color.black;
				}
			}
		}

		// Copy the color values to the texture
		liveLightmap.SetPixels(colors);

		// Apply the changes to the texture and upload the updated texture to the GPU
		liveLightmap.Apply();
	}

	public void WriteToLightmap(LightMapSpace texSpace, Vector3Int pos, Color value)
	{
		if (texSpace == LightMapSpace.WorldSpace)
			pos = WorldToTex(pos);

		if (pos.x < 0 || pos.y < 0 || pos.z < 0 || pos.x >= size || pos.y >= size || pos.z >= size)
			return;

		liveLightmap.SetPixel(pos.x, pos.y, pos.z, value);

		changeCount++;
	}

	private void Update()
	{
		if (!Application.isPlaying)
			return;

		if (changeCount > 0)
			liveLightmap.Apply();
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

	public static void CalculateShadowsFor(ChunkBitArray vertexBit, ChunkBitArray shadowBit)
	{
		int chunkSize = World.GetChunkSize();

		// Calculate shadows
		for (int x = 0; x < chunkSize; x++)
		{
			for (int y = 0; y < chunkSize; y++)
			{
				for (int z = 0; z < chunkSize; z++)
				{
					shadowBit.Set(!vertexBit.Get(x, y, z), x, y, z);
				}
			}
		}

		//// First time calculating for this block
		//if (shadowBit.needsCalc)
		//	shadowBit.needsCalc = shadowBit.needsCalc;
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
