using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomTip : MonoBehaviour
{
	public string[] tips;

	[SerializeField]
	private TMPro.TextMeshProUGUI tipText;

	private void Start()
	{
		tipText.text = tips[Random.Range(0, tips.Length)];
	}
}
