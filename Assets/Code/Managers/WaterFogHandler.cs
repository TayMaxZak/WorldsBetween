using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterFogHandler : MonoBehaviour
{
	private static WaterFogHandler Instance;

	private Camera povCamera;

	[SerializeField]
	private FogSettings init = new FogSettings();
	[SerializeField]
	private FogSettings underwater = new FogSettings();

	[System.Serializable]
	private class FogSettings
	{
		public Color fogColor;
		public float start;
		public float end;
		public Material skybox;
	}

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

	private void Update()
	{
		if (Application.isPlaying)
		{
			if (povCamera == null)
				povCamera = Camera.main;

			SetSettings(InWater(povCamera, World.GetWaterHeight()));
		}
	}

	public static bool InWater(Camera test, int waterHeight)
	{
		return test.transform.position.y <= waterHeight + 0.9f;
	}

	[ContextMenu("Apply Default")]
	private void DefaultSettings()
	{
		FogSettings settings = init;

		RenderSettings.fogColor = settings.fogColor;
		RenderSettings.fogStartDistance = settings.start;
		RenderSettings.fogEndDistance = settings.end;
		RenderSettings.skybox = settings.skybox;
	}

	public static void SetSettings(bool inWater)
	{
		FogSettings settings = inWater ? Instance.underwater : Instance.init;

		RenderSettings.fogColor = settings.fogColor;
		RenderSettings.fogStartDistance = settings.start;
		RenderSettings.fogEndDistance = settings.end;
		RenderSettings.skybox = settings.skybox;
	}
}
