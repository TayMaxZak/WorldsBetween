using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PersistentData : MonoBehaviour
{
	private static PersistentData Instance;

	[SerializeField]
	private int seed = 0;
	[SerializeField]
	private int depth = 0;
	[SerializeField]
	private bool debugMode = false;

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
	public int GetSeed()
	{
		return seed;
	}

	public void SetSeed(int seed)
	{
		this.seed = seed;
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

	public void SetDebugMode(bool value)
	{
		debugMode = value;
	}

	public bool IsDebugMode()
	{
		return debugMode;
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
