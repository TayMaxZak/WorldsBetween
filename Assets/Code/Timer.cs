using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// All-in-one tracker of a max time and current progress
[System.Serializable]
public class Timer
{
	public float maxTime = 1;

	[System.NonSerialized]
	public float currentTime;

	public Timer(float time) : this(time, time)
	{

	}

	public Timer(float time, float startTime)
	{
		maxTime = time;

		currentTime = startTime;
	}

	public void Increment(float deltaTime)
	{
		currentTime -= deltaTime;
	}

	public bool Expired()
	{
		return currentTime <= 0;
	}

	public void Reset()
	{
		currentTime = maxTime;
	}

	public void Reset(float newTime)
	{
		currentTime = newTime;
	}
}
