using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomTip : MonoBehaviour
{
	public string[] tips;

	[SerializeField]
	private TMPro.TextMeshProUGUI tipText;

	public List<int> usable = new List<int>();

	public void Randomize()
	{
		// Out of options
		if (usable.Count == 0)
			RestockTips();

		// Rare tip
		if (SeedlessRandom.NextFloat() < 0.01f)
			tipText.text = tips[0];
		// Other tips
		else
		{
			int index = SeedlessRandom.NextIntInRange(0, usable.Count);

			tipText.text = tips[usable[index]];

			usable.RemoveAt(index);
		}
	}

	private void RestockTips()
	{
		for (int i = 1; i < tips.Length; i++)
			usable.Add(i);
	}
}
