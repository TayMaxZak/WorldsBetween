using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[System.Serializable]
[ExecuteInEditMode]
public class WorldLightAtlas : MonoBehaviour
{
	[System.Serializable]
	public class SubAtlas
	{
		private Color[] array;
		[SerializeField]
		private Texture3D texture;

		public int scale = 1;
		public Vector3Int size;

		public SubAtlas(Vector3Int fullSize, int scale, TextureFormat format)
		{
			size = fullSize / scale;
			this.scale = scale;

			array = new Color[size.x * size.y * size.z];

			for (int z = 0; z < size.z; z++)
			{
				for (int y = 0; y < size.y; y++)
				{
					for (int x = 0; x < size.x; x++)
					{
						SetColor(x, y, z, Color.black, false);
					}
				}
			}

			texture = new Texture3D(size.x, size.y, size.z, format, false)
			{
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear
			};
		}

		public void Apply()
		{
			texture.SetPixels(array);
			texture.Apply();
		}

		public void Clear(bool updateTexture)
		{
			for (int x = 0; x < size.x; x++)
			{
				for (int y = 0; y < size.y; y++)
				{
					for (int z = 0; z < size.z; z++)
					{
						array[IndexFromPos(x, y, z)] = Color.black;
					}
				}
			}

			if (updateTexture)
				Apply();
		}

		public void SetColor(Vector3Int pos, Color value, bool add)
		{
			SetColor(pos.x, pos.y, pos.z, value, add);
		}

		public void SetColor(int x, int y, int z, Color value, bool add)
		{
			array[IndexFromPos(x, y, z)] = add ? value : array[IndexFromPos(x, y, z)] + value;
		}

		public Texture3D GetTexture()
		{
			return texture;
		}

		private int IndexFromPos(int x, int y, int z)
		{
			int zOffset = z * size.y * size.x;
			int yOffset = y * size.x;
			int xOffset = x;

			return xOffset + yOffset + zOffset;
		}
	}

	public static WorldLightAtlas Instance;

	private bool simpleMode = false;

	[SerializeField]
	private Texture3D directTextureDefault; // Used when generated light is not in use
	[SerializeField]
	private Texture3D ambientTextureDefault;  // Used when generated light is not in use

	private Vector3Int fullSize = new Vector3Int(16, 16, 16); // Total size of atlas in world space
	private Vector3Int minPos = new Vector3Int(-8, -8, -8); // Minimum position ("bottom right corner") of atlas in world space

	[SerializeField]
	private SubAtlas directLight;
	[SerializeField]
	private SubAtlas ambientLight;

	//private static int directChanges = 0;
	//private static int ambientChanges = 0;
	private bool didInit = false;

	private void Awake()
	{
		if (Application.isPlaying)
			Instance = this;
	}

	public void OnEnable()
	{
		if (didInit)
			return;
		SetShaderReferences(directTextureDefault, ambientTextureDefault);
	}

	public void Init()
	{
		if (simpleMode)
			return;
		didInit = true;

		fullSize = World.GetWorldBounds().size;
		minPos = World.GetWorldBounds().min;

		directLight = new SubAtlas(fullSize, 1, TextureFormat.RGBAFloat);
		ambientLight = new SubAtlas(fullSize, 16, TextureFormat.RGBAFloat);

		SetShaderReferences(directLight.GetTexture(), ambientLight.GetTexture());

		Instance = this;
	}

	private void SetShaderReferences(Texture directTexture, Texture ambientTexture)
	{
		// For positioning textures correctly
		Shader.SetGlobalVector("LightAtlasSize", new Vector4(fullSize.x, fullSize.y, fullSize.z, 0));
		Shader.SetGlobalVector("LightAtlasMinPos", new Vector4(minPos.x, minPos.y, minPos.z, 0));

		// References to current textures
		Shader.SetGlobalTexture("DirectLightTexture", directTexture);
		Shader.SetGlobalTexture("AmbientLightTexture", ambientTexture);
	}

	public void ClearAtlas(bool updateTex)
	{
		directLight.Clear(updateTex);
		ambientLight.Clear(updateTex);
	}

	public void AggregateChunkLighting()
	{
		int chunkSize = World.GetChunkSize();

		foreach (var chunk in World.GetAllChunks())
		{
			Vector3Int chunkPos = new Vector3Int(chunk.Key.x, chunk.Key.y, chunk.Key.z);

			bool chunkOutOfWorld = !World.Contains(chunkPos);

			for (int x = 0; x < chunkSize; x++)
			{
				for (int y = 0; y < chunkSize; y++)
				{
					for (int z = 0; z < chunkSize; z++)
					{
						Vector3Int directPos = new Vector3Int(chunkPos.x + x, chunkPos.y + y, chunkPos.z + z);

						if (!World.Contains(directPos))
							continue;

						directPos = WorldPosToAtlasPos(directPos);

						directLight.SetColor(directPos, chunk.Value.GetLighting(x, y, z), true);
					}
				}
			}

			if (chunkOutOfWorld)
				continue;

			Vector3Int ambPos = WorldPosToAtlasPos(chunkPos);
			ambPos = new Vector3Int(
				ambPos.x /= 16,
				ambPos.y /= 16,
				ambPos.z /= 16
			);

			ambientLight.SetColor(ambPos, chunk.Value.GetAvgLighting(), false);
		}
	}

	[ContextMenu("Apply Changes")]
	public void UpdateLightTextures()
	{
		Debug.Log("Applied light atlas changes");

		directLight.Apply();
		ambientLight.Apply();
	}

	private Vector3Int WorldPosToAtlasPos(Vector3Int worldPos)
	{
		return worldPos - minPos;
	}
}
