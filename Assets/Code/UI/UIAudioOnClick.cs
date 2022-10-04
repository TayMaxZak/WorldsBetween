using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIAudioOnClick : MonoBehaviour, IPointerDownHandler
{
	public AudioManager.UISoundType soundType = AudioManager.UISoundType.Click;

	public void OnPointerDown(PointerEventData eventData)
	{
		AudioManager.PlayUISound(soundType);
	}
}
