using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SettingsMenu : MonoBehaviour
{
	[SerializeField]
	private AudioMixer mixer;

	[System.Serializable]
	public class SliderOptionData
	{
		public UIOption uiOption = null;

		public string name = "Option";

		//[System.NonSerialized]
		//public float curValueUI = 0.5f; // Value as set in UI
		[System.NonSerialized]
		public float curValue = 0.5f; // Value translated from UI to proper range

		public float minValue = 0.0f; // Lower limit
		public float midValue = 0.5f; // Separates lower and upper halves
		public float maxValue = 1.0f; // Upper limit
	}

	// Controls
	public SliderOptionData lookSensitivity;

	// Audio
	public SliderOptionData masterVolume;
	public SliderOptionData effectsVolume;
	public SliderOptionData musicVolume;

	// Graphics
	public SliderOptionData brightness;

	[System.Serializable]
	public class JSONSettings
	{
		public int lookSensitivity = 500;

		public int masterVolume = 75;
		public int effectsVolume = 75;
		public int musicVolume = 75;

		public int brightness = 0;

		public static float FromJSON(int value)
		{
			return value / 100f;
		}

		public static int ToJSON(float value)
		{
			return Mathf.RoundToInt(value * 100);
		}
	}

	public static JSONSettings jsonSettings;

	private void Start()
	{
		// Create settings object which will preserve options across scene loads
		if (jsonSettings == null)
			jsonSettings = new JSONSettings();

		Debug.Log(JsonUtility.ToJson(jsonSettings));

		// Init controls
		InitOption(lookSensitivity, JSONSettings.FromJSON(jsonSettings.lookSensitivity));

		// Init audio
		InitOption(masterVolume, JSONSettings.FromJSON(jsonSettings.masterVolume));
		AudioManager.SetMasterVolume(masterVolume.curValue);

		InitOption(effectsVolume, JSONSettings.FromJSON(jsonSettings.effectsVolume));
		AudioManager.SetEffectsVolume(effectsVolume.curValue);

		InitOption(musicVolume, JSONSettings.FromJSON(jsonSettings.musicVolume));
		AudioManager.SetMusicVolume(musicVolume.curValue);

		// Init graphics
		InitOption(brightness, JSONSettings.FromJSON(jsonSettings.brightness));
		UIManager.SetBrightness(brightness.curValue);
	}

	private void InitOption(SliderOptionData optionData, float realValue)
	{
		optionData.curValue = realValue;

		optionData.uiOption.InitUI(optionData, ConvertRealToUI(realValue, optionData));
	}

	#region Updating Options from UI
	public void UpdateLookSensitivity(float uiValue)
	{
		UpdateOptionFromUI(uiValue, lookSensitivity);

		jsonSettings.lookSensitivity = JSONSettings.ToJSON(lookSensitivity.curValue);
	}

	public void UpdateMasterVolume(float uiValue)
	{
		UpdateOptionFromUI(uiValue, masterVolume);
		AudioManager.SetMasterVolume(masterVolume.curValue);

		jsonSettings.masterVolume = JSONSettings.ToJSON(masterVolume.curValue);
	}
	public void UpdateEffectsVolume(float uiValue)
	{
		UpdateOptionFromUI(uiValue, effectsVolume);
		AudioManager.SetEffectsVolume(effectsVolume.curValue);
		
		jsonSettings.effectsVolume = JSONSettings.ToJSON(effectsVolume.curValue);
	}
	public void UpdateMusicVolume(float uiValue)
	{
		UpdateOptionFromUI(uiValue, musicVolume);
		AudioManager.SetMusicVolume(musicVolume.curValue);

		jsonSettings.musicVolume = JSONSettings.ToJSON(musicVolume.curValue);
	}

	public void UpdateBrightness(float uiValue)
	{
		UpdateOptionFromUI(uiValue, brightness);
		UIManager.SetBrightness(brightness.curValue);

		jsonSettings.brightness = JSONSettings.ToJSON(brightness.curValue);
	}

	private void UpdateOptionFromUI(float uiValue, SliderOptionData toUpdate)
	{
		toUpdate.curValue = RoundToDigits(ConvertUIToReal(uiValue, toUpdate), 2);

		toUpdate.uiOption.SetValueText(toUpdate.curValue);
	}
	#endregion

	private float ConvertUIToReal(float uiValue, SliderOptionData optionData)
	{
		// Lower half
		if (uiValue < 0.5f)
		{
			return optionData.minValue + uiValue * 2 * (optionData.midValue - optionData.minValue);
		}
		// Upper half
		else
		{
			return optionData.midValue + (uiValue - 0.5f) * 2 * (optionData.maxValue - optionData.midValue);
		}
	}

	private float ConvertRealToUI(float realValue, SliderOptionData optionData)
	{
		// Lower half
		if (realValue < optionData.midValue)
		{
			return 0.5f * (realValue - optionData.minValue) / (optionData.midValue - optionData.minValue);
		}
		// Upper half
		else
		{
			return 0.5f + 0.5f * (realValue - optionData.midValue) / (optionData.maxValue - optionData.midValue);
		}
	}

	private float RoundToDigits(float value, int digits)
	{
		float mult = Mathf.Pow(10.0f, digits);
		float result = Mathf.RoundToInt(mult * value) / mult;
		return result;
	}
}
