using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIKeystone : MonoBehaviour
{
	[SerializeField]
	private InputField sourceField;
	[SerializeField]
	private Text displayText;
	[SerializeField]
	private string finalStringSeed = "000000000";

	private void Awake()
	{
		displayText.text = "";
	}

	public void ConfirmCode()
	{
		string sourceText = sourceField.text;

		// TODO: Optimize with string builder?
		string changed = "";
		finalStringSeed = "";

		// Display text includes whitespace chars
		for (int i = 0; i < sourceText.Length; i++)
		{
			changed += char.ToUpper(sourceText[i]) + " ";
		}

		// String for backend is strictly alphanumeric and exactly 9 chars long
		for (int i = 0; i < 9; i++)
		{
			if (i < sourceText.Length)
				finalStringSeed += char.ToUpper(sourceText[i]);
			else
				finalStringSeed += '0';
		}

		displayText.text = changed;

		Debug.Log("Keystone updated: seed = " + finalStringSeed);
	}

	public string GetStringSeed()
	{
		return finalStringSeed;
	}
}
