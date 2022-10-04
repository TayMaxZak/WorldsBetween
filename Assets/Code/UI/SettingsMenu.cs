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

	public class GameSettings
	{
		public float lookSensitivity = 5;

		public float masterVolume = 0.75f;
		public float effectsVolume = 0.75f;
		public float musicVolume = 0.75f;

		public float brightness = 0;
	}

	public static GameSettings gameSettings;

	private void Start()
	{
		// Create settings object which will preserve options across scene loads
		if (gameSettings == null)
			gameSettings = new GameSettings();

		// Init controls
		InitOption(lookSensitivity, gameSettings.lookSensitivity);

		// Init audio
		InitOption(masterVolume, gameSettings.masterVolume);
		AudioManager.SetMasterVolume(masterVolume.curValue);

		InitOption(effectsVolume, gameSettings.effectsVolume);
		AudioManager.SetEffectsVolume(effectsVolume.curValue);

		InitOption(musicVolume, gameSettings.musicVolume);
		AudioManager.SetMusicVolume(musicVolume.curValue);

		// Init graphics
		InitOption(brightness, gameSettings.brightness);
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

		gameSettings.lookSensitivity = lookSensitivity.curValue;
	}

	public void UpdateMasterVolume(float uiValue)
	{
		UpdateOptionFromUI(uiValue, masterVolume);
		AudioManager.SetMasterVolume(masterVolume.curValue);

		gameSettings.masterVolume = masterVolume.curValue;
	}
	public void UpdateEffectsVolume(float uiValue)
	{
		UpdateOptionFromUI(uiValue, effectsVolume);
		AudioManager.SetEffectsVolume(effectsVolume.curValue);
		
		gameSettings.effectsVolume = effectsVolume.curValue;
	}
	public void UpdateMusicVolume(float uiValue)
	{
		UpdateOptionFromUI(uiValue, musicVolume);
		AudioManager.SetMusicVolume(musicVolume.curValue);

		gameSettings.musicVolume = musicVolume.curValue;
	}

	public void UpdateBrightness(float uiValue)
	{
		UpdateOptionFromUI(uiValue, brightness);
		UIManager.SetBrightness(brightness.curValue);

		gameSettings.brightness = brightness.curValue;
	}

	private void UpdateOptionFromUI(float uiValue, SliderOptionData toUpdate)
	{
		toUpdate.curValue = ConvertUIToReal(uiValue, toUpdate);

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
}
