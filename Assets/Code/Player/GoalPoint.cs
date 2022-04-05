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
		transform.position = blockPos + new Vector3(0.5f, 1, 0.5f);
	}

	public void ActivateGoal()
	{
		Debug.Log("Activated goal");

		activated = true;
	}

	public void Update()
	{
		if (Vector3.SqrMagnitude(transform.position - Player.Instance.transform.position) < 3 * 3)
		{
			PersistentData pd = PersistentData.GetInstanceForRead();
			if (pd)
				pd.SetSeed(pd.GetStringSeed() + 1);

			SceneManager.LoadScene(1);
		}
	}
}
