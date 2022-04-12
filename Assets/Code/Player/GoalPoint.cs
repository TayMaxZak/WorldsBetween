using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalPoint : MonoBehaviour
{
	public static GoalPoint Instance;

	private bool activated = false;

	private void Awake()
	{
		// Ensure singleton
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}
		else
			Instance = this;
	}

	public void InitGoalActor(Vector3 blockPos)
	{
		Debug.Log("Init goal actor at " + blockPos);

		// Set physical position
		transform.position = blockPos + new Vector3(0.5f, 1.5f, 0.5f);

		NewPosition();
	}

	public void ActivateGoal()
	{
		Debug.Log("Activated goal");

		activated = true;
	}

	public void OnEnable()
	{
		NewPosition();
	}

	private void NewPosition()
	{
		Vector3Int intPos = new Vector3Int( Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y), Mathf.FloorToInt(transform.position.z));
		Chunk chunk = World.GetChunk(intPos);
		bool placeLight = true;
		if (chunk != null)
		{
			foreach (LightSource l in chunk.GetLights())
			{
				if (l.pos == intPos)
				{
					placeLight = false;
					break;
				}
			}
			if (placeLight)
			{
				chunk.GetLights().Add(new LightSource()
				{
					pos = intPos,
					lightColor = LightSource.colorWhite,
					brightness = 1,
					spread = 1,
					noise = 0
				}
				);
			}
		}
		else
			placeLight = false;

		Shader.SetGlobalVector("GoalPosition", transform.position);

		if (placeLight)
			World.RecalculateLight();
	}

	public void Update()
	{
		if (!activated)
			return;

		if (Vector3.SqrMagnitude(transform.position - Player.Instance.transform.position) < 3 * 3)
		{
			PersistentData pd = PersistentData.GetInstanceForRead();
			if (pd)
			{
				pd.SetSeed(pd.GetStringSeed() + 1);
				pd.IncreaseDepth(1);
			}

			SceneManager.LoadScene(1);
		}
	}
}
