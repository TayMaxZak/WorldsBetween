using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadBar : MonoBehaviour
{
	[SerializeField]
	private Image fill;

	private void Update()
	{
		SetProgress(World.Generator.GenProgress());
	}

	public void SetProgress(float progress)
	{
		fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, progress * 150);
	}
}
