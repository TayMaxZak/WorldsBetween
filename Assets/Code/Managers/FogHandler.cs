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

		public Color skyTopColor = Color.white;
		public Color skyBottomColor = Color.black;
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

			SetSettings(InWater(povCamera, World.Exists() ? World.GetWaterHeight() : -99999));
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

		RenderSettings.ambientSkyColor = settings.skyTopColor;
		RenderSettings.ambientEquatorColor = settings.fogColor;
		RenderSettings.ambientGroundColor = settings.skyBottomColor;

		Shader.SetGlobalVector("FogSettings", new Vector3(settings.fogStart, settings.fogEnd, World.Exists() ? World.GetWaterHeight() : -99999));
	}

	public static void SetSettings(bool inWater)
	{
		FogSettings settings = inWater ? Instance.underwater : Instance.normal;

		RenderSettings.fogColor = settings.fogColor;
		RenderSettings.fogStartDistance = settings.fogStart;
		RenderSettings.fogEndDistance = settings.fogEnd;

		RenderSettings.ambientSkyColor = settings.skyTopColor;
		RenderSettings.ambientEquatorColor = settings.fogColor;
		RenderSettings.ambientGroundColor = settings.skyBottomColor;

		Shader.SetGlobalVector("FogSettings", new Vector3(settings.fogStart, settings.fogEnd, World.Exists() ? World.GetWaterHeight() : -99999));
	}
}
