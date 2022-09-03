using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Defines a sound with an audio clip and audio settings
[CreateAssetMenu(menuName = "Music Cue")]
[System.Serializable]
public class MusicCue : ScriptableObject
{
	public float volume = 0.5f;

	public AudioClip clip = null;

	public MusicCue next = null;

	public bool interrupts = false;
}