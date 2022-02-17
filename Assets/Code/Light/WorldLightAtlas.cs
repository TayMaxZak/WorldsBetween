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
	public int directScale = 2;

	private int ambSize;
	public int ambientScale = 16;

	private static int directChanges = 0;

	private static int ambientChanges = 0;

	private void OnEnable()
	{
		bool partialInit = !Application.isPlaying || simpleMode;

		if (partialInit)
			fullSize = defaultLightmap.width;
		else
			fullSize = World.GetChunkSize() * (1 + World.WorldBuilder.GetGenRange() * 2);


		// One pixel for every 2 blocks in each dimension (per 8 blocks total)
		dirSize = fullSize / directScale;
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

	//private void OnDestroy()
	//{
	//	SetShaderReferences(defaultLightmap, defaultLightmap2);
	//}

	private void SetShaderReferences(Texture texture, Texture texture2)
	{
		Shader.SetGlobalTexture("LightMap", texture);
		Shader.SetGlobalTexture("LightMap2", texture2);
		Shader.SetGlobalFloat("LightMapScale", fullSize);
	}

	private void CreateDirectLightmap()
	{
		// TODO:
		// Up close - 1:1 pixel to block for 125 chunks around origin (center + 2 in each direction). 1:2 pixel to block everywhere else
		// 1 + 2 + 4(0.5)

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

	public void WriteToLightmap(Vector3Int pos, Color newValue, bool airLight)
	{
		if (directLightTex == null)
			return;

		Vector3Int posD = WorldToTex(pos);
		Vector3Int posA = WorldToTex(pos);

		// Direct light change
		int dirIndex = IndexFromPos(dirSize, posD.x / directScale, posD.y / directScale, posD.z / directScale);
		if (!InBounds(dirSize, dirIndex))
			return;
		directChanges++;

		Color oldValue = directLightArr[dirIndex];

		directLightArr[dirIndex] = newValue;


		if (!airLight)
			return;

		// Ambient light change
		Vector3Int ambPos = new Vector3Int(posA.x / ambientScale, posA.y / ambientScale, posA.z / ambientScale);
		int ambIndex = IndexFromPos(fullSize / ambientScale, ambPos.x, ambPos.y, ambPos.z);

		// To avoid losing color information by using a small number, mult the color sum later in shader as needed 
		float ambChangeStrength = (directScale * directScale * directScale) * 256f / airCountArr[ambIndex];

		if (!InBounds(fullSize / ambientScale, ambIndex))
			return;
		ambientChanges++;

		Color oldAmbValue = ambientLightArr[ambIndex];
		Color newAmbValue = oldAmbValue + (newValue - oldValue) * ambChangeStrength;

		ambientLightArr[ambIndex] = newAmbValue;
	}

	public void SetAirCount(Vector3Int pos, int count)
	{
		Vector3Int posA = WorldToTex(pos);
		Vector3Int ambPos = new Vector3Int(posA.x / ambientScale, posA.y / ambientScale, posA.z / ambientScale);
		int ambIndex = IndexFromPos(fullSize / ambientScale, ambPos.x, ambPos.y, ambPos.z);
		if (InBounds(fullSize / ambientScale, ambIndex))
			airCountArr[ambIndex] = Mathf.Max(1, count);
		else
			Debug.Log("Out of bounds: " + ambIndex + ", in " + pos + ", tex " + posA);
	}

	public void ClearAtlas()
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
		UpdateAmbientTex();
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
		return index >= 0 && index < size * size * size;
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

	public void ApplyChanges()
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
		return wrld + Vector3Int.one * (fullSize / 2 - World.GetChunkSize() / 2);
	}
}
