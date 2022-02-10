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

	private int fullSize;

	private int dirSize;
	//private int hdRange = 5;
	//private int directScaleHQ = 1;
	//private int directScaleLQ = 2;

	private int ambientScale = 16;

	private Timer cleanupTimer = new Timer(15f);

	private Timer recentApplyTimer = new Timer(1f);

	private static int directChanges = 0;
	private static int targDirectChanges = 50000;

	private static int ambientChanges = 0;
	private static int targAmbientChanges = 5000;

	private void OnEnable()
	{
		if (!Application.isPlaying || simpleMode)
		{
			fullSize = defaultLightmap.width;
			dirSize = fullSize;

			SetShaderReferences(defaultLightmap, defaultLightmap2);
		}
		else
		{
			fullSize = World.GetChunkSize() * (1 + World.Generator.GetGenRange() * 2);
			dirSize = fullSize;
			//dirSize = World.GetChunkSize() * (1 + 
			//	(Mathf.Min(hdRange, World.Generator.GetGenRange()) * 2 / directScaleHQ) + 
			//	(Mathf.Max(0, World.Generator.GetGenRange() - hdRange) * 2 / directScaleLQ));

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
		Shader.SetGlobalFloat("LightMapScale", fullSize);
	}

	private void CreateDirectLightmap()
	{
		// TODO:
		// Up close - 1:1 pixel to block for 125 chunks around origin (center + 2 in each direction). 1:2 pixel to block everywhere else
		// 1 + 2 + 4(0.5)
		Debug.Log("dir size " + dirSize);

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

	public void WriteToLightmap(LightMapSpace texSpace, Vector3Int pos, Color newValue, bool airLight)
	{
		if (directLightTex == null)
			return;

		Vector3Int posD = WorldToTex(pos);
		Vector3Int posA = WorldToTex(pos);

		// Direct light change
		int dirIndex = IndexFromPos(dirSize, posD.x, posD.y, posD.z);
		if (!InBounds(dirSize, dirIndex))
			return;
		directChanges++;

		Color oldValue = directLightArr[dirIndex];

		directLightArr[dirIndex] = newValue;


		// To avoid losing color information by using a small number, mult the color sum later in shader as needed 
		// Surface light should count for 16 times as much as air light because of limited surface area compared to volume
		float ambChangeStrength = airLight ? 1/4f : 4f;

		// Ambient light change
		Vector3Int ambPos = new Vector3Int(posA.x / ambientScale, posA.y / ambientScale, posA.z / ambientScale);
		int ambIndex = IndexFromPos(fullSize / ambientScale, ambPos.x, ambPos.y, ambPos.z);

		if (!InBounds(fullSize / ambientScale, ambIndex))
			return;
		ambientChanges++;

		Color oldAmbValue = ambientLightArr[ambIndex];
		Color newAmbValue = oldAmbValue + (newValue - oldValue) * ambChangeStrength;

		ambientLightArr[ambIndex] = newAmbValue;
	}

	//private int DirectPosCoord(int posIn)
	//{
	//	float f1 = posIn;
	//	f1 /= fullSize;
	//	f1 -= 0.5f;

	//	float range = World.GetChunkSize() * (1 + hdRange);
	//	range /= fullSize;
	//	float f = f1 / range;

	//	float hq = Mathf.Clamp(f, -1, 1);
	//	float lq = Mathf.Max(0, Mathf.Abs(f) - 1) * Mathf.Sign(f);

	//	float total = hq / directScaleHQ + lq / directScaleLQ;
	//	total *= range;
	//	total += 0.5f;

	//	total *= dirSize;

	//	return (int)total;
	//}

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
		if (!Application.isPlaying || !GameManager.Instance || simpleMode)
			return;

		recentApplyTimer.Increment(Time.deltaTime);
		if (!recentApplyTimer.Expired())
			return;

		// Apply ambient changes as needed (cheaper to apply)
		if (ambientChanges >= targAmbientChanges)
		{
			UpdateAmbientTex();
			ambientChanges = 0;

			recentApplyTimer.Reset();
		}

		// Apply direct changes as needed (less frequently than ambient)
		if (directChanges >= targDirectChanges)
		{
			UpdateDirectTex();
			directChanges = 0;

			cleanupTimer.Reset();
			recentApplyTimer.Reset();
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
