using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomTip : MonoBehaviour
{
	public string[] tips;

	[SerializeField]
	private TMPro.TextMeshProUGUI tipText;

	public void Randomize()
	{
		tipText.text = tips[SeedlessRandom.NextIntInRange(0, tips.Length)];
	}
}
