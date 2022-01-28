using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Incursometer : MonoBehaviour
{
	public PlayerVitals vitals;

	public GameObject flashlight;

	public bool held;

	public bool flashlightOn;

	public Transform looker;

	public Cephapath[] apparitions;

	public float range = 40;

	public float overallVolume = 0.5f;
	public AudioSource tickloopSlow;
	public AudioSource tickloopMed;
	public AudioSource tickloopFast;

	private float oneoverx;
	private float linear;

	private float proximity;
	private float dot;
	private float intensity;

	private float weightSlow;
	private float weightMed;
	private float weightFast;

	private void Start()
	{
		apparitions = FindObjectsOfType<Cephapath>(true);
	}

	private void Update()
	{
		if (vitals.dead)
		{
			tickloopSlow.volume = 0;
			tickloopMed.volume = 0;
			tickloopFast.volume = 0;

			flashlightOn = false;

			return;
		}

		if (Input.GetButtonDown("Equipment"))
			held = !held;

		if (Input.GetButtonDown("Flashlight"))
		{	
			flashlightOn = !flashlightOn;
			//flashlight.SetActive(flashlightOn);

			if (!flashlightOn)
			{
				Shader.SetGlobalVector("FlashlightA", Vector3.one * 10000);
				Shader.SetGlobalVector("FlashlightB", Vector3.one * 10001);
			}
		}

		UIManager.SetMeterRaised(held);

		if (!held)
		{
			tickloopSlow.volume = 0;
			tickloopMed.volume = 0;
			tickloopFast.volume = 0;

			return;
		}

		intensity = 0;

		float cutoff = 0.05f;
		foreach (Cephapath apparition in apparitions)
		{
			oneoverx = Mathf.Clamp01((2 / Mathf.Max(Vector3.Magnitude(apparition.transform.position - looker.position) - 8, 0)) - cutoff);
			oneoverx = Mathf.Clamp01(oneoverx + cutoff * oneoverx);
			linear = Mathf.Clamp01((1 - Mathf.Max(Vector3.Magnitude(apparition.transform.position - looker.position) - 8, 0) / range) - cutoff);
			linear = Mathf.Clamp01(linear + cutoff * linear);
			proximity = Mathf.Lerp(oneoverx, linear, 0.5f);
			dot = Mathf.Clamp01(Vector3.Dot(looker.forward, (apparition.transform.position - looker.position).normalized));

			float toAdd = Mathf.Clamp01(proximity * (1 + 1 * dot));
			intensity = 1 - (1 - intensity) * (1 - toAdd);
		}

		weightSlow = (1 - intensity);
		weightSlow = Mathf.Clamp01(2 * IncreaseContrast(weightSlow));
		weightMed = (1 - Mathf.Abs(2 * intensity - 1));
		weightMed = IncreaseContrast(weightMed);
		weightFast = intensity;
		weightFast = Mathf.Clamp01(2 * IncreaseContrast(weightFast));

		tickloopSlow.volume = weightSlow * overallVolume;
		tickloopMed.volume = weightMed * overallVolume;
		tickloopFast.volume = weightFast * overallVolume;
	}

	private float IncreaseContrast(float input)
	{
		float output = input;
		output -= 0.5f;
		output *= 2;
		output = Mathf.Clamp01(output);

		return output;
	}
}

