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
	public class JsonSettings
	{
		public int lookSensitivity = 500;

		public int masterVolume = 75;
		public int effectsVolume = 75;
		public int musicVolume = 75;

		public int brightness = 0;

		public static float FromJson(int value)
		{
			return value / 100f;
		}

		public static int ToJson(float value)
		{
			return Mathf.RoundToInt(value * 100);
		}
	}

	public static JsonSettings jsonSettings;

	private void Start()
	{
		// Create settings object which will preserve options across scene loads
		if (jsonSettings == null)
		{
			jsonSettings = new JsonSettings();

			// Read Json file in directory
			string readJson = FileReadWrite.ReadString("GameSettings.json");

			// Will overwrite new fields and only if file exists
			JsonUtility.FromJsonOverwrite(readJson, jsonSettings);
		}

		InitAllOptions();
	}

	private void InitAllOptions()
	{
		// Init controls
		InitOption(lookSensitivity, JsonSettings.FromJson(jsonSettings.lookSensitivity));

		// Init audio
		InitOption(masterVolume, JsonSettings.FromJson(jsonSettings.masterVolume));
		AudioManager.SetMasterVolume(masterVolume.curValue);

		InitOption(effectsVolume, JsonSettings.FromJson(jsonSettings.effectsVolume));
		AudioManager.SetEffectsVolume(effectsVolume.curValue);

		InitOption(musicVolume, JsonSettings.FromJson(jsonSettings.musicVolume));
		AudioManager.SetMusicVolume(musicVolume.curValue);

		// Init graphics
		InitOption(brightness, JsonSettings.FromJson(jsonSettings.brightness));
		UIManager.SetBrightness(brightness.curValue);
	}

	public void SaveSettings()
	{
		Debug.Log("Saved settings");

		// Save game settings Json file when changing scene
		FileReadWrite.WriteString("GameSettings.json", JsonUtility.ToJson(jsonSettings));
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

		jsonSettings.lookSensitivity = JsonSettings.ToJson(lookSensitivity.curValue);
	}

	public void UpdateMasterVolume(float uiValue)
	{
		UpdateOptionFromUI(uiValue, masterVolume);
		AudioManager.SetMasterVolume(masterVolume.curValue);

		jsonSettings.masterVolume = JsonSettings.ToJson(masterVolume.curValue);
	}
	public void UpdateEffectsVolume(float uiValue)
	{
		UpdateOptionFromUI(uiValue, effectsVolume);
		AudioManager.SetEffectsVolume(effectsVolume.curValue);
		
		jsonSettings.effectsVolume = JsonSettings.ToJson(effectsVolume.curValue);
	}
	public void UpdateMusicVolume(float uiValue)
	{
		UpdateOptionFromUI(uiValue, musicVolume);
		AudioManager.SetMusicVolume(musicVolume.curValue);

		jsonSettings.musicVolume = JsonSettings.ToJson(musicVolume.curValue);
	}

	public void UpdateBrightness(float uiValue)
	{
		UpdateOptionFromUI(uiValue, brightness);
		UIManager.SetBrightness(brightness.curValue);

		jsonSettings.brightness = JsonSettings.ToJson(brightness.curValue);
	}

	private void UpdateOptionFromUI(float uiValue, SliderOptionData toUpdate)
	{
		toUpdate.curValue = RoundToDigits(ConvertUIToReal(uiValue, toUpdate), 2);

		toUpdate.uiOption.SetValueText(toUpdate.curValue);
	}

	public void ResetSettings()
	{
		AudioManager.PlayUISound(AudioManager.UISoundType.Click);

		jsonSettings = new JsonSettings();

		InitAllOptions();
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
