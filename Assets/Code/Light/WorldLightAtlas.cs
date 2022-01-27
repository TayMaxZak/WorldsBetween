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

	public Texture3D directLightTex;
	private Color[] directLightArr;
	public Texture3D ambientLightTex;
	private Color[] ambientLightArr;

	private int fullSize;
	private int ambientScale = 16;

	private Timer cleanupTimer = new Timer(15f);

	private static int directChanges = 0;
	private static int targdirectChanges = 50000;

	private static int ambientChanges = 0;
	private static int targambientChanges = 5000;

	private void OnEnable()
	{
		if (!Application.isPlaying)
		{
			SetShaderReferences(defaultLightmap, defaultLightmap2);
		}
		else
		{
			fullSize = World.GetChunkSize() * (1 + World.Generator.GetGenRange() * 2);

			CreateDirectLightmap();
			CreateAmbientLightmap();
			SetShaderReferences(directLightTex, ambientLightTex);

			Instance = this;
		}
	}

	private void OnDestroy()
	{
		if (directLightTex)
		{
			Debug.Log("Destroyed");
		}

		SetShaderReferences(defaultLightmap, defaultLightmap2);
	}

	private void SetShaderReferences(Texture texture, Texture texture2)
	{
		Shader.SetGlobalTexture("LightMap", texture);
		Shader.SetGlobalTexture("LightMap2", texture2);
		Shader.SetGlobalFloat("LightMapScale", texture.width);
	}

	private void CreateDirectLightmap()
	{
		// Create texture and apply configuration. Half is sufficient for most lighting
		directLightTex = new Texture3D(fullSize, fullSize, fullSize, TextureFormat.RGBAHalf, false);

		directLightTex.wrapMode = TextureWrapMode.Clamp;
		directLightTex.filterMode = FilterMode.Bilinear;

		directLightArr = new Color[fullSize * fullSize * fullSize];
		for (int z = 0; z < fullSize; z++)
		{
			for (int y = 0; y < fullSize; y++)
			{
				for (int x = 0; x < fullSize; x++)
				{
					directLightArr[IndexFromPos(fullSize, x, y, z)] = Color.black;
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
		// One pixel per ambient chunk
		int ambSize = fullSize / ambientScale;

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

	public void WriteToLightmap(LightMapSpace texSpace, Vector3Int pos, Color value, bool airLight)
	{
		if (directLightTex == null)
			return;

		// Convert position
		if (texSpace == LightMapSpace.WorldSpace)
			pos = WorldToTex(pos);


		// Direct light change
		int dirIndex = IndexFromPos(fullSize, pos.x, pos.y, pos.z);
		if (!InBounds(fullSize, dirIndex))
			return;
		directChanges++;

		Color oldValue = directLightArr[dirIndex];

		directLightArr[dirIndex] = value;


		// To avoid losing color information by using a small number, mult the color sum later in shader as needed 
		// Surface light should count for 16 times as much because of limited surface area compared to volume
		float ambChangeStrength = airLight ? (1 / 4f) : 4f;

		// Ambient light change
		Vector3Int ambPos = new Vector3Int(pos.x / ambientScale, pos.y / ambientScale, pos.z / ambientScale);
		int ambIndex = IndexFromPos(fullSize / ambientScale, ambPos.x, ambPos.y, ambPos.z);

		if (!InBounds(fullSize / ambientScale, ambIndex))
			return;
		ambientChanges++;

		Color oldAmbValue = ambientLightArr[ambIndex];
		Color newAmbValue = oldAmbValue + (value - oldValue) * ambChangeStrength;

		ambientLightArr[ambIndex] = newAmbValue;
	}

	private int IndexFromPos(int size, int x, int y, int z)
	{
		int zOffset = z * size * size;
		int yOffset = y * size;
		int xOffset = x;

		return xOffset + yOffset + zOffset;
	}

	private bool InBounds(int size, int index)
	{
		return index > 0 && index < size * size * size;
	}

	private void Update()
	{
		if (!Application.isPlaying)
			return;

		// Apply ambient changes as needed (cheaper to apply)
		if (ambientChanges >= targambientChanges)
		{
			UpdateAmbientTex();
			ambientChanges = 0;
		}

		// Apply direct changes as needed (less frequently than ambient)
		if (directChanges >= targdirectChanges)
		{
			UpdateDirectTex();
			directChanges = 0;

			cleanupTimer.Reset();
		}

		// Leftover changes, eventually apply them during down time
		if (directChanges > 0)
			cleanupTimer.Increment(Time.deltaTime);

		if (cleanupTimer.Expired())
		{
			Debug.Log("Leftover changes: " + directChanges + " direct, " + ambientChanges + " ambient");
			cleanupTimer.Reset();

			UpdateDirectTex();
			directChanges = 0;

			UpdateAmbientTex();
			ambientChanges = 0;
		}
	}

	private Vector3Int WorldToTex(Vector3Int wrld)
	{
		return wrld + Vector3Int.one * (fullSize / 2 - World.GetChunkSize() / 2);
	}
}
