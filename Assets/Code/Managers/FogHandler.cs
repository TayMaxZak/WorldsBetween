using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogHandler : MonoBehaviour
{
	private static FogHandler Instance;

	private Camera povCamera;

	[SerializeField]
	private FogSettings normal = new FogSettings();
	[SerializeField]
	private FogSettings underwater = new FogSettings();

	[System.Serializable]
	private class FogSettings
	{
		public Color fogColor = Color.white;
		public float fogStart = 100;
		public float fogEnd = 200;
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
		FogSettings settings = normal;

		RenderSettings.fogColor = settings.fogColor;
		RenderSettings.fogStartDistance = settings.fogStart;
		RenderSettings.fogEndDistance = settings.fogEnd;
		RenderSettings.ambientEquatorColor = settings.fogColor;

		Shader.SetGlobalVector("FogSettings", new Vector3(settings.fogStart, settings.fogEnd, World.GetWaterHeight()));
	}

	public static void SetSettings(bool inWater)
	{
		FogSettings settings = inWater ? Instance.underwater : Instance.normal;

		RenderSettings.fogColor = settings.fogColor;
		RenderSettings.fogStartDistance = settings.fogStart;
		RenderSettings.fogEndDistance = settings.fogEnd;

		// Looking up in water still has the old sky color
		RenderSettings.ambientSkyColor = Instance.normal.fogColor;
		RenderSettings.ambientEquatorColor = settings.fogColor;

		Shader.SetGlobalVector("FogSettings", new Vector3(settings.fogStart, settings.fogEnd, World.GetWaterHeight()));
	}
}
