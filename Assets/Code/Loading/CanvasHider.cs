using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasHider : MonoBehaviour
{
	public void Hide()
	{
		gameObject.SetActive(false);
	}
}
