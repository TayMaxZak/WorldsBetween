using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIKeystone : MonoBehaviour
{
	[SerializeField]
	private InputField field;
	[SerializeField]
	private Text realText;

	private void Awake()
	{
		realText.text = "";
	}

	public void ConfirmCode()
	{
		// TODO: String builder?
		string initial = field.text;

		string changed = "";

		for (int i = 0; i < initial.Length; i++)
		{
			changed += char.ToUpper(initial[i]) + " ";
		}

		realText.text = changed;

		Debug.Log("keystone = " + field.text + " vs " + realText.text);
	}
}
