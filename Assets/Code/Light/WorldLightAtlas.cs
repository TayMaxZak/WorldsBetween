using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[System.Serializable]
[ExecuteInEditMode]
public class WorldLightAtlas : MonoBehaviour
{
	public static WorldLightAtlas Instance;

	public bool simpleMode = false;

	public Texture3D defaultLightmap;
	public Texture3D defaultLightmap2;

	public Texture3D directLightTex;
	private Color[] directLightArr;
	public Texture3D ambientLightTex;
	private Color[] ambientLightArr;

	private int fullSize;

	private int dirSize;
	public int directScale = 1;

	private int ambSize;
	public bool halfScaleAmbient = false;
	private int ambientScale = 16;

	private static int directChanges = 0;

	private static int ambientChanges = 0;

	private void OnEnable()
	{
		bool partialInit = !Application.isPlaying || simpleMode;

		if (partialInit)
			fullSize = defaultLightmap.width;
		else
			fullSize = World.GetWorldSize();


		// One pixel for every 1 blocks in each dimension (per 1 blocks total)
		dirSize = fullSize / directScale;

		ambientScale = halfScaleAmbient ? 8 : 16;
		// One pixel per chunk (per 4096 blocks total)
		ambSize = fullSize / ambientScale;


		if (partialInit)
		{
			SetShaderReferences(defaultLightmap, defaultLightmap2);
		}
		else
		{
			CreateDirectLightmap();
			CreateAmbientLightmap();

			SetShaderReferences(directLightTex, ambientLightTex);

			Instance = this;
		}
	}

	private void SetShaderReferences(Texture texture, Texture texture2)
	{
		Shader.SetGlobalTexture("LightMap", texture);
		Shader.SetGlobalTexture("LightMap2", texture2);
		Shader.SetGlobalFloat("LightMapScale", fullSize);
	}

	private void CreateDirectLightmap()
	{
		// Create texture and apply configuration. RGBAHalf is sufficient for most lighting
		directLightTex = new Texture3D(dirSize, dirSize, dirSize, TextureFormat.RGBAHalf, false);

		directLightTex.wrapMode = TextureWrapMode.Clamp;
		directLightTex.filterMode = FilterMode.Bilinear;

		directLightArr = new Color[dirSize * dirSize * dirSize];
		for (int z = 0; z < dirSize; z++)
		{
			for (int y = 0; y < dirSize; y++)
			{
				for (int x = 0; x < dirSize; x++)
				{
					directLightArr[IndexFromPos(dirSize, x, y, z)] = Color.black;
				}
			}
		}

		UpdateDirectTex();
	}

	private void UpdateDirectTex()
	{
		// Copy the color values to the texture
		directLightTex.SetPixels(directLightArr);

		// Apply the changes to the texture and upload the updated texture to the GPU
		directLightTex.Apply();
	}

	private void CreateAmbientLightmap()
	{
		// Create texture and apply configuration. Use float for higher fidelity in ambient light
		ambientLightTex = new Texture3D(ambSize, ambSize, ambSize, TextureFormat.RGBAFloat, false);

		ambientLightTex.wrapMode = TextureWrapMode.Clamp;
		ambientLightTex.filterMode = FilterMode.Bilinear;

		ambientLightArr = new Color[ambSize * ambSize * ambSize];
		for (int z = 0; z < ambSize; z++)
		{
			for (int y = 0; y < ambSize; y++)
			{
				for (int x = 0; x < ambSize; x++)
				{
					ambientLightArr[IndexFromPos(ambSize, x, y, z)] = Color.black;
				}
			}
		}

		UpdateAmbientTex();
	}

	private void UpdateAmbientTex()
	{
		// Copy the color values to the texture
		ambientLightTex.SetPixels(ambientLightArr);

		// Apply the changes to the texture and upload the updated texture to the GPU
		ambientLightTex.Apply();
	}

	public void ClearAtlas(bool updateTex)
	{
		for (int z = 0; z < dirSize; z++)
		{
			for (int y = 0; y < dirSize; y++)
			{
				for (int x = 0; x < dirSize; x++)
				{
					directLightArr[IndexFromPos(dirSize, x, y, z)] = Color.black;
				}
			}
		}
		if (updateTex)
			UpdateDirectTex();

		for (int z = 0; z < ambSize; z++)
		{
			for (int y = 0; y < ambSize; y++)
			{
				for (int x = 0; x < ambSize; x++)
				{
					ambientLightArr[IndexFromPos(ambSize, x, y, z)] = Color.black;
				}
			}
		}
		if (updateTex)
			UpdateAmbientTex();
	}

	private int IndexFromPos(int size, int x, int y, int z)
	{
		int zOffset = z * size * size;
		int yOffset = y * size;
		int xOffset = x;

		return xOffset + yOffset + zOffset;
	}

	public void AggregateChunkLighting()
	{
		int chunkSize = World.GetChunkSize();

		foreach (var chunk in World.GetAllChunks())
		{
			Vector3Int chunkPos = new Vector3Int(chunk.Key.x, chunk.Key.y, chunk.Key.z);

			bool partiallyOutOfWorld = false;

			for (int x = 0; x < chunkSize; x++)
			{
				for (int y = 0; y < chunkSize; y++)
				{
					for (int z = 0; z < chunkSize; z++)
					{
						Vector3Int pos = WorldToTex(new Vector3Int(chunkPos.x + x, chunkPos.y + y, chunkPos.z + z)) / directScale;

						if (!World.LightAtlasContains(pos))
						{
							partiallyOutOfWorld = true;
							continue;
						}

						int index = IndexFromPos(dirSize, pos.x, pos.y, pos.z);
						directLightArr[index] = chunk.Value.GetLighting(x, y, z);
						directChanges++;
					}
				}
			}

			if (partiallyOutOfWorld)
				continue;

			Vector3Int ambPos = WorldToTex(chunkPos) / ambientScale;
			int ambIndex = IndexFromPos(ambSize, ambPos.x, ambPos.y, ambPos.z);
			ambientLightArr[ambIndex] = chunk.Value.GetAverageLighting();
			ambientChanges++;
		}
	}

	[ContextMenu("Apply Changes")]
	public void UpdateLightTextures()
	{
		if (directChanges == 0 && ambientChanges == 0)
			return;

		Debug.Log("Applied light atlas changes: " + directChanges + " direct, " + ambientChanges + " ambient");

		UpdateDirectTex();
		directChanges = 0;

		UpdateAmbientTex();
		ambientChanges = 0;
	}

	private Vector3Int WorldToTex(Vector3Int wrld)
	{
		return wrld + Vector3Int.one * (fullSize / 2);
	}
}
