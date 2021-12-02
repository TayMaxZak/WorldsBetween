using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;

public class AudioManager : MonoBehaviour
{
	private static AudioManager Instance;

	[SerializeField]
	private int poolSize = 20;

	[SerializeField]
	private AudioSource musicPlayer;

	private List<GameObject> pooledObjects;

	private AudioListener listener;

	private void Awake()
	{
		if (!Instance)
			Instance = this;
		else
			Destroy(gameObject);
	}

	private void Start()
	{
		pooledObjects = new List<GameObject>();
		GameObject tmp;
		for (int i = 0; i < poolSize; i++)
		{
			tmp = new GameObject();
			tmp.SetActive(false);
			tmp.transform.parent = transform;
			tmp.name = "Audio Source";
			pooledObjects.Add(tmp);
		}

		listener = FindObjectOfType<AudioListener>();
	}

	public static AudioSource PlaySound(Sound toPlay, Vector3 position)
	{
		if (!toPlay)
			return null;

		GameObject pooledObject = null;

		for (int i = 0; i < Instance.poolSize; i++)
		{
			if (!Instance.pooledObjects[i].activeInHierarchy)
			{
				pooledObject = Instance.pooledObjects[i];
				break;
			}
		}

		// Available audio source found
		if (pooledObject)
		{
			// Activate the deactivated object
			pooledObject.SetActive(true);
		}
		// If necessary, Override lower priority source
		else
		{
			for (int i = 0; i < Instance.poolSize; i++)
			{
				if (Instance.pooledObjects[i].activeInHierarchy)
				{
					AudioSource oldSource = Instance.pooledObjects[i].GetComponent<AudioSource>();

					// Is the new sound more important than (or as important as) this existing one?
					// TODO: Look at how long this source has been playing, how close to the player it is, etc.
					// TODO: Avoid playing overlapping sources
					if (oldSource)
					{
						bool lessImportant = oldSource.priority > toPlay.preset.priority;
						bool furtherAway = 
							(oldSource.transform.position - Instance.listener.transform.position).sqrMagnitude > (position - Instance.listener.transform.position).sqrMagnitude;

						if (lessImportant || (oldSource.priority == toPlay.preset.priority && furtherAway))
						{
							pooledObject = Instance.pooledObjects[i];

							// Cut out the old one
							Destroy(oldSource);

							break;
						}
					}
				}
			}
		}

		// No suitable audio source to use
		if (!pooledObject)
			return null;

		pooledObject.transform.position = position;

		// Override existing audio source
		AudioSource source = pooledObject.AddComponent(toPlay.preset);

		float randomPitch = Random.Range(toPlay.pitchRange.min, toPlay.pitchRange.max);
		source.pitch *= randomPitch;

		source.clip = toPlay.clip;

		source.Play();

		// Return source back into the pool
		Instance.StartCoroutine(Instance.RecycleAudio(source));

		return source;
	}

	public static void PlayMusicCue()
	{
		if (!Instance.musicPlayer.isPlaying)
			Instance.musicPlayer.Play();
		else
			Instance.musicPlayer.Stop();
	}

	private IEnumerator RecycleAudio(AudioSource source)
	{
		yield return new WaitForSeconds(source.clip.length + 1);

		// May have been removed by now
		if (!source)
			yield break;

		Destroy(source);

		source.gameObject.SetActive(false);
	}
}