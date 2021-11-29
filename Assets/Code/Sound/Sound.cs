using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Defines a sound with an audio clip and audio settings
[CreateAssetMenu(menuName = "Sound")]
[System.Serializable]
public class Sound : ScriptableObject
{
	public AudioClip clip = null;

	public AudioSource preset = null;

	public RangeFloat pitchRange = new RangeFloat(0.5f, 1.5f);
}

[System.Serializable]
public class RangeFloat
{
	public float min = 0.5f;
	public float max = 1.5f;

	public RangeFloat(float min, float max)
	{
		this.min = min;
		this.max = max;
	}
}