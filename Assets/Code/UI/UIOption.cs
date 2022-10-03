using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIOption : MonoBehaviour
{
	public TextMeshProUGUI nameText;
	public TextMeshProUGUI valueText;

	public Slider valueSlider;

	public void InitUI(SettingsMenu.SliderOptionData optionData)
	{
		nameText.text = optionData.name;
		SetValue(optionData.curValueUI, optionData.curValue);
	}

	private void SetValue(float uiValue, float realValue)
	{
		valueSlider.value = uiValue;
		SetValueText(realValue);
	}

	public void SetValueText(float value)
	{
		// Two decimal places
		valueText.text = string.Format("{0:0.00}", value);
	}
}
