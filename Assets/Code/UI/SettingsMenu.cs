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
		public float curValueUI = 0.5f; // Value as set in UI
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

	private void Start()
	{
		InitOption(lookSensitivity, 0.5f);

		InitOption(masterVolume, 0.75f);
		AudioManager.SetMasterVolume(masterVolume.curValue);
		InitOption(effectsVolume, 0.75f);
		AudioManager.SetMasterVolume(effectsVolume.curValue);
		InitOption(musicVolume, 0.75f);
		AudioManager.SetMasterVolume(musicVolume.curValue);

		InitOption(brightness, 0.5f);
		UIManager.SetBrightness(brightness.curValue);
	}

	private void InitOption(SliderOptionData optionData, float uiValue)
	{
		optionData.curValueUI = uiValue;
		optionData.curValue = ConvertUIToReal(uiValue, optionData);

		optionData.uiOption.InitUI(optionData);
	}

	public void UpdateLookSensitivity(float uiValue)
	{
		UpdateOption(uiValue, lookSensitivity);
	}

	public void UpdateMasterVolume(float uiValue)
	{
		UpdateOption(uiValue, masterVolume);
		AudioManager.SetMasterVolume(masterVolume.curValue);
	}
	public void UpdateEffectsVolume(float uiValue)
	{
		UpdateOption(uiValue, effectsVolume);
		AudioManager.SetEffectsVolume(effectsVolume.curValue);
	}
	public void UpdateMusicVolume(float uiValue)
	{
		UpdateOption(uiValue, musicVolume);
		AudioManager.SetMusicVolume(musicVolume.curValue);
	}

	public void UpdateBrightness(float uiValue)
	{
		UpdateOption(uiValue, brightness);
		UIManager.SetBrightness(brightness.curValue);
	}

	private void UpdateOption(float uiValue, SliderOptionData toUpdate)
	{
		toUpdate.curValue = ConvertUIToReal(uiValue, toUpdate);
		toUpdate.curValueUI = uiValue;

		toUpdate.uiOption.SetValueText(toUpdate.curValue);
	}

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
}
