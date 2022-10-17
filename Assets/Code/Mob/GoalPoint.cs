using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class GoalPoint : MonoBehaviour
{
	public static GoalPoint Instance;

	private bool activated = false;
	private bool used = false;

	[SerializeField]
	private float activationDistance = 1;
	[SerializeField]
	private float magnetDistance = 3;
	[SerializeField]
	private float magnetSpeed = 0.5f;

	private Vector3 velocity;

	[SerializeField]
	private GameObject innerVisual;
	[SerializeField]
	private GameObject outerVisual;
	private Vector3 outerVisualPos;

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

		outerVisualPos = transform.position;
	}

	public void InitGoalActor(Vector3 blockPos)
	{
		// Set physical position
		transform.position = blockPos + new Vector3(0.5f, 1f, 0.5f);

		NewPosition();
	}

	public void ActivateGoal()
	{
		activated = true;
	}

	public void OnEnable()
	{
		NewPosition();
	}

	private void NewPosition()
	{
		World.SetGoalPoint(transform.position);
		outerVisualPos = transform.position;
	}

	public void Update()
	{
		if (!activated)
			return;

		Vector3 dif = Player.Instance.transform.position - transform.position;
		float sqrDist = Vector3.SqrMagnitude(dif);

		if (sqrDist < magnetDistance * magnetDistance)
		{
			velocity = magnetSpeed / Mathf.Max(1, Mathf.Sqrt(sqrDist)) * dif.normalized;
		}
		else
			velocity = Vector3.zero;

		transform.Translate(velocity * Time.unscaledDeltaTime);

		outerVisualPos = Vector3.Lerp(outerVisualPos, transform.position, Time.unscaledDeltaTime);
		outerVisual.transform.position = outerVisualPos;

		if (!used && sqrDist < activationDistance * activationDistance)
		{
			used = true;

			innerVisual.SetActive(false);

			World.SetGoalPoint(transform.position);

			PersistentData pd = PersistentData.GetInstanceForRead();
			if (pd)
			{
				pd.SetSeed(pd.GetSeed() + 1);
				pd.IncreaseDepth(1);
			}

			GameManager.FinishLevel();
		}
	}
}
