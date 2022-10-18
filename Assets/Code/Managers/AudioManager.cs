using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using ExtensionMethods;

public class AudioManager : MonoBehaviour
{
	private static AudioManager Instance;

	[SerializeField]
	private AudioMixer mixer;

	[SerializeField]
	private int poolSize = 20;

	[SerializeField]
	private AudioSource musicPlayer;

	private List<GameObject> pooledObjects;

	private AudioListener listener;

	public enum CueType
	{
		Die,
		EncounterPossible,
		EncounterStarting,
		EncounterHappening
	}
	[SerializeField]
	private MusicCue[] musicCues;

	private MusicCue currentMusicCue;

	public enum UISoundType
	{
		Click,
		Back,
		Start
	}
	[SerializeField]
	private Sound[] uiSounds;

	public enum BlockSoundType
	{
		LightBuzz
	}
	[SerializeField]
	private Sound[] blockSounds;

	[SerializeField]
	private Sound caveNoiseSound;
	[SerializeField]
	private float caveNoiseTimeMin = 40;
	[SerializeField]
	private float caveNoiseTimeMax = 160;
	private Timer caveNoiseTimer = new Timer(20);

	private void Awake()
	{
		if (!Instance)
			Instance = this;
		else
			Destroy(gameObject);

		caveNoiseTimer.Reset(SeedlessRandom.NextFloatInRange(caveNoiseTimeMin, caveNoiseTimeMax));
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

	private void Update()
	{
		if (!musicPlayer.isPlaying)
		{
			if (currentMusicCue)
			{
				musicPlayer.clip = currentMusicCue.clip;
				musicPlayer.volume = currentMusicCue.volume;

				musicPlayer.Play();
				currentMusicCue = currentMusicCue.next;
			}
		}

		if (Player.Instance && GameManager.IsFinishedLoading() && (!currentMusicCue || !currentMusicCue.blocksCaveNoises))
		{
			// Play cave noises randomly near player
			caveNoiseTimer.Increment(Time.deltaTime);

			// Only play cave noises if current music cue does not block them
			if (caveNoiseTimer.Expired())
			{
				caveNoiseTimer.Reset(SeedlessRandom.NextFloatInRange(caveNoiseTimeMin, caveNoiseTimeMax));

				Transform playerTrans = Player.Instance.transform;

				float offsetDistance = SeedlessRandom.NextFloatInRange(4, 8);
				PlaySound(caveNoiseSound, playerTrans.position + Quaternion.Euler(0, SeedlessRandom.NextFloatInRange(-60, 60), 0) * playerTrans.forward * -offsetDistance);
			}
		}
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

		source.volume *= toPlay.volumeMult;

		float randomPitch = SeedlessRandom.NextFloatInRange(toPlay.pitchRange.min, toPlay.pitchRange.max);
		source.pitch *= randomPitch;

		source.clip = toPlay.GetClip();

		source.Play();

		// Return source back into the pool
		Instance.StartCoroutine(Instance.RecycleAudio(source));

		return source;
	}

	public static void PlayMusicCue(CueType cue)
	{
		Instance.currentMusicCue = Instance.musicCues[(int)cue];
		if (Instance.currentMusicCue.interrupts)
			Instance.musicPlayer.Stop();
	}

	public static void StopMusicCue()
	{
		Instance.currentMusicCue = null;
		Instance.musicPlayer.Stop();
	}

	public static void PlayUISound(UISoundType uiSound)
	{
		PlaySound(Instance.uiSounds[(int)uiSound], Instance.transform.position);
	}

	public static Sound GetBlockSound(BlockSoundType blockSound)
	{
		return Instance.blockSounds[(int)blockSound];
	}

	public static AudioSource CreateLoopingSound(Sound toPlay, Vector3 position)
	{
		if (!toPlay)
			return null;

		AudioSource source = Instantiate(toPlay.preset, position, Quaternion.identity);

		source.volume *= toPlay.volumeMult;

		float randomPitch = SeedlessRandom.NextFloatInRange(toPlay.pitchRange.min, toPlay.pitchRange.max);
		source.pitch *= randomPitch;

		source.clip = toPlay.GetClip();

		source.loop = true;

		source.Play();

		return source;
	}

	public static AudioSource PlaySoundDontDestroyOnLoad(Sound toPlay, Vector3 position)
	{
		if (!toPlay)
			return null;

		AudioSource source = Instantiate(toPlay.preset, position, Quaternion.identity);

		source.volume *= toPlay.volumeMult;

		float randomPitch = SeedlessRandom.NextFloatInRange(toPlay.pitchRange.min, toPlay.pitchRange.max);
		source.pitch *= randomPitch;

		source.clip = toPlay.GetClip();

		source.Play();

		DontDestroyOnLoad(source.gameObject);
		Destroy(source.gameObject, source.clip.length / source.pitch + 1);

		return source;
	}

	#region Dynamic Mixing
	public static void SetWorldEffectsFade(float volume)
	{
		Instance.mixer.SetFloat("WorldSFXFade", PercentageToDb(volume));
	}

	public static void SetWorldEffectsLowpass(float percent)
	{
		Instance.mixer.SetFloat("WorldSFXLowpass", PercentageToFreq(percent));
	}
	#endregion

	#region Settings
	public static void SetMasterVolume(float volume)
	{
		Instance.mixer.SetFloat("MasterVolume", PercentageToDb(volume));
	}

	public static void SetEffectsVolume(float volume)
	{
		Instance.mixer.SetFloat("SFXVolume", PercentageToDb(volume));
	}

	public static void SetMusicVolume(float volume)
	{
		Instance.mixer.SetFloat("MusicVolume", PercentageToDb(volume));
	}
	#endregion

	static float PercentageToDb(float valueIn)
	{
		return Mathf.Clamp(20 * Mathf.Log10(valueIn), -80, 0);
	}

	static float PercentageToFreq(float valueIn)
	{
		// 10, 100, 1000, 10000
		float expBase = 10;
		// Power ranges from 1 to 4
		float expPower = (1 + valueIn * 3);
		// 22, 220, 2200, 22000
		float expMult = 2.2f;

		return Mathf.Pow(expBase, expPower) * expMult;
	}

	private IEnumerator RecycleAudio(AudioSource source)
	{
		yield return new WaitForSeconds(source.clip.length / source.pitch + 1);

		// May have been removed by now
		if (!source)
			yield break;

		Destroy(source);

		source.gameObject.SetActive(false);
	}
}