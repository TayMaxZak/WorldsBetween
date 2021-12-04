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
	public AudioSource tickloop1;
	public AudioSource tickloop2;

	public float oneoverx;
	public float linear;

	public float proximity;
	public float dot;
	public float intensity;

	private void Update()
	{
		float cutoff = 0.05f;
		oneoverx = Mathf.Clamp01((2 / Mathf.Max(Vector3.Magnitude(apparition.position - looker.position) - 5, 0)) - cutoff);
		oneoverx = Mathf.Clamp01(oneoverx + cutoff * oneoverx);
		linear = Mathf.Clamp01((1 - Mathf.Max(Vector3.Magnitude(apparition.position - looker.position) - 5, 0) / range) - cutoff);
		linear = Mathf.Clamp01(linear + cutoff * linear);
		proximity = Mathf.Lerp(oneoverx, linear, 0.4f);
		dot = Mathf.Clamp01(Vector3.Dot(looker.forward, (apparition.position - looker.position).normalized));

		intensity = Mathf.Clamp01(proximity * (1 + dot));
		intensity *= intensity;

		tickloop1.volume = intensity * overallVolume;
		tickloop2.volume = 0;
	}
}

