using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Incursometer : MonoBehaviour
{
	public Transform looker;

	public Transform apparition;

	public float range = 40;

	public float overallVolume = 0.5f;
	public AudioSource tickloopSlow;
	public AudioSource tickloopMed;
	public AudioSource tickloopFast;

	public float oneoverx;
	public float linear;

	public float proximity;
	public float dot;
	public float intensity;

	public float weightSlow;
	public float weightMed;
	public float weightFast;

	private void Update()
	{
		float cutoff = 0.05f;
		oneoverx = Mathf.Clamp01((2 / Mathf.Max(Vector3.Magnitude(apparition.position - looker.position) - 5, 0)) - cutoff);
		oneoverx = Mathf.Clamp01(oneoverx + cutoff * oneoverx);
		linear = Mathf.Clamp01((1 - Mathf.Max(Vector3.Magnitude(apparition.position - looker.position) - 5, 0) / range) - cutoff);
		linear = Mathf.Clamp01(linear + cutoff * linear);
		proximity = Mathf.Lerp(oneoverx, linear, 0.4f);
		dot = Mathf.Clamp01(Vector3.Dot(looker.forward, (apparition.position - looker.position).normalized));

		intensity = Mathf.Clamp01(proximity * (1 + 0.67f * dot));

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

