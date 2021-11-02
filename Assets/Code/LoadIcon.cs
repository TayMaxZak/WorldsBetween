using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadIcon : MonoBehaviour
{
	[SerializeField]
	private CanvasGroup group;

	[SerializeField]
	private Image icon1, icon2;

	[SerializeField]
	private Text text;

	private Color color1 = new Color(1, 1, 1, 1), color2 = new Color(1, 1, 1, 1);

	[SerializeField]
	private float topSpeed = 30;

	// Update is called once per frame
	void Update()
	{
		bool shouldSpin = World.IsGen();

		group.alpha = shouldSpin ? 0.9f : 0;

		if (!shouldSpin)
			return;

		text.text = World.ChunksToGen().ToString();

		// Rotate in opposite directions
		float rotSpeed = topSpeed * Mathf.Lerp(OTo1FromSinTime(Mathf.PI * 2.9f, 1), 1, 0.67f);

		icon1.rectTransform.Rotate(new Vector3(0, 0, rotSpeed * Time.deltaTime));
		icon2.rectTransform.Rotate(new Vector3(0, 0, -rotSpeed * Time.deltaTime));

		//// Fade from white to black
		//float bright = OTo1FromSinTime(Mathf.PI / 7, 0);
		//color1.r = bright;
		//color1.g = bright;
		//color1.b = bright;

		//color2.r = bright;
		//color2.g = bright;
		//color2.b = bright;

		// Fade from low to high opacity
		float fadeSpeed = 1.5f;
		color1.a = OTo1FromSinTime(Mathf.PI / fadeSpeed, 0);
		color2.a = OTo1FromSinTime(Mathf.PI / fadeSpeed, Mathf.PI / fadeSpeed);

		// Assign colors
		icon1.color = color1;
		icon2.color = color2;
	}

	private float OTo1FromSinTime(float mult, float offset)
	{
		return (Mathf.Sin(Time.time * mult + offset) + 1) / 2;
	}
}
