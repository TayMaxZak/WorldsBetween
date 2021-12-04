using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenHook : MonoBehaviour
{
	[SerializeField]
	private Image loadingBar;

	private void Update()
	{
		SetProgress(World.Generator.GenProgress());
	}

	public void SetProgress(float progress)
	{
		loadingBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, progress * 150);
	}

	public void Hide()
	{
		gameObject.SetActive(false);
	}
}
