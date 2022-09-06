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
	private int[] airCountArr;

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


		// One pixel for every 2 blocks in each dimension (per 8 blocks total)
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

		airCountArr = new int[ambSize * ambSize * ambSize];
		for (int z = 0; z < ambSize; z++)
		{
			for (int y = 0; y < ambSize; y++)
			{
				for (int x = 0; x < ambSize; x++)
				{
					airCountArr[IndexFromPos(ambSize, x, y, z)] = 1024;
				}
			}
		}
	}

	private void UpdateAmbientTex()
	{
		// Copy the color values to the texture
		ambientLightTex.SetPixels(ambientLightArr);

		// Apply the changes to the texture and upload the updated texture to the GPU
		ambientLightTex.Apply();
	}

	public void WriteToLightmap(Vector3Int pos, Color newValue, bool airLight, bool additive = false)
	{
		if (directLightTex == null)
			return;

		if (!World.Contains(pos))
			return;

		pos = WorldToTex(pos);

		// Direct light change
		int dirIndex = IndexFromPos(dirSize, pos.x / directScale, pos.y / directScale, pos.z / directScale);

		directChanges++;

		Color oldValue = directLightArr[dirIndex];

		if (additive)
			newValue = oldValue + newValue;

		directLightArr[dirIndex] = newValue;


		if (!airLight)
			return;

		// Ambient light change
		Vector3Int ambPos = pos / ambientScale;
		int ambIndex = IndexFromPos(fullSize / ambientScale, ambPos.x, ambPos.y, ambPos.z);

		// To avoid losing color information by using a small number, mult the color sum later in shader as needed 
		float ambChangeStrength = (directScale * directScale * directScale) * ((float)ambientScale * ambientScale) / airCountArr[ambIndex];

		ambientChanges++;

		Color oldAmbValue = ambientLightArr[ambIndex];
		Color newAmbValue = oldAmbValue + (newValue - oldValue) * ambChangeStrength;
		ambientLightArr[ambIndex] = newAmbValue;
	}

	public void SetAirCount(Vector3Int pos, int count)
	{
		//if (halfScaleAmbient)
		//{
		//	for (int x = 0; x < 2; x++)
		//	{
		//		for (int y = 0; y < 2; y++)
		//		{
		//			for (int z = 0; z < 2; z++)
		//			{
		//				Vector3Int posA = WorldToTex(pos - new Vector3Int(x, y, z) * 8);
		//				Vector3Int ambPos = new Vector3Int(posA.x / ambientScale, posA.y / ambientScale, posA.z / ambientScale);

		//				int ambIndex = IndexFromPos(fullSize / ambientScale, ambPos.x, ambPos.y, ambPos.z);
		//				airCountArr[ambIndex] = Mathf.Max(1, count);
		//			}
		//		}
		//	}
		//}
		//else
		//{
		//	Vector3Int posA = WorldToTex(pos);
		//	Vector3Int ambPos = new Vector3Int(posA.x / ambientScale, posA.y / ambientScale, posA.z / ambientScale);

		//	int ambIndex = IndexFromPos(fullSize / ambientScale, ambPos.x, ambPos.y, ambPos.z);
		//	airCountArr[ambIndex] = Mathf.Max(1, count);
		//}
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

		// No need to clear air counts
	}

	private int IndexFromPos(int size, int x, int y, int z)
	{
		int zOffset = z * size * size;
		int yOffset = y * size;
		int xOffset = x;

		return xOffset + yOffset + zOffset;
	}

	//private void Update()
	//{
	//	if (!Application.isPlaying || !GameManager.Instance || simpleMode)
	//		return;

	//	recentApplyTimer.Increment(Time.deltaTime);
	//	if (!recentApplyTimer.Expired())
	//		return;

	//	// Apply ambient changes as needed (cheaper to apply)
	//	if (ambientChanges >= targAmbientChanges)
	//	{
	//		UpdateAmbientTex();
	//		ambientChanges = 0;

	//		recentApplyTimer.Reset();
	//	}

	//	// Apply direct changes as needed (less frequently than ambient)
	//	if (directChanges >= targDirectChanges)
	//	{
	//		UpdateDirectTex();
	//		directChanges = 0;

	//		cleanupTimer.Reset();
	//		recentApplyTimer.Reset();
	//	}

	//	// Leftover changes, eventually apply them during down time
	//	if (directChanges > 0)
	//		cleanupTimer.Increment(Time.deltaTime);

	//	if (cleanupTimer.Expired())
	//	{
	//		Debug.Log("Leftover changes: " + directChanges + " direct, " + ambientChanges + " ambient");
	//		cleanupTimer.Reset();

	//		UpdateDirectTex();
	//		directChanges = 0;

	//		UpdateAmbientTex();
	//		ambientChanges = 0;
	//	}
	//}

	[ContextMenu("Apply Changes")]
	public async void ApplyChanges()
	{
		if (directChanges == 0 && ambientChanges == 0)
			return;

		//await ApplyChunkLights();

		Debug.Log("Applied light atlas changes: " + directChanges + " direct, " + ambientChanges + " ambient"); // Why does this say 0 0 sometimes

		UpdateDirectTex();
		directChanges = 0;

		UpdateAmbientTex();
		ambientChanges = 0;
	}

	private async Task ApplyChunkLights()
	{
		int baseLightRange = 10;

		int chunks = 0;
		int chunkLights = 0;
		foreach (var pl in World.GetAllLights())
		{
			foreach (BlockLight light in pl.Value)
			{
				// TODO: Why is it ever null?
				if (light == null)
					continue;

				int lightRange = (int)(baseLightRange * light.spread);

				for (int x = -lightRange; x <= lightRange; x++)
				{
					for (int y = -lightRange * 2; y <= lightRange * 2; y++)
					{
						for (int z = -lightRange; z <= lightRange; z++)
						{
							if ((x - 0.5f) * (x - 0.5f) + ((y - 0.5f) * (y - 0.5f)) / 4f + (z - 0.5f) * (z - 0.5f) > lightRange * lightRange)
								continue;

							Vector3Int newPos = new Vector3Int(
								Mathf.FloorToInt(light.blockPos.x + x),
								Mathf.FloorToInt(light.blockPos.y + y),
								Mathf.FloorToInt(light.blockPos.z + z)
							);

							float dist = Mathf.Sqrt((x - 0.5f) * (x - 0.5f) + ((y - 0.5f) * (y - 0.5f)) / 4f + (z - 0.5f) * (z - 0.5f));
							float falloff = Mathf.Clamp01(1 - dist * (1f / lightRange));
							falloff = falloff * falloff;

							Vector3Int shadowPos = dist > 1 ? new Vector3Int(
								Mathf.FloorToInt(newPos.x - x / dist),
								Mathf.FloorToInt(newPos.y - y / dist),
								Mathf.FloorToInt(newPos.z - z / dist)
							) : light.blockPos;

							float noise = Mathf.Lerp(1, SeedlessRandom.NextFloat(), light.noise);
							if (!World.GetBlock(shadowPos).IsOpaque())
								WriteToLightmap(newPos, light.GetLightColor(falloff) * light.brightness * falloff * noise, !World.GetBlock(newPos).IsFilled(), true);
						}
					}
				} // x y z

				//for (int x = -lightRange * 2; x <= lightRange * 2; x += SeedlessRandom.NextIntInRange(1, 2 + 1))
				//{
				//	for (int y = -lightRange * 2; y <= lightRange * 2; y += SeedlessRandom.NextIntInRange(1, 2 + 1))
				//	{
				//		for (int z = -lightRange * 2; z <= lightRange * 2; z += SeedlessRandom.NextIntInRange(1, 2 + 1))
				//		{
				//			if ((x * x) + (y * y) + (z * z) > (lightRange * 2) * (lightRange * 2))
				//				continue;

				//			Vector3Int newPos = new Vector3Int(
				//				Mathf.FloorToInt(light.pos.x + x),
				//				Mathf.FloorToInt(light.pos.y + y),
				//				Mathf.FloorToInt(light.pos.z + z)
				//			);

				//			float dist = Mathf.Sqrt((x * x) + (y * y) + (z * z));
				//			float falloff = 0.5f * Mathf.Max(0, 1 - dist * (1f / lightRange));
				//			WriteToLightmap(newPos, light.lightColor * falloff, World.GetBlock(newPos).IsFilled(), true);
				//		}
				//	}
				//} // x y z

				chunkLights++;
			}

			chunks++;

			if (chunks % 32 == 0)
				await Task.Delay(1);
		}

		Debug.Log("Chunk lights: " + chunkLights);
	}

	private Vector3Int WorldToTex(Vector3Int wrld)
	{
		return wrld + Vector3Int.one * (fullSize / 2);
	}
}
