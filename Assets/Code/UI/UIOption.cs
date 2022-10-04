using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIOption : MonoBehaviour
{
	public TextMeshProUGUI nameText;
	public TextMeshProUGUI valueText;
	public bool valueTextSigned = false;

	public Slider valueSlider;

	public void InitUI(SettingsMenu.SliderOptionData optionData, float uiValue)
	{
		nameText.text = optionData.name;
		SetValue(uiValue, optionData.curValue);
	}

	private void SetValue(float uiValue, float realValue)
	{
		valueSlider.value = uiValue;
		SetValueText(realValue);
	}

	public void SetValueText(float value)
	{
		// Two decimal places (and sign)
		if (!valueTextSigned)
			valueText.text = value.ToString("0.00");
		else
			valueText.text = value.ToString("+0.00;- 0.00;0.00");
	}
}
