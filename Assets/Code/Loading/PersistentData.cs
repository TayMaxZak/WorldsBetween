using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PersistentData : MonoBehaviour
{
	private static PersistentData Instance;

	[SerializeField]
	private string stringSeed = "0";
	[SerializeField]
	private long numericSeed = 0;

	public Inventory playerInventory;

	private void Awake()
	{
		if (!Instance)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		DontDestroyOnLoad(gameObject);
	}

	#region Get/Set Data
	public void SetSeed(string seed)
	{
		stringSeed = seed;
		numericSeed = SeedDecoder.StringToLong(seed, 10 + 26);
	}

	public string GetStringSeed()
	{
		return stringSeed;
	}

	public long GetNumericSeed()
	{
		return numericSeed;
	}

	public void SetInventory(Inventory inventory)
	{
		playerInventory = inventory;
	}
	#endregion

	// Get existing instance or make one to write persistent data to
	public static PersistentData GetInstanceForWrite()
	{
		if (Instance)
		{
			return Instance;
		}
		// To not clog up scene in editor time
		else
		{
			GameObject go = new GameObject();
			return go.AddComponent<PersistentData>();
		}
	}

	// Get instance for reading persistent data if possible
	public static PersistentData GetInstanceForRead()
	{
		return Instance;
	}
}
