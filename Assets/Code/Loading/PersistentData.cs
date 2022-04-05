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
	[SerializeField]
	private int depth = 0;

	private Inventory playerInventory;

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
		numericSeed = SeedDecoder.StringToLong(seed);
	}

	public string GetStringSeed()
	{
		return stringSeed;
	}

	public long GetNumericSeed()
	{
		return numericSeed;
	}

	public void IncreaseDepth(int step)
	{
		depth += step;
	}

	public int GetDepth()
	{
		return depth;
	}

	public void SetPlayerInventory(Inventory inventory)
	{
		playerInventory = inventory;
	}

	public Inventory GetPlayerInventory()
	{
		return playerInventory;
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
			go.name = "Persistent Data";
			return go.AddComponent<PersistentData>();
		}
	}

	// Get instance for reading persistent data if possible
	public static PersistentData GetInstanceForRead()
	{
		return Instance;
	}
}
